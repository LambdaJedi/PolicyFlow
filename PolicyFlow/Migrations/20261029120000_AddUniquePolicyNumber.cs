using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PolicyFlow.Data;

#nullable disable

namespace PolicyFlow.Migrations
{
    /// <summary>
    /// Limits PolicyNumber to 50 characters (SQL Server can't index nvarchar(max))
    /// and adds a unique index on it.
    /// </summary>
    [DbContext(typeof(PolicyFlowDbContext))]
    [Migration("20261029120000_AddUniquePolicyNumber")]
    public partial class AddUniquePolicyNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PolicyNumber",
                table: "Policies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyNumber",
                table: "Policies",
                column: "PolicyNumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Policies_PolicyNumber",
                table: "Policies");

            migrationBuilder.AlterColumn<string>(
                name: "PolicyNumber",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
