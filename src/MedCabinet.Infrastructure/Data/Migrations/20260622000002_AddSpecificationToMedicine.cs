using Microsoft.EntityFrameworkCore.Migrations;

namespace MedCabinet.Infrastructure.Data.Migrations;

public partial class AddSpecificationToMedicine : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Specification",
            table: "Medicines",
            type: "varchar(200)",
            maxLength: 200,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Specification",
            table: "Medicines");
    }
}
