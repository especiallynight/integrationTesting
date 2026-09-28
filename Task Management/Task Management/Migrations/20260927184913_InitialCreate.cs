using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Task_Management.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskPriorities",
                columns: table => new
                {
                    IdPriority = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PriorityType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskPriorities", x => x.IdPriority);
                });

            migrationBuilder.CreateTable(
                name: "TaskStatuses",
                columns: table => new
                {
                    IdTaskStatus = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatusName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStatuses", x => x.IdTaskStatus);
                });

            migrationBuilder.CreateTable(
                name: "CurrentTasks",
                columns: table => new
                {
                    task_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    task_name = table.Column<string>(type: "text", nullable: true),
                    task_description = table.Column<string>(type: "text", nullable: true),
                    dateadded = table.Column<DateOnly>(type: "date", nullable: false),
                    deadlinedate = table.Column<DateOnly>(type: "date", nullable: false),
                    iscompleted = table.Column<bool>(type: "boolean", nullable: false),
                    statusid = table.Column<int>(type: "integer", nullable: false),
                    priorityid = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: true),
                    assigned_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentTasks", x => x.task_id);
                    table.ForeignKey(
                        name: "FK_CurrentTasks_TaskPriorities_priorityid",
                        column: x => x.priorityid,
                        principalTable: "TaskPriorities",
                        principalColumn: "IdPriority",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrentTasks_TaskStatuses_statusid",
                        column: x => x.statusid,
                        principalTable: "TaskStatuses",
                        principalColumn: "IdTaskStatus",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedTasks",
                columns: table => new
                {
                    IdArchivedTask = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskID = table.Column<int>(type: "integer", nullable: false),
                    TaskName = table.Column<string>(type: "text", nullable: false),
                    CurrentTasktask_id = table.Column<int>(type: "integer", nullable: false),
                    CompletionDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedTasks", x => x.IdArchivedTask);
                    table.ForeignKey(
                        name: "FK_ArchivedTasks_CurrentTasks_CurrentTasktask_id",
                        column: x => x.CurrentTasktask_id,
                        principalTable: "CurrentTasks",
                        principalColumn: "task_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedTasks_CurrentTasktask_id",
                table: "ArchivedTasks",
                column: "CurrentTasktask_id");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentTasks_priorityid",
                table: "CurrentTasks",
                column: "priorityid");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentTasks_statusid",
                table: "CurrentTasks",
                column: "statusid");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentTasks_task_name",
                table: "CurrentTasks",
                column: "task_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivedTasks");

            migrationBuilder.DropTable(
                name: "CurrentTasks");

            migrationBuilder.DropTable(
                name: "TaskPriorities");

            migrationBuilder.DropTable(
                name: "TaskStatuses");
        }
    }
}
