using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Task_Management.Controllers;
using Task_Management.Data;
using Task_Management.Models;

namespace IntegrationTests
{
    [TestFixture]
    public class TopDownIntegrationTests
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

        //1. Тестируем Контроллер + Заглушка Контекста БД
        [Test]
        public async Task CreateTask_ControllerValidation()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: "Step1_TestDb_TopDown")
                .Options;

            using var dbContext = new TaskManagementDbContext(options);
            var mockLogger = new Mock<ILogger<ApiController>>();

            var controller = new ApiController(dbContext, mockLogger.Object);

            var invalidTask = new CurrentTask
            {
                task_name = "",
                statusid = 1,
                priorityid = 1
            };

            var result = await controller.CreateTask(invalidTask);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequest = (BadRequestObjectResult)result;
            Assert.That(badRequest.StatusCode, Is.EqualTo(400));
        }

        // 2. Тестируем Контроллер с реальной In-Memory БД
        [Test]
        public async Task Step2_GetTasks()
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: "Step2_TestDb_TopDown")
                .Options;

            using var context = new TaskManagementDbContext(options);
            context.CurrentTasks.Add(new CurrentTask
            {
                task_id = 1,
                task_name = "Тестовая задача Step 2",
                dateadded = DateOnly.FromDateTime(DateTime.Today),
                deadlinedate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                statusid = 1,
                priorityid = 1
            });
            await context.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<ApiController>>();
            var controller = new ApiController(context, mockLogger.Object);

            var result = await controller.GetTasks(null);

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var tasks = okResult.Value as List<CurrentTask>;

            Assert.That(tasks, Is.Not.Null);
            Assert.That(tasks.Count, Is.EqualTo(1));
            Assert.That(tasks[0].task_name, Is.EqualTo("Тестовая задача Step 2"));
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
