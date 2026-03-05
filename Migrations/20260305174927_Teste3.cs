using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class Teste3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "Usuarios",
                newName: "EmailConfirmado");

            migrationBuilder.AddColumn<string>(
                name: "CodigoResetSenha",
                table: "Usuarios",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiracaoResetSenha",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoResetSenha",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ExpiracaoResetSenha",
                table: "Usuarios");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmado",
                table: "Usuarios",
                newName: "EmailConfirmed");
        }
    }
}
