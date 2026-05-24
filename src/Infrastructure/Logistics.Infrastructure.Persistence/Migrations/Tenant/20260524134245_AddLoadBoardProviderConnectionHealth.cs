using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistics.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddLoadBoardProviderConnectionHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_connection_error",
                table: "load_board_configurations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_connection_tested_at",
                table: "load_board_configurations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_connection_error",
                table: "load_board_configurations");

            migrationBuilder.DropColumn(
                name: "last_connection_tested_at",
                table: "load_board_configurations");
        }
    }
}
