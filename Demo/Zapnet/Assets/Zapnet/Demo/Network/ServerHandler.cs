using UnityEngine;
using Lidgren.Network;
using System;
using zapnet;

public class ServerHandler : IServerHandler
{
    private int _serverVersion;

    public void WriteInitialData(Player player, NetOutgoingMessage buffer)
    {
        // We can write any additional data we want clients to receive to the buffer.
        buffer.Write("Hello, player!");
    }

    public bool CanPlayerAuth(INetworkPacket data)
    {
        var credentials = (LoginCredentials)data;

        // Don't let the player authenticate if their version is different from ours.
        if (credentials.ServerVersion < _serverVersion)
        {
            return false;
        }
		
		return true;
    }

    public void OnPlayerDisconnected(Player player)
    {
        var entity = (player.Entity as BasePlayer);

        if (entity != null)
        {
            // The player disconnected so remove their entity.
            //Zapnet.Entity.Remove(entity);
        }
    }

    public void OnInitialDataReceived(Player player, INetworkPacket data)
    {
        var credentials = (LoginCredentials)data;

        Debug.Log(credentials.Username + " has connected!");

        CreatePlayer(player, credentials);
    }

    public void OnPlayerConnected(Player player, INetworkPacket data)
    {
        
    }

    public void CreatePlayer(Player player, LoginCredentials credentials)
    {
        // We want to create an entity to represent this player and assign control to them.
        var entity = Zapnet.Entity.Create<BasePlayer>("PlayerEntity");
        entity.Controller.onValueChanged += () =>
        {
            Debug.Log("changed: " + entity.Controller.Value + " from " + entity.Controller.LastValue);
        };

        entity.Name.Value = credentials.Username;
        entity.AssignControl(player);

        entity.transform.position = new Vector3(0f, 0.025f, 0f);
    }

    public ServerHandler(int serverVersion)
    {
        _serverVersion = serverVersion;
    }
}
