using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RetroScan.Api.Data;

public sealed class RetroScanDbContext(DbContextOptions<RetroScanDbContext> options) : DbContext(options)
{
    public DbSet<ResearchSession> Sessions => Set<ResearchSession>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Source> Sources => Set<Source>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // MySQL DATETIME has no time zone, so values read back as Unspecified unless marked UTC here.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResearchSession>(session =>
        {
            session.ToTable("research_sessions");
            session.Property(s => s.Title).HasMaxLength(ResearchSession.TitleMaxLength);
            session.HasIndex(s => new { s.OwnerId, s.UpdatedAt });
            session.HasMany(s => s.Questions)
                .WithOne()
                .HasForeignKey(q => q.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(question =>
        {
            question.ToTable("questions");
            question.Property(q => q.Text).HasMaxLength(Question.TextMaxLength);
            question.Property(q => q.SearchQuery).HasMaxLength(Question.SearchQueryMaxLength);
            question.Property(q => q.Answer).HasColumnType("text");
            question.HasMany(q => q.Sources)
                .WithOne()
                .HasForeignKey(s => s.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Source>(source =>
        {
            source.ToTable("sources");
            source.Property(s => s.Title).HasMaxLength(Source.TitleMaxLength);
            source.Property(s => s.Url).HasMaxLength(Source.UrlMaxLength);
            source.Property(s => s.Excerpt).HasColumnType("text");
            source.HasIndex(s => new { s.QuestionId, s.Number }).IsUnique();
        });
    }

    private sealed class UtcConverter() : ValueConverter<DateTime, DateTime>(
        value => value.ToUniversalTime(),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
