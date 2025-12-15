using System.Dynamic;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using AdminDashboard.Controllers;

using CommonApiUtilities.Interfaces;
using CommonApiUtilities.Security.Interfaces;

using Xunit;
using Moq;
using Newtonsoft.Json.Linq;
using System;

namespace AdminDashboardServiceUnitTests
{
    public class DashboardControllerTests
    {
        private readonly Mock<ILogger<DashboardController>> logger;
        private readonly Mock<IApplicationConfiguration> applicationConfiguration;
        private readonly Mock<IJsonUtilities> jsonUtilities;
        private readonly Mock<IFileUtilities> fileUtilities;
        private readonly Mock<ISqlCrud> sqlCrud;
        private readonly Mock<IExpandoObjectHandler> expandoObjectHandler;
        private readonly Mock<IActiveDirectoryCache> activeDirectoryAuthenticationCache;

        private readonly DashboardController dashboardController;

        public DashboardControllerTests()
        {
            logger = new Mock<ILogger<DashboardController>>();
            
            applicationConfiguration = new Mock<IApplicationConfiguration>();

            jsonUtilities = new Mock<IJsonUtilities>();
            fileUtilities = new Mock<IFileUtilities>();
            sqlCrud = new Mock<ISqlCrud>();

            expandoObjectHandler = new Mock<IExpandoObjectHandler>();
            var config = new ExpandoObject[2];

            var c1 = new ExpandoObject() as IDictionary<string, object>;
            c1.Add("ApiEndpoint", "FileTest");
            c1.Add("Type", "File");
            c1.Add("Map", "File");
            config[0] = c1 as ExpandoObject;

            var c2 = new ExpandoObject() as IDictionary<string, object>;
            c2.Add("ApiEndpoint", "SqlTest");
            c2.Add("Type", "SQL");
            c2.Add("Map", "SQL");
            config[1] = c2 as ExpandoObject;

            expandoObjectHandler.Setup(s => s.GetExpandoProperty("ApplicationMapping", null)).Returns(config);
            applicationConfiguration.Setup(s => s.GetApplicationSqlConfiguration<object>(It.IsAny<string>())).Returns(config);

            activeDirectoryAuthenticationCache = new Mock<IActiveDirectoryCache>();

            dashboardController = new DashboardController(logger.Object,
                                                          applicationConfiguration.Object,
                                                          jsonUtilities.Object,
                                                          fileUtilities.Object,
                                                          sqlCrud.Object,
                                                          expandoObjectHandler.Object,
                                                          activeDirectoryAuthenticationCache.Object);
        }
        
        [Fact]
        public void Read_ShouldReturnActionResult() => Assert.IsAssignableFrom<IActionResult>(dashboardController.Get("ApplicationMapping"));

        [Fact]
        public void Read_ShouldHaveValues()
        {
            var request = dashboardController.Get("ApplicationMapping") as OkObjectResult;
            Assert.NotNull(request);
            Assert.Equal(200, request.StatusCode);
            Assert.NotNull(request.Value);
        }
        
        [Fact]
        public void RefreshMapping_ShouldReturnActionResult() => Assert.IsAssignableFrom<IActionResult>(dashboardController.RefreshMapping());

        [Fact]
        public void RefreshMapping_ShouldNotHaveValues()
        {
            var request = dashboardController.RefreshMapping() as OkResult;
            Assert.NotNull(request);
            Assert.Equal(200, request.StatusCode);
        }

        [Fact]
        public void RefreshRoles_ShouldReturnActionResult() => Assert.IsAssignableFrom<IActionResult>(dashboardController.RefreshRoles());

        [Fact]
        public void RefreshRoles_ShouldNotHaveValues()
        {
            var request = dashboardController.RefreshRoles() as OkResult;
            Assert.NotNull(request);
            Assert.Equal(200, request.StatusCode);
        }

        [Fact]
        public void CreateShouldReturn400OnBadInput()
        {
            JObject test_object = new JObject();
            test_object.Add("fake", "data");
            var request = dashboardController.Post("test_string", test_object) as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }

        [Fact]
        public void ReadSchemaShouldReturn400OnBadInput()
        {
            var request = dashboardController.Get("None", "None") as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }
        [Fact]
        public void ReadShouldReturn400OnBadInput()
        {
            var request = dashboardController.Get("None") as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }
  
        [Fact]
        public void UpdateShouldReturn400OnBadInput()
        {
            JObject test_object = new JObject();
            test_object.Add("None", "None");
            var request = dashboardController.Put("NonExistant", test_object) as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }
        [Fact]
        public void UpdateShouldReturn400OnEmptyInput()
        {
            var request = dashboardController.Put("NonExistant", new JObject()) as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }
        [Fact]
        public void DeleteShouldReturn400OnBadInput()
        {
            JObject test_object = new JObject();
            test_object.Add("None", "None");
            var request = dashboardController.Delete("NonExistant", test_object) as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }
        [Fact]
        public void DeleteShouldReturn400OnEmptyInput()
        {
            var request = dashboardController.Delete("NonExistant", new JObject()) as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }

    }
}
