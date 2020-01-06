using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace WashingtonSchools.Api.Migrations
{
    public partial class AddedPolygon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Polygon>(
                name: "Polygon",
                table: "Schools",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Polygon",
                table: "Schools");
        }
    }
}
