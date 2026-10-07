using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Task_Management.Controllers;
using Task_Management.Data;
using Task_Management.Models;

namespace IntegrationTests
{
    [TestFixture]
    public class BottomUpTesting
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                });

            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        // 1.Тестируем самый базовый слой - Контекст БД 
        [Test]
        public async Task Step1_DbContext_SaveAndRetrieveDirectly()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: "BottomUp_Step1_TestDb")
                .Options;

            using var dbContext = new TaskManagementDbContext(options);

            var task = new CurrentTask
            {
                task_name = "Низкоуровневая задача",
                task_description = "Тестируем слой работы с данными напрямую",
                dateadded = DateOnly.FromDateTime(DateTime.Today),
                deadlinedate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                statusid = 1,
                priorityid = 1
            };

            dbContext.CurrentTasks.Add(task);
            await dbContext.SaveChangesAsync();

            var savedTask = await dbContext.CurrentTasks.FirstOrDefaultAsync(t => t.task_name == "Низкоуровневая задача");

            Assert.That(savedTask, Is.Not.Null);
            Assert.That(savedTask.task_description, Is.EqualTo("Тестируем слой работы с данными напрямую"));
        }


        // 2. Интегрируем Контроллер поверх проверенной БД 
        [Test]
        public async Task Step2_ControllerWithRealDbContext()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: "BottomUp_Step2_Db")
                .Options;

            using var dbContext = new TaskManagementDbContext(options);

            dbContext.CurrentTasks.Add(new CurrentTask
            {
                task_id = 1,
                task_name = "Задача Среднего Уровня",
                dateadded = DateOnly.FromDateTime(DateTime.Today),
                deadlinedate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                statusid = 1,
                priorityid = 1
            });
            await dbContext.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<ApiController>>();
            var controller = new ApiController(dbContext, mockLogger.Object);

            var result = await controller.GetTasks(null);

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var tasks = okResult.Value as List<CurrentTask>;

            Assert.That(tasks, Is.Not.Null);
            Assert.That(tasks.Count, Is.EqualTo(1));
            Assert.That(tasks[0].task_name, Is.EqualTo("Задача Среднего Уровня"));
        }


        // 3. Тестируем сквозную интеграцию через HTTP (Все слои + In-Memory БД)
        [Test]
        public async Task Step3_CreateAndGetTask()
        {
            var newTask = new CurrentTask
            {
                task_name = "Интеграционная задача",
                task_description = "Описание задачи",
                dateadded = DateOnly.FromDateTime(DateTime.Today),
                deadlinedate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                statusid = 1,
                priorityid = 1
            };

            var createResponse = await _client.PostAsJsonAsync("/api/tasks/CreateTask", newTask);
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var getResponse = await _client.GetAsync("/api/tasks/GetTasks");

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var content = await getResponse.Content.ReadAsStringAsync();
            Assert.That(content, Does.Contain("Интеграционная задача"));
        }
    }
}
