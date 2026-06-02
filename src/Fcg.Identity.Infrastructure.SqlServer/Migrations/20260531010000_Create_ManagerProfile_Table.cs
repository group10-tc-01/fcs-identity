using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fcg.Identity.Infrastructure.SqlServer.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class Create_ManagerProfile_Table : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ManagerProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                KeycloakUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ManagerProfiles", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "UX_ManagerProfiles_Email",
            table: "ManagerProfiles",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_ManagerProfiles_KeycloakUserId",
            table: "ManagerProfiles",
            column: "KeycloakUserId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ManagerProfiles");
    }
}
