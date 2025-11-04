using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpentubeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFilenameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Filename",
                table: "MediaFiles",
                column: "Filename",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_Filename",
                table: "MediaFiles");
        }
    }
}
