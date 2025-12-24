using System.IO;
using System.Dynamic;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using AdminDashboard.Controllers;

using CommonApiUtilities.Interfaces;

using Xunit;
using Moq;

namespace AdminDashboardServiceUnitTests
{
    public class LogControllerTests
    {
        private readonly Mock<ILogger<LogController>> logger;
        private readonly Mock<IApplicationConfiguration> applicationConfiguration;
        private readonly Mock<IFileUtilities> fileUtilities;
        private readonly Mock<IExpandoObjectHandler> expandoObjectHandler;
        private readonly LogController logController;

        public LogControllerTests()
        {
            logger = new Mock<ILogger<LogController>>();

            applicationConfiguration = new Mock<IApplicationConfiguration>();
            applicationConfiguration.Setup(x => x.GetApplicationFileConfiguration<string>("LogLocation")).Returns(@"D:\Data\Log");
            applicationConfiguration.Setup(x => x.GetMasterExpando()).Returns(new ExpandoObject());
            
            fileUtilities = new Mock<IFileUtilities>();
            fileUtilities.Setup(s => s.ReadFileReverse(It.IsAny<FileInfo>())).Returns("File in reverse");

            expandoObjectHandler = new Mock<IExpandoObjectHandler>();
            var expando = new ExpandoObject() as IDictionary<string, object>;
            var e = new Dictionary<string, object>()
            {
                { "Example1", new List<string>() { { "Trey" }, { "Barton" } } },
                { "Example2", new List<string>() { { "Barton" }, { "Trey" } } }
            };
            expando.Add("LogMapping", e);

            expandoObjectHandler.Setup(s => s.GetExpandoProperty(It.IsAny<string>(), It.IsAny<object>())).Returns(expando as ExpandoObject);

            logController = new LogController(logger.Object, applicationConfiguration.Object, fileUtilities.Object, expandoObjectHandler.Object);
        }

        [Fact]
        public void Read_ShouldReturnActionResult() => Assert.IsAssignableFrom<IActionResult>(logController.Get());

        [Fact]
        public void Read_ShouldHaveValues()
        {
            var request = logController.Get() as OkObjectResult;
            Assert.NotNull(request);
            Assert.Equal(200, request.StatusCode);
            Assert.NotNull(request.Value);
        }

        [Fact]
        public void ReadMapping_ShouldReturnActionResult() => Assert.IsAssignableFrom<IActionResult>(logController.Get(null, null));

        [Fact]
        public void ReadMapping_ShouldHaveValues()
        {
            var request = logController.Get(null, null) as BadRequestObjectResult;
            Assert.NotNull(request);
            Assert.Equal(400, request.StatusCode);
            Assert.NotNull(request.Value);
        }

        [Fact]
        public void ReadWithNoInputShouldReturn200()
        {
            var request = logController.Get() as OkObjectResult;
            Assert.Equal(200, request.StatusCode);
        }

        [Fact]
        public void ReadWithBadInputShouldReturn400()
        {
            var request = logController.Get("noway", "thisexists?2928828") as BadRequestObjectResult;
            Assert.Equal(400, request.StatusCode);
        }

    }
}
