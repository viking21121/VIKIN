using System.Net.Http;
using System.Text;
using VikingMobile.Application.Communication;

namespace VikingMobile.Infrastructure.Communication;

public class WiFiNetworkChannel : ICommunicationChannel
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private string _baseUrl = string.Empty;

    public string Name => "Wi-Fi";

    public bool IsConnected { get; private set; }

    public string? UltimoError { get; private set; }

    public async Task<bool> ConnectAsync(string address)
    {
        try
        {
            _baseUrl = address.TrimEnd('/');
            UltimoError = null;

            using var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/viking/health");

            IsConnected = response.IsSuccessStatusCode;
            if (!IsConnected)
            {
                UltimoError = $"HTTP {(int)response.StatusCode}";
            }

            return IsConnected;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            UltimoError = ex is TaskCanceledException
                ? "Tiempo de espera agotado al conectar con Viking PC."
                : ex.InnerException?.Message ?? ex.Message;
            return false;
        }
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        _baseUrl = string.Empty;
        UltimoError = null;

        return Task.CompletedTask;
    }

    public async Task<string> SendAsync(
        string endpoint,
        string json)
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "El canal Wi-Fi no está conectado.");

        endpoint = endpoint.TrimStart('/');

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(
            $"{_baseUrl}/{endpoint}",
            content);

        string result =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Error HTTP {(int)response.StatusCode}: {result}");
        }

        return result;
    }

    public async Task<string> GetAsync(
        string endpoint)
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "El canal Wi-Fi no está conectado.");

        endpoint = endpoint.TrimStart('/');

        using var response =
            await _httpClient.GetAsync(
                $"{_baseUrl}/{endpoint}");

        string result =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Error HTTP {(int)response.StatusCode}: {result}");
        }

        return result;
    }
}