using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TaskManagement.Infrastructure.Persistence.Migrations;
public partial class AddTaskEstimation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(name: "EstimatedHours", schema: "tasks", table: "tasks", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ProgressPercent", schema: "tasks", table: "tasks", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.Sql("UPDATE tasks.tasks SET \"ProgressPercent\" = 100 WHERE \"Status\" = 'Done';");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EstimatedHours", schema: "tasks", table: "tasks");
        migrationBuilder.DropColumn(name: "ProgressPercent", schema: "tasks", table: "tasks");
    }
}
