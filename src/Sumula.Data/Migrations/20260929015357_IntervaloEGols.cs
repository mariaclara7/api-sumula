using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sumula.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntervaloEGols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "gols_mandante_intervalo",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "gols_visitante_intervalo",
                table: "partidas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gols",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    partida_id = table.Column<int>(type: "integer", nullable: false),
                    minuto = table.Column<int>(type: "integer", nullable: false),
                    acrescimo = table.Column<int>(type: "integer", nullable: true),
                    time_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    autor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gols", x => x.id);
                    table.ForeignKey(
                        name: "fk_gols_partidas_partida_id",
                        column: x => x.partida_id,
                        principalTable: "partidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gols_partida_id",
                table: "gols",
                column: "partida_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gols");

            migrationBuilder.DropColumn(
                name: "gols_mandante_intervalo",
                table: "partidas");

            migrationBuilder.DropColumn(
                name: "gols_visitante_intervalo",
                table: "partidas");
        }
    }
}
