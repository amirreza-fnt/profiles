using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfileService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SsoFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SsoLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SsoFatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SsoGender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SsoBirthDate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SsoEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SsoProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SsoCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SsoPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SsoSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserBirthDate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AvatarFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AvatarShortCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PhotoFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PhotoShortCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.NationalCode);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Profiles_NationalCode",
                        column: x => x.NationalCode,
                        principalTable: "Profiles",
                        principalColumn: "NationalCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_Profiles_NationalCode",
                        column: x => x.NationalCode,
                        principalTable: "Profiles",
                        principalColumn: "NationalCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationChallenges_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_NationalCode_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "NationalCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_NationalCode_Type_Number",
                table: "Contacts",
                columns: new[] { "NationalCode", "Type", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Number",
                table: "Contacts",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationChallenges_ContactId_ConsumedAtUtc_ExpiresAtUtc",
                table: "VerificationChallenges",
                columns: new[] { "ContactId", "ConsumedAtUtc", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "VerificationChallenges");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "Profiles");
        }
    }
}
