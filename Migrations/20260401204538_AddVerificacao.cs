using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoUsuario",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "prestadorVerificado",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Verificacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    ImagemUrl = table.Column<string>(type: "text", nullable: false),
                    ImagemFileId = table.Column<string>(type: "text", nullable: true),
                    Situacao = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ApprovedById = table.Column<int>(type: "integer", nullable: true),
                    DataSolicitacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DataAvaliacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Verificacoes_Usuarios_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Verificacoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Verificacoes_ApprovedById",
                table: "Verificacoes",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Verificacoes_UsuarioId",
                table: "Verificacoes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Verificacoes");

            migrationBuilder.DropColumn(
                name: "TipoUsuario",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "prestadorVerificado",
                table: "Usuarios");
        }
    }
}
