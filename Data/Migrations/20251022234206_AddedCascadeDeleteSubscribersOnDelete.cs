using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Podcast_MVC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedCascadeDeleteSubscribersOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_AspNetUsers_UserID",
                table: "Subscriptions");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_AspNetUsers_UserID",
                table: "Subscriptions",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_AspNetUsers_UserID",
                table: "Subscriptions");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_AspNetUsers_UserID",
                table: "Subscriptions",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
