using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class MovendoOTipoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Adiciona a coluna no Cadastros
            migrationBuilder.AddColumn<int>(
                name: "TipoUsuario",
                table: "Cadastros",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // 2. Migra os dados do Usuario para o Cadastro
            migrationBuilder.Sql(@"
        UPDATE ""Cadastros"" c
        SET ""TipoUsuario"" = u.""TipoUsuario""
        FROM ""Usuarios"" u
        WHERE c.""UsuarioId"" = u.""Id"";
    ");

            // 3. Remove a coluna do Usuarios
            migrationBuilder.DropColumn(
                name: "TipoUsuario",
                table: "Usuarios");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoUsuario",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
        UPDATE ""Usuarios"" u
        SET ""TipoUsuario"" = c.""TipoUsuario""
        FROM ""Cadastros"" c
        WHERE c.""UsuarioId"" = u.""Id"";
    ");

            migrationBuilder.DropColumn(
                name: "TipoUsuario",
                table: "Cadastros");
        }
    }
}
