using System.Reflection;
using System.Text;
using API.Data;
using API.Extensions;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

// Konfigurer Serilog tidligt
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starter H4-MAGS API...");

var builder = WebApplication.CreateBuilder(args);

// Tilføj Serilog til hosting
builder.Host.UseSerilog((context, services, configuration) =>
{
    var seqServerUrl = context.Configuration["Seq:ServerUrl"] ?? "http://seq:80";
    Log.Information("Konfigureret Serilog til at sende logs til Seq på: {SeqUrl}", seqServerUrl);
    
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(
            serverUrl: seqServerUrl,
            apiKey: context.Configuration["Seq:ApiKey"],
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
            period: TimeSpan.FromSeconds(2),
            batchPostingLimit: 100
        );
});

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework Core med PostgreSQL
// Konfigurer med retry logic for Neon.tech "sleep mode" problemer
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionStringWithEnv("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection er ikke konfigureret"),
        npgsqlOptions => npgsqlOptions
            .EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null)
            .CommandTimeout(60)) // 60 sekunder timeout
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableServiceProviderCaching());

// Mail-konfiguration (Gmail SMTP). Password kan sættes via Mail__Password eller User Secrets.
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection(MailSettings.SectionName));
builder.Services.AddScoped<IMailService, MailService>();

// MinIO (S3-kompatibel storage). I prod sættes Storage__MinIO__* via docker-compose env.
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection(MinioOptions.SectionName));

// Add custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOAuthService, OAuthService>(); // Generisk OAuth service

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration.GetConfigValue("Jwt:SecretKey", "Jwt__SecretKey") 
    ?? throw new InvalidOperationException("JWT SecretKey er ikke konfigureret");
var jwtIssuer = builder.Configuration.GetConfigValue("Jwt:Issuer", "Jwt__Issuer") 
    ?? throw new InvalidOperationException("JWT Issuer er ikke konfigureret");
var jwtAudience = builder.Configuration.GetConfigValue("Jwt:Audience", "Jwt__Audience") 
    ?? throw new InvalidOperationException("JWT Audience er ikke konfigureret");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer, // Altid H4-MAGS-API
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add CORS support - Allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)  // Tillad alle origins
            .AllowAnyMethod()                  // Allow GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader()                  // Allow any headers
            .AllowCredentials();               // Allow cookies/auth headers
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // TODO: Add JWT support to Swagger - requires Microsoft.OpenApi.Models namespace
    // This will be implemented once the correct package reference is resolved
});

// OpenAPI configuration will be handled by middleware
var app = builder.Build();

// Kør EF-migrationer ved opstart i demo/CI (fx container med postgres)
var connStr = builder.Configuration.GetConnectionStringWithEnv("DefaultConnection");
var runMigrations = string.Equals(Environment.GetEnvironmentVariable("RUN_MIGRATIONS"), "true", StringComparison.OrdinalIgnoreCase)
    || (connStr?.Contains("Host=postgres", StringComparison.OrdinalIgnoreCase) ?? false);
if (runMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

// Enable Swagger JSON endpoint
app.UseSwagger();

// Enable Swagger UI (klassisk dokumentation (Med Darkmode))
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    options.RoutePrefix = "swagger"; // Tilgængelig på /swagger
    options.AddSwaggerBootstrap(); // UI Pakke lavet af NHave - https://github.com/nhave
    
    // JWT authentication konfigureres automatisk via Swagger security scheme
});

app.UseStaticFiles(); // Vigtig for SwaggerBootstrap pakken

// Enable Scalar UI (moderne alternativ til Swagger UI)
app.MapScalarApiReference(options =>
    {
        options.WithTitle("API Documentation")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        // Scalar understøtter automatisk JWT authentication baseret på OpenAPI security schemes
    });


// Enable CORS - SKAL være før UseAuthentication
app.UseCors("AllowAll");

// Request logging middleware (kun i development)
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🌐 [DEBUG] Request: {Method} {Path}", context.Request.Method, context.Request.Path);
        logger.LogInformation("🌐 [DEBUG] Origin: {Origin}", context.Request.Headers["Origin"].ToString());
        logger.LogInformation("🌐 [DEBUG] Content-Type: {ContentType}", context.Request.ContentType);
        
        await next();
        
        logger.LogInformation("🌐 [DEBUG] Response: {StatusCode}", context.Response.StatusCode);
    });
}

// Authentication og Authorization - SKAL være i denne rækkefølge
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log API dokumentations URL'er ved opstart
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var addresses = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
        .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses;

    if (addresses != null && app.Environment.IsDevelopment())
    {
        foreach (var address in addresses)
        {
            logger.LogInformation("Swagger UI: {Address}/swagger", address);
            logger.LogInformation("Scalar UI:  {Address}/scalar", address);
        }
    }
});

try
{
    Log.Information("H4-MAGS API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "H4-MAGS API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
