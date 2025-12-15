using System;
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
    public class AuditControllerTests
    {
        private readonly Mock<ILogger<AuditController>> logger;
        private readonly Mock<IApplicationConfiguration> applicationConfiguration;
        private readonly Mock<ISqlCrud> sqlCrud;
        private readonly Mock<IExpandoObjectHandler> expandoObjectHandler;
        private readonly AuditController auditController;

        public AuditControllerTests()
        {
            logger = new Mock<ILogger<AuditController>>();
            applicationConfiguration = new Mock<IApplicationConfiguration>();
            sqlCrud = new Mock<ISqlCrud>();
            expandoObjectHandler = new Mock<IExpandoObjectHandler>();

            var config = new ExpandoObject[1];

            var c1 = new ExpandoObject() as IDictionary<string, object>;
            c1.Add("ApiEndpoint", "FileTest");
            c1.Add("Type", "File");
            c1.Add("Map", "File");
            config[0] = c1 as ExpandoObject;

            expandoObjectHandler.Setup(s => s.GetExpandoProperty("AdminDashboardAudit", null)).Returns(config);

            auditController = new AuditController(logger.Object, applicationConfiguration.Object, sqlCrud.Object, expandoObjectHandler.Object);
        }

        [Fact]
        public void Read_ShouldReturnActionResult() => Assert.IsAssignableFrom<IActionResult>(auditController.Get(null, null, null, null, null));

        [Fact]
        public void Read_ShouldHaveValues()
        {
            var request = auditController.Get(null, null, null, null, null) as OkObjectResult;
            Assert.NotNull(request);
            Assert.Equal(200, request.StatusCode);
            Assert.NotNull(request.Value);
        }
    }
}
