using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace RetroScan.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "research_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_sessions", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Text = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    SearchQuery = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    AskedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_research_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "research_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false),
                    Excerpt = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sources_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_questions_SessionId",
                table: "questions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_research_sessions_OwnerId_UpdatedAt",
                table: "research_sessions",
                columns: new[] { "OwnerId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sources_QuestionId_Number",
                table: "sources",
                columns: new[] { "QuestionId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sources");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "research_sessions");
        }
    }
}
