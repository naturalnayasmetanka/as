using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProjectsService.Core.AuthService;

public sealed class AuthServiceClient : IAuthServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<AuthCurrentUser, AuthServiceClientError>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/auth/me", cancellationToken);
        var result = await ReadEnvelopeAsync<AuthGetMeResponse>(response, cancellationToken);

        return result.Map(user => new AuthCurrentUser(user.Id, user.Email));
    }

    public async Task<Result<IReadOnlyCollection<AuthUser>, AuthServiceClientError>> GetUsersAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/auth/admin/users", cancellationToken);
        var result = await ReadEnvelopeAsync<IReadOnlyCollection<AuthAdminUserResponse>>(response, cancellationToken);

        return result.Map(users =>
            (IReadOnlyCollection<AuthUser>)users
                .Select(user => new AuthUser(user.Id, user.Email, user.Roles))
                .ToArray());
    }

    private static async Task<Result<T, AuthServiceClientError>> ReadEnvelopeAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return Result.Failure<T, AuthServiceClientError>(
                new AuthServiceClientError(response.StatusCode, response.ReasonPhrase ?? response.StatusCode.ToString()));
        }

        AuthServiceEnvelope<T>? envelope;
        try
        {
            envelope = await response.Content.ReadFromJsonAsync<AuthServiceEnvelope<T>>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return Result.Failure<T, AuthServiceClientError>(
                new AuthServiceClientError(response.StatusCode, "AuthService returned an invalid response"));
        }

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<T, AuthServiceClientError>(
                new AuthServiceClientError(response.StatusCode, GetEnvelopeMessage(envelope) ?? response.ReasonPhrase ?? "AuthService request failed"));
        }

        if (envelope is null)
        {
            return Result.Failure<T, AuthServiceClientError>(
                new AuthServiceClientError(response.StatusCode, "AuthService returned an empty response"));
        }

        if (envelope.IsError || envelope.Result is null)
        {
            return Result.Failure<T, AuthServiceClientError>(
                new AuthServiceClientError(response.StatusCode, GetEnvelopeMessage(envelope) ?? "AuthService returned an error"));
        }

        return Result.Success<T, AuthServiceClientError>(envelope.Result);
    }

    private static string? GetEnvelopeMessage<T>(AuthServiceEnvelope<T>? envelope) =>
        envelope?.Error?.Messages?.FirstOrDefault(message => !string.IsNullOrWhiteSpace(message.Message))?.Message;
}
