using FluentAssertions;
using System.Net;
using TeamsReportDashboard.Tests.Integration;
using Xunit;

namespace TeamsReportDashboard.Tests.Integration;

public class AnalysisEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnalysisEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StartAnalysis_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/Analysis/start");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
