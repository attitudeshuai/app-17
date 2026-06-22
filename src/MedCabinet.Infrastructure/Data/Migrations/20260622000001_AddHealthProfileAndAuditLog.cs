using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MedCabinet.Infrastructure.Data.Migrations;

public partial class AddHealthProfileAndAuditLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HealthProfiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<int>(type: "int", nullable: false),
                HouseholdId = table.Column<int>(type: "int", nullable: false),
                FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                Age = table.Column<int>(type: "int", nullable: true),
                Gender = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                BloodType = table.Column<int>(type: "int", nullable: false),
                HeightCm = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                WeightKg = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                Allergies = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                ChronicDiseases = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                Medications = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                MedicalHistory = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                EmergencyContact = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                EmergencyPhone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HealthProfiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_HealthProfiles_Households_HouseholdId",
                    column: x => x.HouseholdId,
                    principalTable: "Households",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_HealthProfiles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "HealthProfileAuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                HealthProfileId = table.Column<int>(type: "int", nullable: false),
                ModifiedByUserId = table.Column<int>(type: "int", nullable: false),
                ModifiedByUsername = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                ChangeType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                FieldName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                OldValue = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                NewValue = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                ModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HealthProfileAuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_HealthProfileAuditLogs_HealthProfiles_HealthProfileId",
                    column: x => x.HealthProfileId,
                    principalTable: "HealthProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_HealthProfiles_HouseholdId_UserId",
            table: "HealthProfiles",
            columns: new[] { "HouseholdId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HealthProfiles_HouseholdId",
            table: "HealthProfiles",
            column: "HouseholdId");

        migrationBuilder.CreateIndex(
            name: "IX_HealthProfiles_UserId",
            table: "HealthProfiles",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_HealthProfileAuditLogs_HealthProfileId",
            table: "HealthProfileAuditLogs",
            column: "HealthProfileId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HealthProfileAuditLogs");

        migrationBuilder.DropTable(
            name: "HealthProfiles");
    }
}
