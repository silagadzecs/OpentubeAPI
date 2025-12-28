using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpentubeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshIp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceIp",
                table: "UserRefreshTokens",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceIp",
                table: "UserRefreshTokens");
        }
    }
}
