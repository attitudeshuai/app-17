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
    public DbSet<ProcurementSuggestion> ProcurementSuggestions { get; set; }
    public DbSet<HealthProfile> HealthProfiles { get; set; }
    public DbSet<HealthProfileAuditLog> HealthProfileAuditLogs { get; set; }
    public DbSet<MedicineShare> MedicineShares { get; set; }
    public DbSet<SharedMedicine> SharedMedicines { get; set; }
    public DbSet<BorrowRequest> BorrowRequests { get; set; }
    public DbSet<BorrowRecord> BorrowRecords { get; set; }

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

        // ProcurementSuggestion 配置
        modelBuilder.Entity<ProcurementSuggestion>(entity =>
        {
            entity.HasKey(ps => ps.Id);
            entity.Property(ps => ps.SuggestedQuantity).IsRequired();
            entity.Property(ps => ps.SuggestedPurchaseDate).IsRequired();
            entity.Property(ps => ps.UrgencyLevel).IsRequired();
            entity.Property(ps => ps.Status).IsRequired();
            entity.Property(ps => ps.UsageFrequency).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(ps => ps.CurrentStock).IsRequired();
            entity.Property(ps => ps.DaysUntilExpiry).IsRequired();
            entity.Property(ps => ps.Notes).HasMaxLength(500);
            entity.Property(ps => ps.PurchasedQuantity);

            entity.HasOne(ps => ps.Household)
                  .WithMany(h => h.ProcurementSuggestions)
                  .HasForeignKey(ps => ps.HouseholdId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ps => ps.Medicine)
                  .WithMany(m => m.ProcurementSuggestions)
                  .HasForeignKey(ps => ps.MedicineId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ps => ps.User)
                  .WithMany(u => u.ProcurementSuggestions)
                  .HasForeignKey(ps => ps.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // HealthProfile 配置
        modelBuilder.Entity<HealthProfile>(entity =>
        {
            entity.HasKey(hp => hp.Id);
            entity.HasIndex(hp => new { hp.HouseholdId, hp.UserId }).IsUnique();
            entity.Property(hp => hp.FullName).IsRequired().HasMaxLength(100);
            entity.Property(hp => hp.Gender).HasMaxLength(10);
            entity.Property(hp => hp.BloodType).IsRequired();
            entity.Property(hp => hp.HeightCm).HasColumnType("decimal(5,2)");
            entity.Property(hp => hp.WeightKg).HasColumnType("decimal(5,2)");
            entity.Property(hp => hp.Allergies).HasMaxLength(1000);
            entity.Property(hp => hp.ChronicDiseases).HasMaxLength(1000);
            entity.Property(hp => hp.Medications).HasMaxLength(1000);
            entity.Property(hp => hp.MedicalHistory).HasMaxLength(2000);
            entity.Property(hp => hp.EmergencyContact).HasMaxLength(100);
            entity.Property(hp => hp.EmergencyPhone).HasMaxLength(30);
            entity.Property(hp => hp.Notes).HasMaxLength(2000);

            entity.HasOne(hp => hp.User)
                  .WithMany()
                  .HasForeignKey(hp => hp.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(hp => hp.Household)
                  .WithMany()
                  .HasForeignKey(hp => hp.HouseholdId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // HealthProfileAuditLog 配置
        modelBuilder.Entity<HealthProfileAuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.Property(log => log.ModifiedByUsername).IsRequired().HasMaxLength(50);
            entity.Property(log => log.ChangeType).IsRequired().HasMaxLength(20);
            entity.Property(log => log.FieldName).HasMaxLength(50);
            entity.Property(log => log.OldValue).HasMaxLength(2000);
            entity.Property(log => log.NewValue).HasMaxLength(2000);

            entity.HasOne(log => log.HealthProfile)
                  .WithMany(hp => hp.AuditLogs)
                  .HasForeignKey(log => log.HealthProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // MedicineShare 配置
        modelBuilder.Entity<MedicineShare>(entity =>
        {
            entity.HasKey(ms => ms.Id);
            entity.HasIndex(ms => ms.InviteCode).IsUnique();
            entity.Property(ms => ms.InviteCode).IsRequired().HasMaxLength(20);
            entity.Property(ms => ms.Status).IsRequired();
            entity.Property(ms => ms.Notes).HasMaxLength(500);

            entity.HasOne(ms => ms.LenderHousehold)
                  .WithMany()
                  .HasForeignKey(ms => ms.LenderHouseholdId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ms => ms.BorrowerHousehold)
                  .WithMany()
                  .HasForeignKey(ms => ms.BorrowerHouseholdId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ms => ms.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(ms => ms.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ms => ms.RevokedByUser)
                  .WithMany()
                  .HasForeignKey(ms => ms.RevokedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SharedMedicine 配置
        modelBuilder.Entity<SharedMedicine>(entity =>
        {
            entity.HasKey(sm => sm.Id);
            entity.HasIndex(sm => new { sm.MedicineShareId, sm.MedicineId }).IsUnique();
            entity.Property(sm => sm.IsActive).HasDefaultValue(true);

            entity.HasOne(sm => sm.MedicineShare)
                  .WithMany(ms => ms.SharedMedicines)
                  .HasForeignKey(sm => sm.MedicineShareId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sm => sm.Medicine)
                  .WithMany()
                  .HasForeignKey(sm => sm.MedicineId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BorrowRequest 配置
        modelBuilder.Entity<BorrowRequest>(entity =>
        {
            entity.HasKey(br => br.Id);
            entity.Property(br => br.RequestedQuantity).IsRequired();
            entity.Property(br => br.Purpose).HasMaxLength(500);
            entity.Property(br => br.Status).IsRequired();
            entity.Property(br => br.RejectionReason).HasMaxLength(500);
            entity.Property(br => br.ExpectedReturnDate).IsRequired();

            entity.HasOne(br => br.MedicineShare)
                  .WithMany(ms => ms.BorrowRequests)
                  .HasForeignKey(br => br.MedicineShareId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.Medicine)
                  .WithMany()
                  .HasForeignKey(br => br.MedicineId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.RequesterUser)
                  .WithMany()
                  .HasForeignKey(br => br.RequesterUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.ApprovedByUser)
                  .WithMany()
                  .HasForeignKey(br => br.ApprovedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // BorrowRecord 配置
        modelBuilder.Entity<BorrowRecord>(entity =>
        {
            entity.HasKey(br => br.Id);
            entity.Property(br => br.BorrowedQuantity).IsRequired();
            entity.Property(br => br.Status).IsRequired();
            entity.Property(br => br.Notes).HasMaxLength(500);
            entity.Property(br => br.BorrowedAt).IsRequired();
            entity.Property(br => br.ExpectedReturnDate).IsRequired();
            entity.Property(br => br.ReminderSent).HasDefaultValue(false);

            entity.HasOne(br => br.MedicineShare)
                  .WithMany(ms => ms.BorrowRecords)
                  .HasForeignKey(br => br.MedicineShareId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.BorrowRequest)
                  .WithOne(r => r.BorrowRecord)
                  .HasForeignKey<BorrowRecord>(br => br.BorrowRequestId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.Medicine)
                  .WithMany()
                  .HasForeignKey(br => br.MedicineId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.LenderHousehold)
                  .WithMany()
                  .HasForeignKey(br => br.LenderHouseholdId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.BorrowerHousehold)
                  .WithMany()
                  .HasForeignKey(br => br.BorrowerHouseholdId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(br => br.BorrowerUser)
                  .WithMany()
                  .HasForeignKey(br => br.BorrowerUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
