using GeminiWeatherApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeminiWeatherApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AiRequestRecord> AiRequestRecords => Set<AiRequestRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<AiRequestRecord>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Topic).HasMaxLength(50).IsRequired();
            entity.Property(record => record.City).HasMaxLength(120).IsRequired();
            entity.Property(record => record.Prompt).HasMaxLength(5000).IsRequired();
            entity.Property(record => record.Model).HasMaxLength(120).IsRequired();
            entity.Property(record => record.ResultJson).HasColumnType("text").IsRequired();
            entity.HasIndex(record => new { record.UserId, record.CreatedAtUtc });

            entity.HasOne(record => record.User)
                .WithMany(user => user.Requests)
                .HasForeignKey(record => record.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
