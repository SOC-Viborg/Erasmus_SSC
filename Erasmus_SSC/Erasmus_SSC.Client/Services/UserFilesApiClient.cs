using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erasmus_SSC.Client.Dtos;
using Microsoft.AspNetCore.Components.Forms;

namespace Erasmus_SSC.Client.Services;

public sealed class UserFilesApiClient
{
    private const long MaxFileBytes = 10 * 1024 * 1024;

    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;

    public UserFilesApiClient(HttpClient http, ITokenStore tokens)
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

    public async Task<List<UserFileItemDto>> GetFilesAsync(CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Get, "/api/user-files", null, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? $"GetFiles failed ({(int)resp.StatusCode})"
                    : text);
        }

        return await resp.Content.ReadFromJsonAsync<List<UserFileItemDto>>(cancellationToken: ct) ?? new();
    }

    public async Task<UserFileItemDto> UploadAsync(IBrowserFile file, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();

        var stream = file.OpenReadStream(maxAllowedSize: MaxFileBytes);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

        content.Add(fileContent, "File", file.Name);

        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/user-files/upload", content, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? $"Upload failed ({(int)resp.StatusCode})"
                    : text);
        }

        var uploaded = await resp.Content.ReadFromJsonAsync<UserFileItemDto>(cancellationToken: ct);
        if (uploaded is null)
            throw new InvalidOperationException("Upload succeeded but response is empty.");

        return uploaded;
    }

    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadAsync(int id, CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"/api/user-files/{id}/download", null, ct);
        using var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? $"Download failed ({(int)resp.StatusCode})"
                    : text);
        }

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        var headerFileName =
            resp.Content.Headers.ContentDisposition?.FileNameStar ??
            resp.Content.Headers.ContentDisposition?.FileName;

        var fileName = string.IsNullOrWhiteSpace(headerFileName)
            ? $"file-{id}"
            : headerFileName.Trim('"');

        return (bytes, fileName, contentType);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var req = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"/api/user-files/{id}", null, ct);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? $"Delete failed ({(int)resp.StatusCode})"
                    : text);
        }
    }
}