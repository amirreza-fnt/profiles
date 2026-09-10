using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;

namespace ProfileService.Infrastructure.Persistence;

public sealed class ProfileDbContext : DbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options) { }

    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<ContactNumber> Contacts => Set<ContactNumber>();
    public DbSet<VerificationChallenge> VerificationChallenges => Set<VerificationChallenge>();
    public DbSet<ProfileAuditLog> AuditLogs => Set<ProfileAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(e =>
        {
            e.ToTable("Profiles");
            e.HasKey(x => x.NationalCode);
            e.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.SsoFirstName).HasMaxLength(100);
            e.Property(x => x.SsoLastName).HasMaxLength(100);
            e.Property(x => x.SsoFatherName).HasMaxLength(100);
            e.Property(x => x.SsoGender).HasMaxLength(20);
            e.Property(x => x.SsoBirthDate).HasMaxLength(32);
            e.Property(x => x.SsoEmail).HasMaxLength(256);
            e.Property(x => x.SsoProvince).HasMaxLength(100);
            e.Property(x => x.SsoCity).HasMaxLength(100);
            e.Property(x => x.SsoPostalCode).HasMaxLength(20);
            e.Property(x => x.UserBirthDate).HasMaxLength(32);
            e.Property(x => x.AvatarShortCode).HasMaxLength(32);
            e.Property(x => x.AvatarUrl).HasMaxLength(512);
            e.Property(x => x.PhotoShortCode).HasMaxLength(32);
            e.Property(x => x.PhotoUrl).HasMaxLength(512);
            e.HasMany(x => x.Contacts).WithOne(x => x.Profile).HasForeignKey(x => x.NationalCode).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.AuditLogs).WithOne(x => x.Profile).HasForeignKey(x => x.NationalCode).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactNumber>(e =>
        {
            e.ToTable("Contacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.NationalCode, x.Type, x.Number }).IsUnique();
            e.HasIndex(x => x.Number);
            e.HasMany(x => x.Challenges).WithOne(x => x.Contact).HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VerificationChallenge>(e =>
        {
            e.ToTable("VerificationChallenges");
            e.HasKey(x => x.Id);
            e.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.ContactId, x.ConsumedAtUtc, x.ExpiresAtUtc });
        });

        modelBuilder.Entity<ProfileAuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Action).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ActorId).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => new { x.NationalCode, x.CreatedAtUtc });
        });
    }
}
