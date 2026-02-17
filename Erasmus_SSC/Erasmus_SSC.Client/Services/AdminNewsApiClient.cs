using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erasmus_SSC.Client.Dtos;
using Microsoft.AspNetCore.Components.Forms;

namespace Erasmus_SSC.Client.Services;

public sealed class AdminNewsApiClient
{
    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;

    public AdminNewsApiClient(HttpClient http, ITokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("No access token. Please log in again.");

        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (content is not null)
            req.Content = content;

        return req;
    }

    public async Task<List<AdminNewsDto>> GetNewsAsync(CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Get, "/api/admin/news", null, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(text)
                ? $"GetNews failed ({(int)resp.StatusCode})"
                : text);
        }

        return await resp.Content.ReadFromJsonAsync<List<AdminNewsDto>>(cancellationToken: ct) ?? new();
    }

    public async Task<AdminNewsDto> CreateNewsAsync(string title, string description, DateTime date, IBrowserFile? image, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent(description), "Description");
        content.Add(new StringContent(date.ToString("yyyy-MM-dd")), "Date");

        if (image is not null)
        {
            var stream = image.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
            content.Add(fileContent, "Image", image.Name);
        }

        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/admin/news", content, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(text)
                ? $"CreateNews failed ({(int)resp.StatusCode})"
                : text);
        }

        var created = await resp.Content.ReadFromJsonAsync<AdminNewsDto>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("CreateNews succeeded but response is empty.");

        return created;
    }

    public async Task DeleteNewsAsync(int id, CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"/api/admin/news/{id}", null, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent)
            return;

        var text = await resp.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(text)
            ? $"DeleteNews failed ({(int)resp.StatusCode})"
            : text);
    }
}
