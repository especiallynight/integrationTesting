using Microsoft.AspNetCore.Mvc;
using Task_Management.Models;
using Task_Management.Services;

namespace Task_Management.Controllers;

public class ReportController : Controller
{
    private readonly ReportService _reportService;
    private readonly ExcelExportService _excelExportService;

    public ReportController(
        ReportService reportService,
        ExcelExportService excelExportService)
    {
        _reportService = reportService;
        _excelExportService = excelExportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? startDate, DateOnly? endDate)
    {
        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var request = new ReportRequestDto { StartDate = start, EndDate = end };
        var report = await _reportService.GenerateReportAsync(request);

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExcel(DateOnly startDate, DateOnly endDate)
    {
        var request = new ReportRequestDto { StartDate = startDate, EndDate = endDate };
        var report = await _reportService.GenerateReportAsync(request);

        var fileBytes = _excelExportService.ExportToExcel(report);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx");
    }
}