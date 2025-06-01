using System.Net;
using System.Text.Json;
using FluentAssertions;

using NUnitTests.Reports;
using NUnitTests.Auth;
using Microsoft.Extensions.Configuration;

namespace NUnitTests.Tests
{
    [TestFixture]
    [Category("Books")]
    public class Tests
    {
        private string _baseUrl;
        private string _token;
        private HttpClient _client;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            ReportManager.CreateTest("BookTests");

            _baseUrl = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build()
                .GetSection("ApiSettings")["BaseUrl"];

            _token = await TokenProvider.GetTokenAsync();

            _client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        }

        [Test]
        public async Task CreateBook_ShouldReturn201AndMatchInput()
        {
            ReportManager.CreateTest("CreateBook_ShouldReturn201AndMatchInput");

            var book = new
            {
                title = "TEST3",
                author = "TEST3",
                isbn = "TEST3",
                publishedDate = DateTime.UtcNow.ToString("O")
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/Books")
            {
                Content = new StringContent(JsonSerializer.Serialize(book), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            ReportManager.LogInfo("Sending POST /Books with test book payload");

            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            ReportManager.LogPass("Received 201 Created");

            var content = await response.Content.ReadAsStringAsync();
            var responseBody = JsonDocument.Parse(content).RootElement;

            responseBody.GetProperty("title").GetString().Should().Be(book.title);
            responseBody.GetProperty("author").GetString().Should().Be(book.author);
            responseBody.GetProperty("isbn").GetString().Should().Be(book.isbn);
            ReportManager.LogPass("Response body matches the input data");
        }


        [Test]
        public async Task GetAllBooks_ShouldReturnListWithRequiredFields()
        {
            ReportManager.CreateTest("GetAllBooks_ShouldReturnListWithRequiredFields");

            var request = new HttpRequestMessage(HttpMethod.Get, "/Books");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            ReportManager.LogInfo("Sending GET /Books to retrieve all books");
            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            ReportManager.LogPass("Received 200 OK");

            var content = await response.Content.ReadAsStringAsync();
            var books = JsonSerializer.Deserialize<JsonElement>(content);

            books.ValueKind.Should().Be(JsonValueKind.Array);
            ReportManager.LogInfo($"Total books returned: {books.GetArrayLength()}");

            foreach (var book in books.EnumerateArray())
            {
                book.TryGetProperty("title", out _).Should().BeTrue("Book should contain 'title'");
                book.TryGetProperty("author", out _).Should().BeTrue("Book should contain 'author'");
                book.TryGetProperty("publishedDate", out _).Should().BeTrue("Book should contain 'publishedDate'");
            }

            ReportManager.LogPass("Each book entry contains title, author, and publishedDate");
        }


        [Test]
        public async Task GetBookById_ShouldReturn200WithValidBook()
        {
            ReportManager.CreateTest("GetBookById_ShouldReturn200WithValidBook");

            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/Books")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    title = "TEST",
                    author = "TEST",
                    isbn = Guid.NewGuid().ToString(),
                    publishedDate = DateTime.UtcNow
                }), System.Text.Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            var createResponse = await _client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var bookId = createdJson.RootElement.GetProperty("id").GetString();

            await BookApiValidator.EnsureBookExists(_client, _token, bookId);
            await BookApiValidator.EnsureBookNotFound(_client, _token, Guid.NewGuid().ToString());
            await BookApiValidator.EnsureBadRequest(_client, _token, "invalid-guid");

            var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/Books/{bookId}");
            getRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            var getResponse = await _client.SendAsync(getRequest);
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var fetchedBookJson = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
            var fetchedBook = fetchedBookJson.RootElement;

            fetchedBook.GetProperty("title").GetString().Should().Be("TEST");
            fetchedBook.GetProperty("author").GetString().Should().Be("TEST");
            ReportManager.LogPass("Valid ID: Book fetched successfully and data matches");
        }

        [Test]
        public async Task UpdateBook_ShouldReturn204AndValidate()
        {
            ReportManager.CreateTest("UpdateBook_ShouldReturn204AndValidate");

            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/Books")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    title = "Before Update",
                    author = "Before Author",
                    isbn = Guid.NewGuid().ToString(),
                    publishedDate = DateTime.UtcNow
                }), System.Text.Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            var createResponse = await _client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var bookId = createdJson.RootElement.GetProperty("id").GetString();

            var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/Books/{bookId}")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    title = "After Update",
                    author = "After Author",
                    publishedDate = DateTime.UtcNow
                }), System.Text.Encoding.UTF8, "application/json")
            };
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            var updateResponse = await _client.SendAsync(updateRequest);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            ReportManager.LogPass("Book updated successfully (204)");

            var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/Books/{bookId}");
            getRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            var getResponse = await _client.SendAsync(getRequest);
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedBook = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync()).RootElement;
            updatedBook.GetProperty("title").GetString().Should().Be("After Update");
            updatedBook.GetProperty("author").GetString().Should().Be("After Author");
            ReportManager.LogPass("Updated book data validated");

            await BookApiValidator.EnsureBookExists(_client, _token, bookId);
            await BookApiValidator.EnsureBookNotFound(_client, _token, Guid.NewGuid().ToString());
            await BookApiValidator.EnsureBadRequest(_client, _token, "not-a-guid");
        }

        [Test]
        public async Task DeleteBook_ShouldReturn204AndRemove()
        {
            ReportManager.CreateTest("DeleteBook_ShouldReturn204AndRemove");

            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/Books")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    title = "ToDelete",
                    author = "ToDelete",
                    isbn = Guid.NewGuid().ToString(),
                    publishedDate = DateTime.UtcNow
                }), System.Text.Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            var createResponse = await _client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var bookId = createdJson.RootElement.GetProperty("id").GetString();

            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/Books/{bookId}");
            deleteRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            var deleteResponse = await _client.SendAsync(deleteRequest);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            ReportManager.LogPass("Book deleted successfully (204)");

            await BookApiValidator.EnsureBookNotFound(_client, _token, bookId);
            await BookApiValidator.EnsureBookNotFound(_client, _token, Guid.NewGuid().ToString());
            await BookApiValidator.EnsureBadRequest(_client, _token, "invalid-guid");
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            ReportManager.FlushReport();
            _client.Dispose();
        }
    }
}
