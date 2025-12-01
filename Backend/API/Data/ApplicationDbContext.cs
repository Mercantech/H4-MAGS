using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ExternalIdentity> ExternalIdentities { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<QuizSession> QuizSessions { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<ParticipantAnswer> ParticipantAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Case-insensitive unique indexes (PostgreSQL)
            entity.HasIndex(e => e.Username)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Username");
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Email");
            
            // Value converters for case-insensitive storage
            entity.Property(e => e.Username)
                  .IsRequired()
                  .HasMaxLength(100)
                  .HasConversion(
                      v => v.ToLowerInvariant(),
                      v => v.ToLowerInvariant());
            
            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(255)
                  .HasConversion(
                      v => v.ToLowerInvariant(),
                      v => v.ToLowerInvariant());
            
            entity.Property(e => e.PasswordHash).HasMaxLength(500); // Nullable - SSO-only brugere har ikke password
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Configure ExternalIdentity entity
        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Composite unique index: En bruger kan kun have én identitet pr. provider
            entity.HasIndex(e => new { e.UserId, e.Provider })
                  .IsUnique()
                  .HasDatabaseName("IX_ExternalIdentities_UserId_Provider");
            
            // Index på ProviderUserId for hurtig lookup
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId })
                  .IsUnique()
                  .HasDatabaseName("IX_ExternalIdentities_Provider_ProviderUserId");
            
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProviderUserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ProviderEmail).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Configure relationship with User
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ExternalIdentities)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsRevoked).IsRequired();

            // Configure relationship with User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Quiz entity
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Pin).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.Pin).IsUnique();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Configure relationship with User
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Quizzes)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TimeLimitSeconds).IsRequired();
            entity.Property(e => e.Points).IsRequired();
            entity.Property(e => e.OrderIndex).IsRequired();
            entity.Property(e => e.QuizId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Configure relationship with Quiz
            entity.HasOne(e => e.Quiz)
                  .WithMany(q => q.Questions)
                  .HasForeignKey(e => e.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Answer entity
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsCorrect).IsRequired();
            entity.Property(e => e.OrderIndex).IsRequired();
            entity.Property(e => e.QuestionId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Configure relationship with Question
            entity.HasOne(e => e.Question)
                  .WithMany(q => q.Answers)
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure QuizSession entity
        modelBuilder.Entity<QuizSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionPin).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.SessionPin).IsUnique();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.QuizId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Configure relationship with Quiz
            entity.HasOne(e => e.Quiz)
                  .WithMany(q => q.Sessions)
                  .HasForeignKey(e => e.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Participant entity
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nickname).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TotalPoints).IsRequired();
            entity.Property(e => e.QuizSessionId).IsRequired();
            entity.Property(e => e.JoinedAt).IsRequired();

            // Configure relationship with QuizSession
            entity.HasOne(e => e.QuizSession)
                  .WithMany(s => s.Participants)
                  .HasForeignKey(e => e.QuizSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ParticipantAnswer entity
        modelBuilder.Entity<ParticipantAnswer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PointsEarned).IsRequired();
            entity.Property(e => e.ResponseTimeMs).IsRequired();
            entity.Property(e => e.ParticipantId).IsRequired();
            entity.Property(e => e.QuestionId).IsRequired();
            entity.Property(e => e.AnswerId).IsRequired();
            entity.Property(e => e.AnsweredAt).IsRequired();

            // Configure relationship with Participant
            entity.HasOne(e => e.Participant)
                  .WithMany(p => p.Answers)
                  .HasForeignKey(e => e.ParticipantId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with Question
            entity.HasOne(e => e.Question)
                  .WithMany(q => q.ParticipantAnswers)
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.Restrict); // Ikke slet spørgsmål hvis der er svar

            // Configure relationship with Answer
            entity.HasOne(e => e.Answer)
                  .WithMany(a => a.ParticipantAnswers)
                  .HasForeignKey(e => e.AnswerId)
                  .OnDelete(DeleteBehavior.Restrict); // Ikke slet svar hvis der er deltager svar
        });
    }

    /// <summary>
    /// Override SaveChangesAsync for at automatisk opdatere UpdatedAt for alle BaseEntity entiteter
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                // Sæt CreatedAt hvis det ikke allerede er sat
                if (entity.CreatedAt == default)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }
            
            // Opdater UpdatedAt for både nye og modificerede entiteter
            entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Override SaveChanges for at automatisk opdatere UpdatedAt for alle BaseEntity entiteter
    /// </summary>
    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                // Sæt CreatedAt hvis det ikke allerede er sat
                if (entity.CreatedAt == default)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }
            
            // Opdater UpdatedAt for både nye og modificerede entiteter
            entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChanges();
    }
}
