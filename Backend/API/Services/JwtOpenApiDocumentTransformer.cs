using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace API.Services;

public class JwtOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        
        if (document.Components.SecuritySchemes == null)
        {
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
        }
        
        // Tilføj JWT Bearer security scheme
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Indtast JWT token. Format: Bearer {token}"
        };

        // Tilføj security requirement til alle endpoints (optional - kan også sættes per endpoint)
        // Dette gør at "Authorize" knappen vises i Swagger UI
        if (document.Security == null)
        {
            document.Security = new List<OpenApiSecurityRequirement>();
        }

        return Task.CompletedTask;
    }
}

