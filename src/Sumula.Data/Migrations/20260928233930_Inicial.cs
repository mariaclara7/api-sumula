using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sumula.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "artilheiros",
                columns: table => new
                {
                    competicao = table.Column<string>(type: "text", nullable: false),
                    temporada = table.Column<int>(type: "integer", nullable: false),
                    jogador_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    time_id = table.Column<int>(type: "integer", nullable: false),
                    jogos = table.Column<int>(type: "integer", nullable: true),
                    gols = table.Column<int>(type: "integer", nullable: false),
                    assistencias = table.Column<int>(type: "integer", nullable: true),
                    penaltis = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artilheiros", x => new { x.competicao, x.temporada, x.jogador_id });
                });

            migrationBuilder.CreateTable(
                name: "coletas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    competicao = table.Column<string>(type: "text", nullable: false),
                    temporada = table.Column<int>(type: "integer", nullable: false),
                    executada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    partidas_atualizadas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coletas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "times",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    nome_curto = table.Column<string>(type: "text", nullable: false),
                    sigla = table.Column<string>(type: "text", nullable: false),
                    escudo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_times", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participacoes",
                columns: table => new
                {
                    competicao = table.Column<string>(type: "text", nullable: false),
                    temporada = table.Column<int>(type: "integer", nullable: false),
                    time_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participacoes", x => new { x.competicao, x.temporada, x.time_id });
                    table.ForeignKey(
                        name: "fk_participacoes_times_time_id",
                        column: x => x.time_id,
                        principalTable: "times",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "partidas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    competicao = table.Column<string>(type: "text", nullable: false),
                    temporada = table.Column<int>(type: "integer", nullable: false),
                    rodada = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    mandante_id = table.Column<int>(type: "integer", nullable: false),
                    visitante_id = table.Column<int>(type: "integer", nullable: false),
                    gols_mandante = table.Column<int>(type: "integer", nullable: true),
                    gols_visitante = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partidas", x => x.id);
                    table.ForeignKey(
                        name: "fk_partidas_times_mandante_id",
                        column: x => x.mandante_id,
                        principalTable: "times",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_partidas_times_visitante_id",
                        column: x => x.visitante_id,
                        principalTable: "times",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coletas_competicao_temporada_executada_em",
                table: "coletas",
                columns: new[] { "competicao", "temporada", "executada_em" });

            migrationBuilder.CreateIndex(
                name: "ix_participacoes_time_id",
                table: "participacoes",
                column: "time_id");

            migrationBuilder.CreateIndex(
                name: "ix_partidas_competicao_temporada",
                table: "partidas",
                columns: new[] { "competicao", "temporada" });

            migrationBuilder.CreateIndex(
                name: "ix_partidas_mandante_id",
                table: "partidas",
                column: "mandante_id");

            migrationBuilder.CreateIndex(
                name: "ix_partidas_visitante_id",
                table: "partidas",
                column: "visitante_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artilheiros");

            migrationBuilder.DropTable(
                name: "coletas");

            migrationBuilder.DropTable(
                name: "participacoes");

            migrationBuilder.DropTable(
                name: "partidas");

            migrationBuilder.DropTable(
                name: "times");
        }
    }
}
