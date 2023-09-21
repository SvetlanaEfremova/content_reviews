using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace course_project.Migrations
{
    public partial class ReviewReactionOneToManyRelationProvided : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reactions_ReviewId",
                table: "Reactions",
                column: "ReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_Reviews_ReviewId",
                table: "Reactions",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_Reviews_ReviewId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_ReviewId",
                table: "Reactions");
        }
    }
}
