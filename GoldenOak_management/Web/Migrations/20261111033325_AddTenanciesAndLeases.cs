using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTenanciesAndLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenancies",
                columns: table => new
                {
                    tenancy_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    lease_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    lease_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    monthly_rent = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    deposit_amount = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    deposit_received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenancies", x => x.tenancy_id);
                    table.ForeignKey(
                        name: "FK_tenancies_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "unit_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "leases",
                columns: table => new
                {
                    lease_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tenancy_id = table.Column<long>(type: "bigint", nullable: false),
                    lease_form_data = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signed_document_id = table.Column<long>(type: "bigint", nullable: true),
                    signed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leases", x => x.lease_id);
                    table.ForeignKey(
                        name: "FK_leases_tenancies_tenancy_id",
                        column: x => x.tenancy_id,
                        principalTable: "tenancies",
                        principalColumn: "tenancy_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenancy_renters",
                columns: table => new
                {
                    tenancy_id = table.Column<long>(type: "bigint", nullable: false),
                    renter_id = table.Column<long>(type: "bigint", nullable: false),
                    is_primary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenancy_renters", x => new { x.tenancy_id, x.renter_id });
                    table.ForeignKey(
                        name: "FK_tenancy_renters_tenancies_tenancy_id",
                        column: x => x.tenancy_id,
                        principalTable: "tenancies",
                        principalColumn: "tenancy_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenancy_renters_users_renter_id",
                        column: x => x.renter_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_leases_tenancy_id",
                table: "leases",
                column: "tenancy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenancies_unit_id",
                table: "tenancies",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenancy_renters_renter_id",
                table: "tenancy_renters",
                column: "renter_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leases");

            migrationBuilder.DropTable(
                name: "tenancy_renters");

            migrationBuilder.DropTable(
                name: "tenancies");
        }
    }
}
