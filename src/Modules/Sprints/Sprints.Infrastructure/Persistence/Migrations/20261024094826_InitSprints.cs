using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sprints.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitSprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sprints");

            migrationBuilder.CreateTable(
                name: "sprints",
                schema: "sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Goal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sprint_tasks",
                schema: "sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprint_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sprint_tasks_sprints_SprintId",
                        column: x => x.SprintId,
                        principalSchema: "sprints",
                        principalTable: "sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sprint_tasks_SprintId_TaskId",
                schema: "sprints",
                table: "sprint_tasks",
                columns: new[] { "SprintId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sprints_ProjectId_Active",
                schema: "sprints",
                table: "sprints",
                column: "ProjectId",
                unique: true,
                filter: "\"Status\" = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sprint_tasks",
                schema: "sprints");

            migrationBuilder.DropTable(
                name: "sprints",
                schema: "sprints");
        }
    }
}
