using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpentubeAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserProfilePictureColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePicturePath",
                table: "Users",
                newName: "ProfilePicture");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePicture",
                table: "Users",
                newName: "ProfilePicturePath");
        }
    }
}
