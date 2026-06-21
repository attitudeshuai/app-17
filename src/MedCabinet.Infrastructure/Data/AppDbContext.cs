using MedCabinet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedCabinet.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Household> Households { get; set; }
    public DbSet<HouseholdMember> HouseholdMembers { get; set; }
    public DbSet<Medicine> Medicines { get; set; }
    public DbSet<MedUsage> MedUsages { get; set; }
    public DbSet<MedAlert> MedAlerts { get; set; }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                var createdAtProp = entry.Entity.GetType().GetProperty("CreatedAt");
                if (createdAtProp != null && createdAtProp.PropertyType == typeof(DateTime))
                {
                    createdAtProp.SetValue(entry.Entity, now);
                }

                var updatedAtProp = entry.Entity.GetType().GetProperty("UpdatedAt");
                if (updatedAtProp != null && updatedAtProp.PropertyType == typeof(DateTime))
                {
                    updatedAtProp.SetValue(entry.Entity, now);
                }

                var joinedAtProp = entry.Entity.GetType().GetProperty("JoinedAt");
                if (joinedAtProp != null && joinedAtProp.PropertyType == typeof(DateTime))
                {
                    joinedAtProp.SetValue(entry.Entity, now);
                }

                var usedAtProp = entry.Entity.GetType().GetProperty("UsedAt");
                if (usedAtProp != null && usedAtProp.PropertyType == typeof(DateTime))
                {
                    usedAtProp.SetValue(entry.Entity, now);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Entity.GetType().GetProperty("UpdatedAt");
                if (updatedAtProp != null && updatedAtProp.PropertyType == typeof(DateTime))
                {
                    updatedAtProp.SetValue(entry.Entity, now);
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Avatar).HasMaxLength(500);
        });

        // Household 配置
        modelBuilder.Entity<Household>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasIndex(h => h.InviteCode).IsUnique();
            entity.Property(h => h.Name).IsRequired().HasMaxLength(100);
            entity.Property(h => h.InviteCode).IsRequired().HasMaxLength(20);

            entity.HasOne(h => h.Creator)
                  .WithMany()
                  .HasForeignKey(h => h.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // HouseholdMember 配置
        modelBuilder.Entity<HouseholdMember>(entity =>
        {
            entity.HasKey(hm => hm.Id);
            entity.HasIndex(hm => new { hm.HouseholdId, hm.UserId }).IsUnique();
            entity.Property(hm => hm.Role).IsRequired().HasMaxLength(20);

            entity.HasOne(hm => hm.Household)
                  .WithMany(h => h.HouseholdMembers)
                  .HasForeignKey(hm => hm.HouseholdId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(hm => hm.User)
                  .WithMany(u => u.HouseholdMembers)
                  .HasForeignKey(hm => hm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Medicine 配置
        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Name).IsRequired().HasMaxLength(200);
            entity.Property(m => m.Category).IsRequired().HasMaxLength(50);
            entity.Property(m => m.Indication).HasMaxLength(500);
            entity.Property(m => m.Dosage).HasMaxLength(200);
            entity.Property(m => m.StorageLocation).HasMaxLength(200);
            entity.Property(m => m.Contraindications).HasMaxLength(1000);
            entity.Property(m => m.PhotoUrl).HasMaxLength(500);
            entity.Property(m => m.Status).IsRequired();

            entity.HasOne(m => m.Household)
                  .WithMany(h => h.Medicines)
                  .HasForeignKey(m => m.HouseholdId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MedUsage 配置
        modelBuilder.Entity<MedUsage>(entity =>
        {
            entity.HasKey(mu => mu.Id);
            entity.Property(mu => mu.UsedBy).IsRequired().HasMaxLength(50);
            entity.Property(mu => mu.UsedQuantity).IsRequired();
            entity.Property(mu => mu.SymptomNote).HasMaxLength(500);

            entity.HasOne(mu => mu.Medicine)
                  .WithMany(m => m.MedUsages)
                  .HasForeignKey(mu => mu.MedicineId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mu => mu.User)
                  .WithMany(u => u.MedUsages)
                  .HasForeignKey(mu => mu.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MedAlert 配置
        modelBuilder.Entity<MedAlert>(entity =>
        {
            entity.HasKey(ma => ma.Id);
            entity.Property(ma => ma.AlertType).IsRequired();
            entity.Property(ma => ma.Message).IsRequired().HasMaxLength(500);
            entity.Property(ma => ma.IsRead).HasDefaultValue(false);

            entity.HasOne(ma => ma.Medicine)
                  .WithMany(m => m.MedAlerts)
                  .HasForeignKey(ma => ma.MedicineId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ma => ma.User)
                  .WithMany(u => u.MedAlerts)
                  .HasForeignKey(ma => ma.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
