using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageViewer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureRowVersionForAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "ViewSessions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "Tags",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "Images",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "ImageMetadata",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "ImageCacheInfos",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "CollectionTags",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "CollectionStatistics",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "CollectionSettings",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "Collections",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                comment: "Row version for optimistic concurrency control",
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "CollectionCacheBindings",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "CacheFolders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "BackgroundJobs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Row version for optimistic concurrency control");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "ViewSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "ImageMetadata");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "ImageCacheInfos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "CollectionTags");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "CollectionStatistics");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "CollectionSettings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "CollectionCacheBindings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "CacheFolders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "public",
                table: "BackgroundJobs");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                schema: "public",
                table: "Collections",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true,
                oldComment: "Row version for optimistic concurrency control");
        }
    }
}
