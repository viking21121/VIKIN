namespace VikingMobile.Application.Communication;

public class CommunicationManager
{
    private readonly List<ICommunicationChannel> _channels = new();

    public ICommunicationChannel? CurrentChannel { get; private set; }

    public void AddChannel(ICommunicationChannel channel)
    {
        if (!_channels.Contains(channel))
            _channels.Add(channel);
    }

    public async Task<bool> ConnectAsync(string address)
    {
        foreach (var channel in _channels)
        {
            if (await channel.ConnectAsync(address))
            {
                CurrentChannel = channel;
                return true;
            }
        }

        CurrentChannel = null;
        return false;
    }

    public async Task DisconnectAsync()
    {
        if (CurrentChannel == null)
            return;

        await CurrentChannel.DisconnectAsync();
        CurrentChannel = null;
    }

    public async Task<string> SendAsync(string endpoint, string json)
    {
        if (CurrentChannel == null)
            throw new InvalidOperationException(
                "No hay ningún canal de comunicación conectado.");

        return await CurrentChannel.SendAsync(endpoint, json);
    }
}