using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Erasmus_SSC.Client.Dtos;
using Microsoft.AspNetCore.Components.Forms;

namespace Erasmus_SSC.Client.Services;

public sealed class ReportsApiClient
{
    private const long MaxFileBytes = 20 * 1024 * 1024;

    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;

    public ReportsApiClient(HttpClient http, ITokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken ct)
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

    public async Task<List<ReportItemDto>> GetReportsAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync("/api/reports", ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(resp, "GetReports", ct));

        return await resp.Content.ReadFromJsonAsync<List<ReportItemDto>>(cancellationToken: ct) ?? new();
    }

    public async Task<List<ReportLanguageDto>> GetLanguagesAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync("/api/reports/languages", ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(resp, "GetLanguages", ct));

        return await resp.Content.ReadFromJsonAsync<List<ReportLanguageDto>>(cancellationToken: ct) ?? new();
    }

    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadAsync(int id, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync($"/api/reports/{id}/download", ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(resp, "Download", ct));

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        var headerFileName =
            resp.Content.Headers.ContentDisposition?.FileNameStar ??
            resp.Content.Headers.ContentDisposition?.FileName;

        var fileName = string.IsNullOrWhiteSpace(headerFileName)
            ? $"report-{id}"
            : headerFileName.Trim('"');

        return (bytes, fileName, contentType);
    }

    public async Task<ReportItemDto> UploadAsync(string title, int languageId, IBrowserFile file, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent(languageId.ToString()), "LanguageId");

        var stream = file.OpenReadStream(maxAllowedSize: MaxFileBytes);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

        content.Add(fileContent, "File", file.Name);

        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/reports/upload", content, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(resp, "Upload", ct));

        var created = await resp.Content.ReadFromJsonAsync<ReportItemDto>(cancellationToken: ct);
        if (created is null)
            throw new InvalidOperationException("Upload succeeded but response is empty.");

        return created;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"/api/reports/{id}", null, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent)
            return;

        throw new InvalidOperationException(await ReadErrorAsync(resp, "Delete", ct));
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp, string operation, CancellationToken ct)
    {
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(text))
            return $"{operation} failed ({(int)resp.StatusCode})";

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var messageEl) && messageEl.ValueKind == JsonValueKind.String)
                return messageEl.GetString() ?? $"{operation} failed ({(int)resp.StatusCode})";

            if (root.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String)
            {
                if (root.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Object)
                {
                    var details = errorsEl
                        .EnumerateObject()
                        .SelectMany(x => x.Value.ValueKind == JsonValueKind.Array
                            ? x.Value.EnumerateArray().Select(v => v.ToString())
                            : new[] { x.Value.ToString() })
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    if (details.Count > 0)
                        return $"{titleEl.GetString()}: {string.Join("; ", details)}";
                }

                return titleEl.GetString() ?? $"{operation} failed ({(int)resp.StatusCode})";
            }
        }
        catch (JsonException)
        {
        }

        return text;
    }
}
