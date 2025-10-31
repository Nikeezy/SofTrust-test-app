using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class RecaptchaService : IRecaptchaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RecaptchaOptions _options;

    public RecaptchaService(IHttpClientFactory httpClientFactory, IOptions<RecaptchaOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return false;
        }

        var client = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = token
        });

        var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken: cancellationToken);
        return payload?.Success ?? false;
    }

    private class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
