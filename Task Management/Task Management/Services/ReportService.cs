using Microsoft.EntityFrameworkCore;
using Task_Management.Data;
using Task_Management.Models;

namespace Task_Management.Services;

public class ReportService
{
    private readonly TaskManagementDbContext _context;

    public ReportService(TaskManagementDbContext context)
    {
        _context = context;
    }

    public async Task<ReportDto> GenerateReportAsync(ReportRequestDto request)
    {
        var tasksQuery = await _context.CurrentTasks
            .Include(t => t.StatusTask)
            .Include(t => t.TaskPriority)
            .Where(t => t.dateadded >= request.StartDate && t.dateadded <= request.EndDate)
            .ToListAsync();

        var taskItems = tasksQuery.Select(t => new ReportTaskItemDto
        {
            TaskId = t.task_id,
            TaskName = t.task_name ?? "Без названия",
            PriorityName = t.TaskPriority?.PriorityType ?? "Не указан",
            StatusName = t.StatusTask?.StatusName ?? "Не указан",
            DateAdded = t.dateadded,
            DeadlineDate = t.deadlinedate,
            IsCompleted = t.iscompleted
        }).ToList();

        return new ReportDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalTasks = taskItems.Count,
            CompletedTasksCount = taskItems.Count(t => t.IsCompleted),
            InProgressTasksCount = taskItems.Count(t => !t.IsCompleted && t.StatusName.Contains("работе", StringComparison.OrdinalIgnoreCase)),
            CanceledTasksCount = taskItems.Count(t => t.StatusName.Contains("отмен", StringComparison.OrdinalIgnoreCase)),
            Tasks = taskItems
        };
    }
}