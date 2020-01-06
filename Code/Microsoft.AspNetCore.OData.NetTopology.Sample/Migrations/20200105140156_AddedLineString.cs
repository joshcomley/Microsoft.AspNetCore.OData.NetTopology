using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace WashingtonSchools.Api.Migrations
{
    public partial class AddedLineString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<LineString>(
                name: "Line",
                table: "Schools",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Line",
                table: "Schools");
        }
    }
}
