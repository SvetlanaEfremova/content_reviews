using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace course_project.Migrations
{
    public partial class LikesAddedToUserModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Likes",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Likes",
                table: "AspNetUsers");
        }
    }
}
