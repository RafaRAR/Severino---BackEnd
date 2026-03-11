using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class AddImagemUrlToCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Cadastros",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Cadastros");
        }
    }
}
