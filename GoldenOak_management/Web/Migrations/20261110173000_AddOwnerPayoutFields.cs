using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Migrations;

public partial class AddOwnerPayoutFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "account_holder_name",
            table: "owner_profiles",
            type: "varchar(150)",
            maxLength: 150,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "account_last_four",
            table: "owner_profiles",
            type: "varchar(4)",
            maxLength: 4,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "bank_name",
            table: "owner_profiles",
            type: "varchar(150)",
            maxLength: 150,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<bool>(
            name: "is_primary_payout_owner",
            table: "unit_owners",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "account_holder_name", table: "owner_profiles");
        migrationBuilder.DropColumn(name: "account_last_four", table: "owner_profiles");
        migrationBuilder.DropColumn(name: "bank_name", table: "owner_profiles");
        migrationBuilder.DropColumn(name: "is_primary_payout_owner", table: "unit_owners");
    }
}
