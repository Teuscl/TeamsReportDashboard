using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TeamsReportDashboard.Backend.Controllers;
using TeamsReportDashboard.Backend.Models.Job;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Delete;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Query;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Start;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Update;
using TeamsReportDashboard.Backend.Services.JobSynchronization;
using TeamsReportDashboard.Exceptions;
using Xunit;

namespace TeamsReportDashboard.Tests.Unit;

public class AnalysisControllerTests
{
    private readonly Mock<IStartAnalysisService> _startServiceMock;
    private readonly Mock<IAnalysisJobQueryService> _queryServiceMock;
    private readonly Mock<IJobManagementService> _jobManagementServiceMock;
    private readonly Mock<IUpdateAnalysisService> _updateServiceMock;
    private readonly Mock<IDeleteJobService> _deleteServiceMock;
    private readonly Mock<ILogger<AnalysisController>> _loggerMock;
    private readonly AnalysisController _controller;

    public AnalysisControllerTests()
    {
        _startServiceMock = new Mock<IStartAnalysisService>();
        _queryServiceMock = new Mock<IAnalysisJobQueryService>();
        _jobManagementServiceMock = new Mock<IJobManagementService>();
        _updateServiceMock = new Mock<IUpdateAnalysisService>();
        _deleteServiceMock = new Mock<IDeleteJobService>();
        _loggerMock = new Mock<ILogger<AnalysisController>>();

        _controller = new AnalysisController(
            _startServiceMock.Object,
            _queryServiceMock.Object,
            _jobManagementServiceMock.Object,
            _updateServiceMock.Object,
            _deleteServiceMock.Object,
            _loggerMock.Object
        );

        // Configura User no ControllerContext para passar no validador do userIdClaim
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim("id", Guid.NewGuid().ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task StartAnalysis_WhenValidRequest_ShouldReturnAccepted()
    {
        // Arrange
        var dto = new StartJobAnalysisDto();
        var expectedJobId = Guid.NewGuid();
        
        _startServiceMock.Setup(s => s.ExecuteAsync(It.IsAny<StartJobAnalysisDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(expectedJobId);

        // Act
        var result = await _controller.StartAnalysis(dto);

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedAtActionResult>().Subject;
        acceptedResult.ActionName.Should().Be(nameof(AnalysisController.GetJobStatus));
        acceptedResult.RouteValues.Should().ContainKey("jobId").WhoseValue.Should().Be(expectedJobId);
    }

    [Fact]
    public async Task StartAnalysis_WhenUserNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } 
        };
        var dto = new StartJobAnalysisDto();

        // Act
        var result = await _controller.StartAnalysis(dto);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task StartAnalysis_WhenErrorOnValidation_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new StartJobAnalysisDto();
        _startServiceMock.Setup(s => s.ExecuteAsync(It.IsAny<StartJobAnalysisDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new ErrorOnValidationException(["Invalid input"]));

        // Act
        var result = await _controller.StartAnalysis(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteJob_WhenJobExists_ShouldReturnNoContent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _deleteServiceMock.Setup(s => s.Execute(jobId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteJob(jobId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteJob_WhenJobDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _deleteServiceMock.Setup(s => s.Execute(jobId)).ThrowsAsync(new KeyNotFoundException("Not found"));

        // Act
        var result = await _controller.DeleteJob(jobId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
