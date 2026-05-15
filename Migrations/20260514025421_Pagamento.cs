using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class Pagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pagamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatRoomId = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    PrestadorId = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxaPlataforma = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorLiquido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StripeTransferId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StripeContaPrestadorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ClienteSolicitouCancelamento = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PrestadorSolicitouCancelamento = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DataPagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataLiberacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataCancelamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrazoLimiteCancelamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagamentos_ChatRooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "ChatRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagamentos_Usuarios_PrestadorId",
                        column: x => x.PrestadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ChatRoomId",
                table: "Pagamentos",
                column: "ChatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ClienteId",
                table: "Pagamentos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_PrestadorId",
                table: "Pagamentos",
                column: "PrestadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagamentos");
        }
    }
}
