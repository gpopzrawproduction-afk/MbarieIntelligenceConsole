using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SeniorityLevel",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobPosition",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImapPort",
                table: "EmailAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImapServer",
                table: "EmailAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordEncrypted",
                table: "EmailAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "EmailAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SmtpServer",
                table: "EmailAccounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseSsl",
                table: "EmailAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_CreatedAt",
                table: "EmailMessages",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CreatedAt",
                table: "Alerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Severity_Status",
                table: "Alerts",
                columns: new[] { "Severity", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_UserId_CreatedAt",
                table: "EmailMessages");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_CreatedAt",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_Severity_Status",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ImapPort",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "ImapServer",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "PasswordEncrypted",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "SmtpServer",
                table: "EmailAccounts");

            migrationBuilder.DropColumn(
                name: "UseSsl",
                table: "EmailAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "SeniorityLevel",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobPosition",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
