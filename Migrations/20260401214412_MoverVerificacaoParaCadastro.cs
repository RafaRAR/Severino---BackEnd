using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class MoverVerificacaoParaCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Adiciona a coluna no Cadastros
            migrationBuilder.AddColumn<bool>(
                name: "prestadorVerificado",
                table: "Cadastros",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 2. Migra os dados do Usuario para o Cadastro
            migrationBuilder.Sql(@"
        UPDATE ""Cadastros"" c
        SET ""prestadorVerificado"" = u.""prestadorVerificado""
        FROM ""Usuarios"" u
        WHERE c.""UsuarioId"" = u.""Id"";
    ");

            // 3. Remove a coluna do Usuarios
            migrationBuilder.DropColumn(
                name: "prestadorVerificado",
                table: "Usuarios");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "prestadorVerificado",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
        UPDATE ""Usuarios"" u
        SET ""prestadorVerificado"" = c.""prestadorVerificado""
        FROM ""Cadastros"" c
        WHERE c.""UsuarioId"" = u.""Id"";
    ");

            migrationBuilder.DropColumn(
                name: "prestadorVerificado",
                table: "Cadastros");
        }
    }
}
