using FluentAssertions;
using NUnitTests.Reports;
using System.Net;

public static class BookApiValidator
{
    public static async Task EnsureBookExists(HttpClient client, string token, string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/Books/{id}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Book with ID {id} should exist");
        ReportManager.LogPass($"Verified book with ID {id} exists");
    }

    public static async Task EnsureBookNotFound(HttpClient client, string token, string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/Books/{id}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"Book with ID {id} should not exist");
        ReportManager.LogPass($"Verified book with ID {id} does not exist (404)");
    }

    public static async Task EnsureBadRequest(HttpClient client, string token, string invalidId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/Books/{invalidId}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"ID '{invalidId}' is not a valid GUID");
        ReportManager.LogPass($"Verified invalid ID format returns 400");
    }
}