using System;
using System.Collections.Generic;
using ImageViewer.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageViewer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_Format",
                schema: "public",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Metadata",
                schema: "public",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Settings",
                schema: "public",
                table: "Collections");

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                schema: "public",
                table: "Images",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                schema: "public",
                table: "Images",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId1",
                schema: "public",
                table: "ImageCacheInfos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedItems",
                schema: "public",
                table: "BackgroundJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrentItem",
                schema: "public",
                table: "BackgroundJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Errors",
                schema: "public",
                table: "BackgroundJobs",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedCompletion",
                schema: "public",
                table: "BackgroundJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                schema: "public",
                table: "BackgroundJobs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollectionSettings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalImages = table.Column<int>(type: "integer", nullable: false),
                    TotalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ThumbnailWidth = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailHeight = table.Column<int>(type: "integer", nullable: false),
                    CacheWidth = table.Column<int>(type: "integer", nullable: false),
                    CacheHeight = table.Column<int>(type: "integer", nullable: false),
                    AutoGenerateThumbnails = table.Column<bool>(type: "boolean", nullable: false),
                    AutoGenerateCache = table.Column<bool>(type: "boolean", nullable: false),
                    CacheExpiration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    AdditionalSettingsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionSettings_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "public",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageMetadata",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quality = table.Column<int>(type: "integer", nullable: false),
                    ColorSpace = table.Column<string>(type: "text", nullable: true),
                    Compression = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Camera = table.Column<string>(type: "text", nullable: true),
                    Software = table.Column<string>(type: "text", nullable: true),
                    AdditionalMetadataJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageMetadata_Images_ImageId",
                        column: x => x.ImageId,
                        principalSchema: "public",
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageCacheInfos_ImageId1",
                schema: "public",
                table: "ImageCacheInfos",
                column: "ImageId1");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionSettings_CollectionId",
                schema: "public",
                table: "CollectionSettings",
                column: "CollectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadata_ImageId",
                schema: "public",
                table: "ImageMetadata",
                column: "ImageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageCacheInfos_Images_ImageId1",
                schema: "public",
                table: "ImageCacheInfos",
                column: "ImageId1",
                principalSchema: "public",
                principalTable: "Images",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageCacheInfos_Images_ImageId1",
                schema: "public",
                table: "ImageCacheInfos");

            migrationBuilder.DropTable(
                name: "CollectionSettings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ImageMetadata",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_ImageCacheInfos_ImageId1",
                schema: "public",
                table: "ImageCacheInfos");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                schema: "public",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ImageId1",
                schema: "public",
                table: "ImageCacheInfos");

            migrationBuilder.DropColumn(
                name: "CompletedItems",
                schema: "public",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "CurrentItem",
                schema: "public",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "Errors",
                schema: "public",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "EstimatedCompletion",
                schema: "public",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "Message",
                schema: "public",
                table: "BackgroundJobs");

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                schema: "public",
                table: "Images",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<ImageMetadata>(
                name: "Metadata",
                schema: "public",
                table: "Images",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<CollectionSettings>(
                name: "Settings",
                schema: "public",
                table: "Collections",
                type: "jsonb",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Images_Format",
                schema: "public",
                table: "Images",
                column: "Format");
        }
    }
}
