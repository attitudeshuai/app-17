using MedCabinet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MedCabinet.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("MedCabinet.Domain.Entities.HealthProfile", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            b.Property<int>("Age")
                .HasColumnType("int");

            b.Property<string>("Allergies")
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            b.Property<int>("BloodType")
                .HasColumnType("int");

            b.Property<string>("ChronicDiseases")
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime(6)");

            b.Property<DateTime?>("DateOfBirth")
                .HasColumnType("datetime(6)");

            b.Property<string>("EmergencyContact")
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");

            b.Property<string>("EmergencyPhone")
                .HasMaxLength(30)
                .HasColumnType("varchar(30)");

            b.Property<string>("FullName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");

            b.Property<string>("Gender")
                .HasMaxLength(10)
                .HasColumnType("varchar(10)");

            b.Property<int>("HouseholdId")
                .HasColumnType("int");

            b.Property<decimal?>("HeightCm")
                .HasColumnType("decimal(5,2)");

            b.Property<string>("MedicalHistory")
                .HasMaxLength(2000)
                .HasColumnType("varchar(2000)");

            b.Property<string>("Medications")
                .HasMaxLength(1000)
                .HasColumnType("varchar(1000)");

            b.Property<string>("Notes")
                .HasMaxLength(2000)
                .HasColumnType("varchar(2000)");

            b.Property<DateTime>("UpdatedAt")
                .HasColumnType("datetime(6)");

            b.Property<int>("UserId")
                .HasColumnType("int");

            b.Property<decimal?>("WeightKg")
                .HasColumnType("decimal(5,2)");

            b.HasKey("Id");

            b.HasIndex("HouseholdId");

            b.HasIndex("UserId");

            b.HasIndex("HouseholdId", "UserId")
                .IsUnique();

            b.ToTable("HealthProfiles");
        });

        modelBuilder.Entity("MedCabinet.Domain.Entities.HealthProfileAuditLog", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            b.Property<string>("ChangeType")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("varchar(20)");

            b.Property<string>("FieldName")
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            b.Property<int>("HealthProfileId")
                .HasColumnType("int");

            b.Property<int>("ModifiedByUserId")
                .HasColumnType("int");

            b.Property<string>("ModifiedByUsername")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            b.Property<DateTime>("ModifiedAt")
                .HasColumnType("datetime(6)");

            b.Property<string>("NewValue")
                .HasMaxLength(2000)
                .HasColumnType("varchar(2000)");

            b.Property<string>("OldValue")
                .HasMaxLength(2000)
                .HasColumnType("varchar(2000)");

            b.HasKey("Id");

            b.HasIndex("HealthProfileId");

            b.ToTable("HealthProfileAuditLogs");
        });

        modelBuilder.Entity("MedCabinet.Domain.Entities.HealthProfile", b =>
        {
            b.HasOne("MedCabinet.Domain.Entities.Household", "Household")
                .WithMany()
                .HasForeignKey("HouseholdId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("MedCabinet.Domain.Entities.User", "User")
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Household");

            b.Navigation("User");
        });

        modelBuilder.Entity("MedCabinet.Domain.Entities.HealthProfileAuditLog", b =>
        {
            b.HasOne("MedCabinet.Domain.Entities.HealthProfile", "HealthProfile")
                .WithMany("AuditLogs")
                .HasForeignKey("HealthProfileId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("HealthProfile");
        });

        modelBuilder.Entity("MedCabinet.Domain.Entities.HealthProfile", b =>
        {
            b.Navigation("AuditLogs");
        });
#pragma warning restore 612, 618
    }
}
