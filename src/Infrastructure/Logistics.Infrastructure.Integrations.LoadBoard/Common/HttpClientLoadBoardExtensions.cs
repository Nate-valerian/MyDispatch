using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Logistics.Infrastructure.Integrations.LoadBoard;
using Microsoft.Extensions.Logging;

namespace Logistics.Infrastructure.Integrations.LoadBoard.Common;

internal sealed record HttpJsonResult<T>(bool IsSuccess, T? Value, string ErrorBody, HttpStatusCode? StatusCode);
internal sealed record HttpStatusResult(bool IsSuccess, string ErrorBody, HttpStatusCode? StatusCode);

/// <summary>
/// HttpClient extensions used by load board provider services. Each method wraps the
/// "send + status check + JSON deserialise + log on failure" pattern so providers stop
/// repeating try/catch + IsSuccessStatusCode boilerplate in every operation.
/// </summary>
internal static class HttpClientLoadBoardExtensions
{
    public static async Task<T?> TryGetFromJsonAsync<T>(
        this HttpClient client,
        string url,
        ILogger logger,
        string action,
        LoadBoardOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                token => client.GetAsync(url, token),
                options,
                logger,
                action,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("LoadBoard {Action} returned {StatusCode}", action, response.StatusCode);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            logger.LogError(ex, "LoadBoard {Action} failed", action);
            return default;
        }
    }

    public static async Task<HttpJsonResult<TResp>> TryPostAsJsonAsync<TReq, TResp>(
        this HttpClient client,
        string url,
        TReq body,
        ILogger logger,
        string action,
        LoadBoardOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                token => client.PostAsJsonAsync(url, body, token),
                options,
                logger,
                action,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("LoadBoard {Action} failed: {StatusCode} - {Error}", action, response.StatusCode, error);
                return new HttpJsonResult<TResp>(false, default, error, response.StatusCode);
            }

            var value = await response.Content.ReadFromJsonAsync<TResp>(ct);
            return new HttpJsonResult<TResp>(true, value, string.Empty, response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            logger.LogError(ex, "LoadBoard {Action} failed", action);
            return new HttpJsonResult<TResp>(false, default, ex.Message, null);
        }
    }

    public static async Task<HttpJsonResult<TResp>> TryPostFormAsync<TResp>(
        this HttpClient client,
        string url,
        Dictionary<string, string> form,
        ILogger logger,
        string action,
        LoadBoardOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                token => client.PostAsync(url, new FormUrlEncodedContent(form), token),
                options,
                logger,
                action,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("LoadBoard {Action} failed: {StatusCode} - {Error}", action, response.StatusCode, error);
                return new HttpJsonResult<TResp>(false, default, error, response.StatusCode);
            }

            var value = await response.Content.ReadFromJsonAsync<TResp>(ct);
            return new HttpJsonResult<TResp>(true, value, string.Empty, response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            logger.LogError(ex, "LoadBoard {Action} failed", action);
            return new HttpJsonResult<TResp>(false, default, ex.Message, null);
        }
    }

    public static async Task<HttpStatusResult> TryGetSuccessAsync(
        this HttpClient client,
        string url,
        ILogger logger,
        string action,
        LoadBoardOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                token => client.GetAsync(url, token),
                options,
                logger,
                action,
                ct);

            if (response.IsSuccessStatusCode)
            {
                return new HttpStatusResult(true, string.Empty, response.StatusCode);
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("LoadBoard {Action} failed: {StatusCode} - {Error}", action, response.StatusCode, error);
            return new HttpStatusResult(false, error, response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "LoadBoard {Action} failed", action);
            return new HttpStatusResult(false, ex.Message, null);
        }
    }

    public static async Task<bool> TryPostAsync<TReq>(
        this HttpClient client,
        string url,
        TReq body,
        ILogger logger,
        string action,
        LoadBoardOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                token => client.PostAsJsonAsync(url, body, token),
                options,
                logger,
                action,
                ct);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("LoadBoard {Action} returned {StatusCode}", action, response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            logger.LogError(ex, "LoadBoard {Action} failed", action);
            return false;
        }
    }

    public static async Task<bool> TryPutAsync<TReq>(
        this HttpClient client,
        string url,
        TReq body,
        ILogger logger,
        string action,
        LoadBoardOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                token => client.PutAsJsonAsync(url, body, token),
                options,
                logger,
                action,
                ct);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("LoadBoard {Action} returned {StatusCode}", action, response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            logger.LogError(ex, "LoadBoard {Action} failed", action);
            return false;
        }
    }

    public static async Task<bool> TryDeleteAsync(
        this HttpClient client,
        string url,
        ILogger logger,
        string action,
        LoadBoardOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRetryAsync(
                token => client.DeleteAsync(url, token),
                options,
                logger,
                action,
                ct);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("LoadBoard {Action} returned {StatusCode}", action, response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "LoadBoard {Action} failed", action);
            return false;
        }
    }

    private static async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        LoadBoardOptions? options,
        ILogger logger,
        string action,
        CancellationToken ct)
    {
        var maxAttempts = GetMaxAttempts(options);

        for (var attempt = 1; ; attempt++)
        {
            HttpResponseMessage? response = null;

            try
            {
                response = await send(ct);
                if (!ShouldRetry(response.StatusCode) || attempt >= maxAttempts)
                {
                    return response;
                }

                logger.LogWarning(
                    "LoadBoard {Action} returned transient {StatusCode}; retrying attempt {Attempt}/{MaxAttempts}",
                    action,
                    response.StatusCode,
                    attempt + 1,
                    maxAttempts);
            }
            catch (Exception ex) when (IsTransient(ex) && !ct.IsCancellationRequested && attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "LoadBoard {Action} hit a transient HTTP error; retrying attempt {Attempt}/{MaxAttempts}",
                    action,
                    attempt + 1,
                    maxAttempts);
            }

            response?.Dispose();
            await Task.Delay(GetRetryDelay(options, attempt), ct);
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private static bool IsTransient(Exception ex) => ex is HttpRequestException or TaskCanceledException;

    private static int GetMaxAttempts(LoadBoardOptions? options)
    {
        var retries = options?.MaxRetryAttempts ?? 2;
        return 1 + Math.Clamp(retries, 0, 5);
    }

    private static TimeSpan GetRetryDelay(LoadBoardOptions? options, int attempt)
    {
        var baseDelayMilliseconds = Math.Max(50, options?.RetryBaseDelayMilliseconds ?? 250);
        return TimeSpan.FromMilliseconds(baseDelayMilliseconds * Math.Pow(2, attempt - 1));
    }
}
