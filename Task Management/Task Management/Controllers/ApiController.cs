using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Task_Management.Data;
using Task_Management.Models;

namespace Task_Management.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class ApiController : ControllerBase
    {
        private readonly TaskManagementDbContext _context;
        private readonly ILogger<ApiController> _logger;

        public ApiController(TaskManagementDbContext context, ILogger<ApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("CreateTask")]
        public async Task<IActionResult> CreateTask([FromBody] CurrentTask? request)
        {
            var requestTime = DateTime.UtcNow;
            Tracer.TaskManagerTrace.TraceEvent(TraceEventType.Start, 0, "Начало CreateTask");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[{Time}] Попытка передать пустое тело запроса в CreateTask.", requestTime);
                    return BadRequest("Тело запроса не может быть пустым.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[{Time}] Невалидная модель при создании задачи.", requestTime);
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.task_name))
                {
                    _logger.LogWarning("[{Time}] Попытка создать задачу с пустым названием.", requestTime);
                    return BadRequest("Название задачи (task_name) обязательное для заполнения.");
                }

                var statusExists = await _context.TaskStatuses.AnyAsync(s => s.IdTaskStatus == request.statusid);
                if (!statusExists)
                {
                    _logger.LogWarning("[{Time}] Указан несуществующий StatusID={StatusID}.", requestTime, request.statusid);
                    return BadRequest($"Статус с ID {request.statusid} не существует.");
                }

                var priorityExists = await _context.TaskPriorities.AnyAsync(p => p.IdPriority == request.priorityid);
                if (!priorityExists)
                {
                    _logger.LogWarning("[{Time}] Указан несуществующий PriorityID={PriorityID}.", requestTime, request.priorityid);
                    return BadRequest($"Приоритет с ID {request.priorityid} не существует.");
                }

                if (request.deadlinedate < request.dateadded)
                {
                    _logger.LogWarning("[{Time}] Дата дедлайна раньше даты создания.", requestTime);
                    return BadRequest("Дата окончания (deadlinedate) не может быть раньше даты создания (dateadded).");
                }

                await _context.CurrentTasks.AddAsync(request);
                await _context.SaveChangesAsync();

                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Stop,
                    1,
                    $"Завершение CreateTask. Время: {sw.ElapsedMilliseconds} мс"
                );
                _logger.LogInformation("[{Time}] Задача успешно создана с TaskID={TaskID}.", requestTime, request.task_id);

                return Ok(new
                {
                    task_id = request.task_id,
                    message = "Задача успешно создана."
                });
            }
            catch (Exception ex)
            {
                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Error,
                    3,
                    $"Ошибка в CreateTask: {ex.Message}"
                );
                _logger.LogError(ex, "[{Time}] Ошибка при создании задачи.", requestTime);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ошибка сервера: {ex.Message}");
            }
        }

        [HttpGet("GetTasks")]
        public async Task<IActionResult> GetTasks([FromQuery] int? statusid = null)
        {
            var requestTime = DateTime.UtcNow;
            Tracer.TaskManagerTrace.TraceEvent(TraceEventType.Start, 0, "Начало GetTasks");
            var sw = Stopwatch.StartNew();

            try
            {
                if (statusid.HasValue && statusid.Value <= 0)
                {
                    _logger.LogWarning("[{Time}] Передан невалидный StatusID={StatusID}.", requestTime, statusid.Value);
                    return BadRequest("Идентификатор статуса должен быть положительным числом.");
                }

                IQueryable<CurrentTask> query = _context.CurrentTasks.AsNoTracking();

                if (statusid.HasValue)
                {
                    query = query.Where(t => t.statusid == statusid.Value);
                }

                var tasks = await query.ToListAsync();

                if (tasks.Count == 0)
                {
                    Tracer.TaskManagerTrace.TraceEvent(
                        TraceEventType.Warning,
                        1,
                        "Попытка получить пустой список задач"
                    );
                    _logger.LogInformation("[{Time}] Задачи не найдены.", requestTime);
                    return Ok(new List<CurrentTask>());
                }

                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Stop,
                    1,
                    $"Завершение GetTasks. Время: {sw.ElapsedMilliseconds} мс"
                );
                _logger.LogInformation("[{Time}] Успешно получено {Count} задач.", requestTime, tasks.Count);

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Error,
                    3,
                    $"Ошибка в GetTasks: {ex.Message}"
                );
                _logger.LogError(ex, "[{Time}] Ошибка при получении задач.", requestTime);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ошибка сервера: {ex.Message}");
            }
        }

        [HttpPut("UpdateTask")]
        public async Task<IActionResult> UpdateTask([FromBody] CurrentTask? request)
        {
            var requestTime = DateTime.UtcNow;
            Tracer.TaskManagerTrace.TraceEvent(TraceEventType.Start, 0, "Начало UpdateTask");
            var sw = Stopwatch.StartNew();

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[{Time}] Попытка передать пустое тело запроса в UpdateTask.", requestTime);
                    return BadRequest("Тело запроса не может быть пустым.");
                }

                if (request.task_id <= 0)
                {
                    _logger.LogWarning("[{Time}] Указан некорректный ID задачи ({TaskID}).", requestTime, request.task_id);
                    return BadRequest("Идентификатор задачи должен быть положительным числом.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[{Time}] Невалидная модель при обновлении задачи ID={TaskID}.", requestTime, request.task_id);
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.task_name))
                {
                    _logger.LogWarning("[{Time}] Передано пустое имя при обновлении задачи ID={TaskID}.", requestTime, request.task_id);
                    return BadRequest("Название задачи (task_name) не может быть пустым.");
                }

                var existingTask = await _context.CurrentTasks.FirstOrDefaultAsync(t => t.task_id == request.task_id);

                if (existingTask == null)
                {
                    Tracer.TaskManagerTrace.TraceEvent(
                        TraceEventType.Warning,
                        1,
                        "Попытка обновить несуществующую задачу"
                    );
                    _logger.LogWarning("[{Time}] Ошибка: задача с ID={TaskID} не была обновлена (не найдена).", requestTime, request.task_id);
                    return NotFound($"Задача с ID {request.task_id} не найдена.");
                }

                var statusExists = await _context.TaskStatuses.AnyAsync(s => s.IdTaskStatus == request.statusid);
                if (!statusExists)
                {
                    _logger.LogWarning("[{Time}] Указан несуществующий StatusID={StatusID} при обновлении.", requestTime, request.statusid);
                    return BadRequest($"Статус с ID {request.statusid} не существует.");
                }

                var priorityExists = await _context.TaskPriorities.AnyAsync(p => p.IdPriority == request.priorityid);
                if (!priorityExists)
                {
                    _logger.LogWarning("[{Time}] Указан несуществующий PriorityID={PriorityID} при обновлении.", requestTime, request.priorityid);
                    return BadRequest($"Приоритет с ID {request.priorityid} не существует.");
                }

                if (request.deadlinedate < request.dateadded)
                {
                    _logger.LogWarning("[{Time}] Дата дедлайна раньше даты создания при обновлении.", requestTime);
                    return BadRequest("Дата окончания (deadlinedate) не может быть раньше даты создания (dateadded).");
                }

                existingTask.task_name = request.task_name;
                existingTask.task_description = request.task_description;
                existingTask.dateadded = request.dateadded;
                existingTask.deadlinedate = request.deadlinedate;
                existingTask.iscompleted = request.iscompleted;
                existingTask.statusid = request.statusid;
                existingTask.priorityid = request.priorityid;

                await _context.SaveChangesAsync();

                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Stop,
                    1,
                    $"Завершение UpdateTask. Время: {sw.ElapsedMilliseconds} мс"
                );
                _logger.LogInformation("[{Time}] Задача с TaskID={TaskID} успешно обновлена.", requestTime, request.task_id);

                return Ok($"Задача с ID {request.task_id} успешно обновлена.");
            }
            catch (Exception ex)
            {
                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Error,
                    3,
                    $"Ошибка в UpdateTask: {ex.Message}"
                );
                _logger.LogError(ex, "[{Time}] Ошибка: не удалось обновить задачу с ID={TaskID}.", requestTime, request.task_id);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ошибка сервера: {ex.Message}");
            }
        }

        [HttpDelete("DeleteTask/{task_id}")]
        public async Task<IActionResult> DeleteTask(int task_id)
        {
            var requestTime = DateTime.UtcNow;
            Tracer.TaskManagerTrace.TraceEvent(TraceEventType.Start, 0, "Начало DeleteTask");
            var sw = Stopwatch.StartNew();

            try
            {
                if (task_id <= 0)
                {
                    _logger.LogWarning("[{Time}] Попытка удаления задачи с невалидным ID={TaskID}.", requestTime, task_id);
                    return BadRequest("Идентификатор задачи должен быть положительным числом.");
                }

                var task = await _context.CurrentTasks.FirstOrDefaultAsync(t => t.task_id == task_id);

                if (task == null)
                {
                    Tracer.TaskManagerTrace.TraceEvent(
                        TraceEventType.Warning,
                        2,
                        "Попытка удалить несуществующую задачу"
                    );
                    _logger.LogWarning("[{Time}] Ошибка: задача с ID={TaskID} не была удалена", requestTime, task_id);
                    return NotFound($"Задача с ID {task_id} не найдена.");
                }

                _context.CurrentTasks.Remove(task);
                await _context.SaveChangesAsync();

                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Stop,
                    2,
                    $"Завершение DeleteTask. Время: {sw.ElapsedMilliseconds} мс"
                );
                _logger.LogInformation("[{Time}] Задача с TaskID={TaskID} успешно удалена.", requestTime, task_id);

                return Ok($"Задача с ID {task_id} успешно удалена.");
            }
            catch (Exception ex)
            {
                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Error,
                    3,
                    $"Ошибка в DeleteTask: {ex.Message}"
                );
                _logger.LogError(ex, "[{Time}] Ошибка: не удалось удалить задачу с ID={TaskID}.", requestTime, task_id);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ошибка сервера: {ex.Message}");
            }
        }

        [HttpPost("ArchiveTasks")]
        public async Task<IActionResult> ArchiveCompletedTasks()
        {
            var requestTime = DateTime.UtcNow;
            Tracer.TaskManagerTrace.TraceEvent(TraceEventType.Start, 0, "Начало ArchiveTasks");
            var sw = Stopwatch.StartNew();

            try
            {
                int archivedCount = 0;

                if (_context.Database.IsRelational())
                {
                    var result = await _context.Database
                        .SqlQuery<int>($"SELECT archiveTask() AS \"Value\"")
                        .ToListAsync();

                    archivedCount = result.FirstOrDefault();
                }
                else
                {
                    _logger.LogWarning("[{Time}] Вызов функции архивации в непровайдерной реляционной БД.", requestTime);
                }

                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Stop,
                    2,
                    $"Завершение ArchiveTasks. Время: {sw.ElapsedMilliseconds} мс"
                );
                _logger.LogInformation("[{Time}] Архивировано задач: {Count}", requestTime, archivedCount);

                return Ok(new
                {
                    archived_tasks_count = archivedCount,
                    message = archivedCount > 0 ?
                        $"Архивировано {archivedCount} задач" :
                        "У вас нет выполненных задач"
                });
            }
            catch (Exception ex)
            {
                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Error,
                    3,
                    $"Ошибка в ArchiveTasks: {ex.Message}"
                );
                _logger.LogError(ex, "[{Time}] Ошибка при архивации выполненных задач.", requestTime);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ошибка архивации: {ex.Message}");
            }
        }

        [HttpGet("GetArchivedTasks")]
        public async Task<IActionResult> GetArchivedTasks()
        {
            var requestTime = DateTime.UtcNow;
            Tracer.TaskManagerTrace.TraceEvent(TraceEventType.Start, 0, "Начало GetArchivedTasks");
            var sw = Stopwatch.StartNew();

            try
            {
                var archivedTasks = await _context.ArchivedTasks
                    .AsNoTracking()
                    .OrderByDescending(at => at.CompletionDate)
                    .ThenByDescending(at => at.IdArchivedTask)
                    .ToListAsync();

                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Stop,
                    2,
                    $"Завершение GetArchivedTasks. Время: {sw.ElapsedMilliseconds} мс"
                );
                _logger.LogInformation("[{Time}] Успешно получено {Count} архивированных задач.", requestTime, archivedTasks.Count);

                return Ok(archivedTasks);
            }
            catch (Exception ex)
            {
                Tracer.TaskManagerTrace.TraceEvent(
                    TraceEventType.Error,
                    3,
                    $"Ошибка в GetArchivedTasks: {ex.Message}"
                );
                _logger.LogError(ex, "[{Time}] Ошибка при получении архивированных задач.", requestTime);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Ошибка сервера: {ex.Message}");
            }
        }
    }
}