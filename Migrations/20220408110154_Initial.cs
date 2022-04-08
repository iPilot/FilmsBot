using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FilmsBot.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PARTICIPANTS",
                columns: table => new
                {
                    ID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    JOINED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTICIPANTS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "FILMS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NAME = table.Column<string>(type: "citext", nullable: false),
                    COMMENT = table.Column<string>(type: "text", nullable: true),
                    YEAR = table.Column<int>(type: "integer", nullable: true),
                    ADDED_BY_ID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GUILD_ID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ADDED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FILMS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FILMS_PARTICIPANTS_ADDED_BY_ID",
                        column: x => x.ADDED_BY_ID,
                        principalTable: "PARTICIPANTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RATINGS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PARTICIPANT_ID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FILM_ID = table.Column<long>(type: "bigint", nullable: false),
                    RATING = table.Column<double>(type: "double precision", nullable: false),
                    DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RATINGS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FILM_RATING_FILM_ID",
                        column: x => x.FILM_ID,
                        principalTable: "FILMS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FILM_RATING_PARTICIPANT_ID",
                        column: x => x.PARTICIPANT_ID,
                        principalTable: "PARTICIPANTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SESSIONS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    START = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    END = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FILM_ID = table.Column<long>(type: "bigint", nullable: true),
                    GUILD_ID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CREATED_BY = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SESSIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FILM_SESSION_CREATOR_ID",
                        column: x => x.CREATED_BY,
                        principalTable: "PARTICIPANTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FILM_SESSION_FILM_ID",
                        column: x => x.FILM_ID,
                        principalTable: "FILMS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VOTES",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FILM_ID = table.Column<long>(type: "bigint", nullable: false),
                    PARTICIPANT_ID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    SESSION_ID = table.Column<long>(type: "bigint", nullable: false),
                    AMOUNT = table.Column<int>(type: "integer", nullable: false),
                    DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VOTES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FILM_VOTE_FILM_ID",
                        column: x => x.FILM_ID,
                        principalTable: "FILMS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FILM_VOTE_PARTICIPANT_ID",
                        column: x => x.PARTICIPANT_ID,
                        principalTable: "PARTICIPANTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FILM_VOTE_SESSION_ID",
                        column: x => x.SESSION_ID,
                        principalTable: "SESSIONS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FILMS_ADDED_BY_ID",
                table: "FILMS",
                column: "ADDED_BY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RATINGS_FILM_ID",
                table: "RATINGS",
                column: "FILM_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RATINGS_PARTICIPANT_ID",
                table: "RATINGS",
                column: "PARTICIPANT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SESSIONS_CREATED_BY",
                table: "SESSIONS",
                column: "CREATED_BY");

            migrationBuilder.CreateIndex(
                name: "IX_SESSIONS_FILM_ID",
                table: "SESSIONS",
                column: "FILM_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VOTES_FILM_ID",
                table: "VOTES",
                column: "FILM_ID");

            migrationBuilder.CreateIndex(
                name: "IX_VOTES_PARTICIPANT_ID",
                table: "VOTES",
                column: "PARTICIPANT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_VOTES_SESSION_ID",
                table: "VOTES",
                column: "SESSION_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RATINGS");

            migrationBuilder.DropTable(
                name: "VOTES");

            migrationBuilder.DropTable(
                name: "SESSIONS");

            migrationBuilder.DropTable(
                name: "FILMS");

            migrationBuilder.DropTable(
                name: "PARTICIPANTS");
        }
    }
}
