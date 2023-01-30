using Lidgren.Network;
using UnityEngine;
using zapnet;

public class ClientHandler : IClientHandler
{
    private int _serverVersion;

    public void ReadInitialData(NetIncomingMessage buffer)
    {
        // We can read that initial data we wrote in our IServerHandler implementation.
        var message = buffer.ReadString();
        Debug.Log("The server said: " + message);
    }

    public INetworkPacket GetCredentialsPacket()
    {
        // We need to create a login credentials packet to send, this is an implementation of INetworkPacket.
        var packet = Zapnet.Network.CreatePacket<LoginCredentials>();

        // We can put anything we want in our login credentials packet. See the example LoginCredentials packet in the included demo.
        packet.ServerVersion = _serverVersion;

        // If you're using Steamworks, you could include the local player's SteamID and have it authenticate with Steam.
        packet.Username = "Player";

        return packet;
    }

    public void OnDisconnected()
    {
        // We disconnected from the server.
    }

    public void OnShutdown()
    {
        // The application has shutdown.
    }

    public ClientHandler(int serverVersion)
    {
        _serverVersion = serverVersion;
    }
}
