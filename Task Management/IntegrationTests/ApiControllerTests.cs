using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using Task_Management.Data;
using Task_Management.Models;

namespace IntegrationTests
{
    [TestFixture]
    public class ApiControllerTests
    {
        private CustomWebApplicationFactory _factory;
        private HttpClient _client;

        [SetUp]
        public void Setup()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            db.Database.EnsureCreated();

            if (!db.TaskStatuses.Any())
            {
                db.TaskStatuses.Add(new StatusTask { IdTaskStatus = 1, StatusName = "В процессе" });
                db.TaskPriorities.Add(new TaskPriority { IdPriority = 1, PriorityType = "Высокий" });
                db.SaveChanges();
            }
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }
        //Тест 1 - Создание и сохранение задачи
        [Test]
        public async Task CreateTask_ShouldReturnOk_AndSaveToDatabase()
        {
            var task = new CurrentTask
            {
                task_name = "Тест создания задачи",
                task_description = "создание и сохранение задачи",
                dateadded = DateOnly.FromDateTime(DateTime.UtcNow),
                deadlinedate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                iscompleted = false,
                statusid = 1,
                priorityid = 1
            };

            var response = await _client.PostAsJsonAsync("/api/tasks/CreateTask", task);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var savedTask = db.CurrentTasks.FirstOrDefault(t => t.task_name == "Тест создания задачи");

            Assert.That(savedTask, Is.Not.Null);
            Assert.That(savedTask.statusid, Is.EqualTo(1));
        }
        //Тест 2 - получение списка задач
        [Test]
        public async Task GetTasks_ShouldReturnOkWithTaskList()
        {
            var response = await _client.GetAsync("/api/tasks/GetTasks");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        // Тест 3 - обновление задачи
        [Test]
        public async Task UpdateTask_ShouldReturnOk_AndSaveToDatabase()
        {
            int createdTaskId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

                var initialTask = new CurrentTask
                {
                    task_name = "Исходная задача",
                    task_description = "Описание до обновления",
                    dateadded = DateOnly.FromDateTime(DateTime.UtcNow),
                    deadlinedate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                    iscompleted = false,
                    statusid = 1,
                    priorityid = 1
                };

                db.CurrentTasks.Add(initialTask);
                await db.SaveChangesAsync();

                createdTaskId = initialTask.task_id;
            }

            var taskToUpdate = new CurrentTask
            {
                task_id = createdTaskId,
                task_name = "Исходная задача",
                task_description = "Обновляем задачу",
                dateadded = DateOnly.FromDateTime(DateTime.UtcNow),
                deadlinedate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
                iscompleted = false,
                statusid = 1,
                priorityid = 1 
            };

            var response = await _client.PutAsJsonAsync("/api/tasks/UpdateTask", taskToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using (var checkScope = _factory.Services.CreateScope())
            {
                var checkDb = checkScope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var updatedTask = await checkDb.CurrentTasks.FindAsync(createdTaskId);

                Assert.That(updatedTask, Is.Not.Null);
                Assert.That(updatedTask.task_description, Is.EqualTo("Обновляем задачу"));
                Assert.That(updatedTask.deadlinedate, Is.EqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4))));
            }
        }
        //Тест 4 - Обновление несуществующей задачи
        [Test]
        public async Task UpdateNonExistentTask()
        {
            var task = new CurrentTask
            {
                task_id = 73281,
                task_name = "Несуществующая задача",
                statusid = 1,
                priorityid = 1
            };

            var response = await _client.PutAsJsonAsync("/api/tasks/UpdateTask", task);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
        //Тест 5 - Создание задачи с некорректными данными
        [Test]
        public async Task CreateTaskWithIncorrectParameters()
        {
            var task = new CurrentTask
            {
                task_name = "Тест создания задачи",
                task_description = "создание и сохранение задачи",
                dateadded = DateOnly.FromDateTime(DateTime.UtcNow),
                deadlinedate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                iscompleted = false,
                statusid = 68954,
                priorityid = 1
            };

            var response = await _client.PostAsJsonAsync("/api/tasks/CreateTask", task);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        // Тест 6 - Удаление существующей задачи
        [Test]
        public async Task DeleteTask_ShouldReturnOk_AndRemoveFromDatabase()
        {
            int taskIdToDelete;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var task = new CurrentTask
                {
                    task_name = "Задача на удаление",
                    statusid = 1,
                    priorityid = 1
                };
                db.CurrentTasks.Add(task);
                await db.SaveChangesAsync();
                taskIdToDelete = task.task_id;
            }

            var response = await _client.DeleteAsync($"/api/tasks/DeleteTask/{taskIdToDelete}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var checkScope = _factory.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var deletedTask = await checkDb.CurrentTasks.FindAsync(taskIdToDelete);

            Assert.That(deletedTask, Is.Null);
        }

        // Тест 7 - Удаление несуществующей задачи
        [Test]
        public async Task DeleteNonExistentTask_ShouldReturnNotFound()
        {
            var response = await _client.DeleteAsync("/api/tasks/DeleteTask/999999");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        // Тест 8 - Получение списка архивированных задач
        [Test]
        public async Task GetArchivedTasks_ShouldReturnOkWithList()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                db.ArchivedTasks.Add(new ArchivedTask { IdArchivedTask = 1, TaskName = "Архивная задача" });
                await db.SaveChangesAsync();
            }

            var response = await _client.GetAsync("/api/tasks/GetArchivedTasks");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var archivedTasks = await response.Content.ReadFromJsonAsync<List<ArchivedTask>>();
            Assert.That(archivedTasks, Is.Not.Null);
            Assert.That(archivedTasks, Is.Not.Empty);
        }
    }
}