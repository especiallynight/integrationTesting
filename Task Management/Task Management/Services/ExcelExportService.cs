using ClosedXML.Excel;
using System.Drawing;
using System.IO;
using Task_Management.Models;

namespace Task_Management.Services;

public class ExcelExportService
{
    public byte[] ExportToExcel(ReportDto report)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Отчет по задачам");

        worksheet.Cell(1, 1).Value = $"Отчет по задачам с {report.StartDate:dd.MM.yyyy} по {report.EndDate:dd.MM.yyyy}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Cell(3, 1).Value = "Всего задач:";
        worksheet.Cell(3, 2).Value = report.TotalTasks;
        worksheet.Cell(4, 1).Value = "Выполнено:";
        worksheet.Cell(4, 2).Value = report.CompletedTasksCount;
        worksheet.Cell(5, 1).Value = "В работе:";
        worksheet.Cell(5, 2).Value = report.InProgressTasksCount;

        int startRow = 7;
        worksheet.Cell(startRow, 1).Value = "ID";
        worksheet.Cell(startRow, 2).Value = "Название задачи";
        worksheet.Cell(startRow, 3).Value = "Приоритет";
        worksheet.Cell(startRow, 4).Value = "Статус";
        worksheet.Cell(startRow, 5).Value = "Дата создания";
        worksheet.Cell(startRow, 6).Value = "Дедлайн";
        worksheet.Cell(startRow, 7).Value = "Завершено";

        var headerRange = worksheet.Range(startRow, 1, startRow, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        int currentRow = startRow + 1;
        foreach (var task in report.Tasks)
        {
            worksheet.Cell(currentRow, 1).Value = task.TaskId;
            worksheet.Cell(currentRow, 2).Value = task.TaskName;
            worksheet.Cell(currentRow, 3).Value = task.PriorityName;
            worksheet.Cell(currentRow, 4).Value = task.StatusName;
            worksheet.Cell(currentRow, 5).Value = task.DateAdded.ToString("dd.MM.yyyy");
            worksheet.Cell(currentRow, 6).Value = task.DeadlineDate.ToString("dd.MM.yyyy");
            worksheet.Cell(currentRow, 7).Value = task.IsCompleted ? "Да" : "Нет";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}