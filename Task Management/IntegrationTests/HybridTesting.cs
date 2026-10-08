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
    public class HybridTesting
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

        // 1. Элемент "снизу вверх": прямое тестирование связки Контроллера и In-Memory БД без сетевого слоя
        [Test]
        public async Task Step1_GetTasks_DirectIntegration()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: "Hybrid_Step1_Db")
                .Options;

            using var context = new TaskManagementDbContext(options);
            context.CurrentTasks.Add(new CurrentTask
            {
                task_id = 1,
                task_name = "Тестовая задача Step 1",
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
            Assert.That(tasks[0].task_name, Is.EqualTo("Тестовая задача Step 1")); 
        }

        // 2. Сквозная интеграция: HTTP API -> Контроллер -> EF Core -> In-Memory БД
        [Test]
        public async Task Step2_CreateAndGetTask_FullStack()
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

        // 3. Элемент "сверху вниз": изолированное тестирование входной валидации на уровне Контроллера
        [Test]
        public async Task Step3_CreateTask_ControllerValidation()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: "Hybrid_Step3_Db")
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
        
        //4. Сквозной негативный сценарий: проверка отсечения невалидного запроса на транспортном уровне HTTP API
        [Test]
        public async Task Step4_CreateTask_InvalidData()
        {
            var invalidTask = new CurrentTask
            {
                task_name = "",
                statusid = 1,
                priorityid = 1
            };

            var response = await _client.PostAsJsonAsync(
                "/api/tasks/CreateTask",
                invalidTask);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}