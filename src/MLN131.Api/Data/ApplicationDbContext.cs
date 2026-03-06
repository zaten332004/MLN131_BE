using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MLN131.Api.Data;

public sealed class ApplicationDbContext
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<VisitSession> VisitSessions => Set<VisitSession>();
    public DbSet<PageViewEvent> PageViewEvents => Set<PageViewEvent>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserResponse> UserResponses => Set<UserResponse>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(b =>
        {
            b.HasIndex(x => x.CreatedAt);
            b.Property(x => x.FullName).HasMaxLength(200);
            b.Property(x => x.AvatarUrl).HasMaxLength(500);
        });

        builder.Entity<VisitSession>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.VisitorId);
            b.HasIndex(x => x.StartedAt);
            b.HasIndex(x => x.LastSeenAt);
            b.Property(x => x.UserAgent).HasMaxLength(512);
            b.Property(x => x.IpAddress).HasMaxLength(64);
            b.Property(x => x.PathFirst).HasMaxLength(256);
        });

        builder.Entity<PageViewEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.OccurredAt);
            b.Property(x => x.Path).HasMaxLength(256);
            b.Property(x => x.Referrer).HasMaxLength(512);
        });

        builder.Entity<ChatMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.CreatedAt });
            b.Property(x => x.Role).HasMaxLength(32);
        });

        builder.Entity<UserResponse>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.CreatedAt });
            b.Property(x => x.QuestionKey).HasMaxLength(128);
        });

        builder.Entity<ContentPage>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Slug).IsUnique();
            b.Property(x => x.Slug).HasMaxLength(128);
            b.Property(x => x.Title).HasMaxLength(256);
        });
    }
}

