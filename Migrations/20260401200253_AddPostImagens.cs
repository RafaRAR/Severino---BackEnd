using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace APIseverino.Migrations
{
    /// <inheritdoc />
    public partial class AddPostImagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Cria a nova tabela PostImagens
            migrationBuilder.CreateTable(
                name: "PostImagens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    FileId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostImagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostImagens_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostImagens_PostId",
                table: "PostImagens",
                column: "PostId");

            // 2. Migra dados existentes: copia ImagemUrl/ImagemFileId para PostImagens
            migrationBuilder.Sql(@"
                INSERT INTO ""PostImagens"" (""PostId"", ""Url"", ""FileId"")
                SELECT ""Id"", ""ImagemUrl"", ""ImagemFileId""
                FROM ""Posts""
                WHERE ""ImagemUrl"" IS NOT NULL AND ""ImagemUrl"" <> '';
            ");

            // 3. Remove as colunas antigas da tabela Posts
            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ImagemFileId",
                table: "Posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recria as colunas antigas
            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagemFileId",
                table: "Posts",
                type: "text",
                nullable: true);

            // Restaura apenas a primeira imagem de cada post (rollback parcial)
            migrationBuilder.Sql(@"
                UPDATE ""Posts"" p
                SET
                    ""ImagemUrl"" = pi.""Url"",
                    ""ImagemFileId"" = pi.""FileId""
                FROM (
                    SELECT DISTINCT ON (""PostId"") ""PostId"", ""Url"", ""FileId""
                    FROM ""PostImagens""
                    ORDER BY ""PostId"", ""Id""
                ) pi
                WHERE p.""Id"" = pi.""PostId"";
            ");

            migrationBuilder.DropTable(name: "PostImagens");
        }
    }
}