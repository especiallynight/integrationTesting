namespace Task_Management.Models;

public class ReportRequestDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class ReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasksCount { get; set; }
    public int InProgressTasksCount { get; set; }
    public int CanceledTasksCount { get; set; }

    public List<ReportTaskItemDto> Tasks { get; set; } = new();
}

public class ReportTaskItemDto
{
    public int TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateOnly DateAdded { get; set; }
    public DateOnly DeadlineDate { get; set; }
    public bool IsCompleted { get; set; }

    public int DaysAllocated => DeadlineDate.DayNumber - DateAdded.DayNumber;
}