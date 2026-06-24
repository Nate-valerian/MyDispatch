using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistics.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddAssignedDispatcherToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_dispatcher_id",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_assigned_dispatcher_id",
                table: "employees",
                column: "assigned_dispatcher_id");

            migrationBuilder.AddForeignKey(
                name: "fk_employees_employees_assigned_dispatcher_id",
                table: "employees",
                column: "assigned_dispatcher_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_employees_employees_assigned_dispatcher_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_assigned_dispatcher_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "assigned_dispatcher_id",
                table: "employees");
        }
    }
}
