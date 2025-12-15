using System.IO;
using System.Dynamic;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using AdminDashboard.Controllers;

using CommonApiUtilities.Interfaces;

using Xunit;
using Moq;
using AdminDashboardService.Controllers;
using Newtonsoft.Json.Linq;

namespace AdminDashboardServiceUnitTests
{
    public class HeartBeatControllerTests
    {
        private readonly Mock<ILogger<HeartbeatController>> logger;
        private readonly Mock<IApplicationConfiguration> applicationConfiguration;
        private readonly Mock<IFileUtilities> fileUtilities;
        private readonly Mock<IExpandoObjectHandler> expandoObjectHandler;
        private readonly HeartbeatController heartbeatController;

        public HeartBeatControllerTests()
        {
            logger = new Mock<ILogger<HeartbeatController>>();

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

            heartbeatController = new HeartbeatController(logger.Object);
        }

        [Fact]
        public void HeartbeatShouldReturn200()
        {
            var request = heartbeatController.Get() as OkObjectResult;
            Assert.Equal(200, request.StatusCode);
        }
    }
}
