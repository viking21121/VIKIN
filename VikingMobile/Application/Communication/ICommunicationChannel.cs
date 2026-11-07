namespace VikingMobile.Application.Communication;

public interface ICommunicationChannel
{
    string Name { get; }

    bool IsConnected { get; }

    Task<bool> ConnectAsync(string address);

    Task DisconnectAsync();

    Task<string> SendAsync(
        string endpoint,
        string json);

    Task<string> GetAsync(
        string endpoint);
}