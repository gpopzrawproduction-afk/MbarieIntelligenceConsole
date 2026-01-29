using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessTokenEncrypted = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    RefreshTokenEncrypted = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GrantedScopes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalEmailsSynced = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAttachmentsSynced = table.Column<int>(type: "INTEGER", nullable: false),
                    DeltaLink = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    HistoryId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastSyncError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialSyncDays = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncAttachments = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxAttachmentSizeMB = table.Column<int>(type: "INTEGER", nullable: false),
                    FoldersToSync = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    StorageUsedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    UnreadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresResponseCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FromAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FromName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ToRecipients = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CcRecipients = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    BccRecipients = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BodyText = table.Column<string>(type: "TEXT", nullable: false),
                    BodyHtml = table.Column<string>(type: "TEXT", nullable: true),
                    BodyPreview = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFlagged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasAttachments = table.Column<bool>(type: "INTEGER", nullable: false),
                    Folder = table.Column<int>(type: "INTEGER", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    AIPriority = table.Column<int>(type: "INTEGER", nullable: false),
                    AICategory = table.Column<int>(type: "INTEGER", nullable: false),
                    Sentiment = table.Column<int>(type: "INTEGER", nullable: false),
                    ContainsActionItems = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresResponse = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuggestedResponseBy = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AISummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExtractedKeywords = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ActionItems = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    AIConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    IsAIProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    AIProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InReplyTo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    KnowledgeEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SizeInBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EmailMessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ProcessingError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AISummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExtractedKeywords = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DocumentCategory = table.Column<int>(type: "INTEGER", nullable: true),
                    ClassificationConfidence = table.Column<double>(type: "REAL", nullable: true),
                    KnowledgeEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EmbeddingId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsIndexed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_EmailMessages_EmailMessageId",
                        column: x => x.EmailMessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_EmailAddress",
                table: "EmailAccounts",
                column: "EmailAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_IsActive",
                table: "EmailAccounts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_IsPrimary",
                table: "EmailAccounts",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_LastSyncedAt",
                table: "EmailAccounts",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_Provider",
                table: "EmailAccounts",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_Status",
                table: "EmailAccounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId",
                table: "EmailAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId_EmailAddress",
                table: "EmailAccounts",
                columns: new[] { "UserId", "EmailAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId_IsActive",
                table: "EmailAccounts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccounts_UserId_IsPrimary",
                table: "EmailAccounts",
                columns: new[] { "UserId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailMessageId",
                table: "EmailAttachments",
                column: "EmailMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailMessageId_Type",
                table: "EmailAttachments",
                columns: new[] { "EmailMessageId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_IsIndexed",
                table: "EmailAttachments",
                column: "IsIndexed");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_IsProcessed",
                table: "EmailAttachments",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_KnowledgeEntryId",
                table: "EmailAttachments",
                column: "KnowledgeEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_Status",
                table: "EmailAttachments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_Type",
                table: "EmailAttachments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_AICategory",
                table: "EmailMessages",
                column: "AICategory");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_AIPriority",
                table: "EmailMessages",
                column: "AIPriority");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ConversationId",
                table: "EmailMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_EmailAccountId",
                table: "EmailMessages",
                column: "EmailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_EmailAccountId_ReceivedDate",
                table: "EmailMessages",
                columns: new[] { "EmailAccountId", "ReceivedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Folder",
                table: "EmailMessages",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_FromAddress",
                table: "EmailMessages",
                column: "FromAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_IsRead",
                table: "EmailMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_MessageId",
                table: "EmailMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ReceivedDate",
                table: "EmailMessages",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_RequiresResponse",
                table: "EmailMessages",
                column: "RequiresResponse");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_SentDate",
                table: "EmailMessages",
                column: "SentDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId",
                table: "EmailMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_IsRead_Folder",
                table: "EmailMessages",
                columns: new[] { "UserId", "IsRead", "Folder" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_ReceivedDate",
                table: "EmailMessages",
                columns: new[] { "UserId", "ReceivedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_RequiresResponse",
                table: "EmailMessages",
                columns: new[] { "UserId", "RequiresResponse" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAccounts");

            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "EmailMessages");
        }
    }
}
