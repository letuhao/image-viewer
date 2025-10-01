using System;
using ImageViewer.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageViewer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "BackgroundJobs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Parameters = table.Column<string>(type: "jsonb", nullable: true),
                    Result = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CacheFolders",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MaxSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CurrentSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CacheFolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Collections",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Settings = table.Column<CollectionSettings>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Color = table.Column<TagColor>(type: "jsonb", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectionCacheBindings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CacheFolderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionCacheBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionCacheBindings_CacheFolders_CacheFolderId",
                        column: x => x.CacheFolderId,
                        principalSchema: "public",
                        principalTable: "CacheFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionCacheBindings_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "public",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionStatistics",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalImages = table.Column<int>(type: "integer", nullable: false),
                    TotalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    AverageWidth = table.Column<int>(type: "integer", nullable: false),
                    AverageHeight = table.Column<int>(type: "integer", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionStatistics_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "public",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<ImageMetadata>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "public",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionTags",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionTags_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "public",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionTags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "public",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageCacheInfos",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CachePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Dimensions = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageCacheInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageCacheInfos_Images_ImageId",
                        column: x => x.ImageId,
                        principalSchema: "public",
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewSessions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentImageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Settings = table.Column<ViewSessionSettings>(type: "jsonb", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImagesViewed = table.Column<int>(type: "integer", nullable: false),
                    TotalViewTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViewSessions_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "public",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ViewSessions_Images_CurrentImageId",
                        column: x => x.CurrentImageId,
                        principalSchema: "public",
                        principalTable: "Images",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_CompletedAt",
                schema: "public",
                table: "BackgroundJobs",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_CreatedAt",
                schema: "public",
                table: "BackgroundJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_JobType",
                schema: "public",
                table: "BackgroundJobs",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_StartedAt",
                schema: "public",
                table: "BackgroundJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_Status",
                schema: "public",
                table: "BackgroundJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CacheFolders_IsActive",
                schema: "public",
                table: "CacheFolders",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CacheFolders_Name",
                schema: "public",
                table: "CacheFolders",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CacheFolders_Path",
                schema: "public",
                table: "CacheFolders",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_CacheFolders_Priority",
                schema: "public",
                table: "CacheFolders",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCacheBindings_CacheFolderId",
                schema: "public",
                table: "CollectionCacheBindings",
                column: "CacheFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCacheBindings_CollectionId",
                schema: "public",
                table: "CollectionCacheBindings",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCacheBindings_CollectionId_CacheFolderId",
                schema: "public",
                table: "CollectionCacheBindings",
                columns: new[] { "CollectionId", "CacheFolderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_CreatedAt",
                schema: "public",
                table: "Collections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_IsDeleted",
                schema: "public",
                table: "Collections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Name",
                schema: "public",
                table: "Collections",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Path",
                schema: "public",
                table: "Collections",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Type",
                schema: "public",
                table: "Collections",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionStatistics_CollectionId",
                schema: "public",
                table: "CollectionStatistics",
                column: "CollectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionStatistics_LastViewedAt",
                schema: "public",
                table: "CollectionStatistics",
                column: "LastViewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionStatistics_ViewCount",
                schema: "public",
                table: "CollectionStatistics",
                column: "ViewCount");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionTags_CollectionId",
                schema: "public",
                table: "CollectionTags",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionTags_CollectionId_TagId",
                schema: "public",
                table: "CollectionTags",
                columns: new[] { "CollectionId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionTags_TagId",
                schema: "public",
                table: "CollectionTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageCacheInfos_CachedAt",
                schema: "public",
                table: "ImageCacheInfos",
                column: "CachedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImageCacheInfos_ExpiresAt",
                schema: "public",
                table: "ImageCacheInfos",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImageCacheInfos_ImageId",
                schema: "public",
                table: "ImageCacheInfos",
                column: "ImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageCacheInfos_IsValid",
                schema: "public",
                table: "ImageCacheInfos",
                column: "IsValid");

            migrationBuilder.CreateIndex(
                name: "IX_Images_CollectionId",
                schema: "public",
                table: "Images",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_CollectionId_Filename",
                schema: "public",
                table: "Images",
                columns: new[] { "CollectionId", "Filename" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_Filename",
                schema: "public",
                table: "Images",
                column: "Filename");

            migrationBuilder.CreateIndex(
                name: "IX_Images_FileSize",
                schema: "public",
                table: "Images",
                column: "FileSize");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Format",
                schema: "public",
                table: "Images",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Height",
                schema: "public",
                table: "Images",
                column: "Height");

            migrationBuilder.CreateIndex(
                name: "IX_Images_IsDeleted",
                schema: "public",
                table: "Images",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Width",
                schema: "public",
                table: "Images",
                column: "Width");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                schema: "public",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UsageCount",
                schema: "public",
                table: "Tags",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSessions_CollectionId",
                schema: "public",
                table: "ViewSessions",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSessions_CurrentImageId",
                schema: "public",
                table: "ViewSessions",
                column: "CurrentImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSessions_EndedAt",
                schema: "public",
                table: "ViewSessions",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ViewSessions_StartedAt",
                schema: "public",
                table: "ViewSessions",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CollectionCacheBindings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CollectionStatistics",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CollectionTags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ImageCacheInfos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ViewSessions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CacheFolders",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Tags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Images",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Collections",
                schema: "public");
        }
    }
}
