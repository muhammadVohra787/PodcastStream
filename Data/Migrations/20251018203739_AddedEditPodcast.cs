using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Podcast_MVC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedEditPodcast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Podcasts_AspNetUsers_CreatorID",
                table: "Podcasts");

            migrationBuilder.AlterColumn<string>(
                name: "CreatorID",
                table: "Podcasts",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Podcasts_AspNetUsers_CreatorID",
                table: "Podcasts",
                column: "CreatorID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Podcasts_AspNetUsers_CreatorID",
                table: "Podcasts");

            migrationBuilder.AlterColumn<string>(
                name: "CreatorID",
                table: "Podcasts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Podcasts_AspNetUsers_CreatorID",
                table: "Podcasts",
                column: "CreatorID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
