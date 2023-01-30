using Lidgren.Network;
using zapnet;

public class LoginCredentials : INetworkPacket
{
    public int ServerVersion;
    public string Username;

    public virtual void Write(NetOutgoingMessage buffer)
    {
        // We'll write the server version and username into the buffer ready for sending to the client.
        buffer.Write(ServerVersion);
        buffer.Write(Username);
    }

    public virtual bool Read(NetIncomingMessage buffer)
    {
        // We'll read the server version and username we wrote on the server.
        ServerVersion = buffer.ReadInt32();
        Username = buffer.ReadString();

        return true;
    }

    // We must implement this, its called whenever a network packet is recycled and put back into its pool.
    public void OnRecycled() { }

    // We must implement this, its called whenever a network packet is fetched from its pool.
    public void OnFetched() { }
}