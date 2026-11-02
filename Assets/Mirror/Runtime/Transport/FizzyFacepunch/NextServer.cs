using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mirror.FizzySteam
{
  public class NextServer : NextCommon, IServer
  {
    private event Action<int> OnConnected;
    private event Action<int, byte[], int> OnReceivedData;
    private event Action<int> OnDisconnected;
    private event Action<int, Exception> OnReceivedError;
    private readonly BidirectionalDictionary<Connection, int> connToMirrorID = new BidirectionalDictionary<Connection, int>();
    private readonly BidirectionalDictionary<SteamId, int> steamIDToMirrorID = new BidirectionalDictionary<SteamId, int>();
    private readonly ConcurrentQueue<Action> statusChanges = new ConcurrentQueue<Action>();
    private readonly HashSet<Connection> pendingConnections = new HashSet<Connection>();
    private readonly int maxConnections;
    private int nextConnectionID = 1;
    private bool stopped;
    private FizzySocketManager listenSocket;

    private NextServer(int maxConnections) { this.maxConnections = maxConnections; }

    public static NextServer CreateServer(FizzyFacepunch transport, int maxConnections)
    {
      var server = new NextServer(maxConnections);
      server.OnConnected += id => transport.OnServerConnected.Invoke(id);
      server.OnDisconnected += id => transport.OnServerDisconnected.Invoke(id);
      server.OnReceivedData += (id, data, ch) => transport.OnServerDataReceived.Invoke(id, new ArraySegment<byte>(data), ch);
      server.OnReceivedError += (id, error) => transport.OnServerError.Invoke(id, error);
      if (!SteamClient.IsValid) throw new InvalidOperationException("SteamWorks not initialized.");
      server.listenSocket = SteamNetworkingSockets.CreateRelaySocket<FizzySocketManager>();
      server.listenSocket.ForwardMessage = server.OnMessageReceived;
      server.listenSocket.ForwardState = (conn, info) => server.statusChanges.Enqueue(() => server.OnConnectionStatusChanged(conn, info));
      return server;
    }

    private void OnConnectionStatusChanged(Connection conn, ConnectionInfo info)
    {
      if (stopped) return;
      SteamId steamId = info.Identity.SteamId;
      if (info.State == ConnectionState.Connecting)
      {
        if (connToMirrorID.Contains(conn) || pendingConnections.Contains(conn)) return;
        if (connToMirrorID.Count + pendingConnections.Count >= maxConnections || steamIDToMirrorID.Contains(steamId))
        { conn.Close(false, 0, "Server full or account already connected"); return; }
        pendingConnections.Add(conn);
        var result = conn.Accept();
        if (result != Result.OK)
        {
          pendingConnections.Remove(conn);
          conn.Close(false, 0, "Connection could not be accepted");
        }
      }
      else if (info.State == ConnectionState.Connected)
      {
        pendingConnections.Remove(conn);
        if (connToMirrorID.Contains(conn)) return;
        if (connToMirrorID.Count >= maxConnections || steamIDToMirrorID.Contains(steamId))
        { conn.Close(false, 0, "Server full or account already connected"); return; }
        int id = nextConnectionID++;
        connToMirrorID.Add(conn, id);
        steamIDToMirrorID.Add(steamId, id);
        OnConnected?.Invoke(id);
      }
      else if (info.State == ConnectionState.ClosedByPeer || info.State == ConnectionState.ProblemDetectedLocally || info.State == ConnectionState.None)
      {
        pendingConnections.Remove(conn);
        if (connToMirrorID.TryGetValue(conn, out int id)) InternalDisconnect(id, conn);
      }
    }

    private void InternalDisconnect(int id, Connection conn, Exception error = null)
    {
      if (!connToMirrorID.Contains(id)) return;
      // Remove first. Mirror's callback may disconnect/send/stop the server again.
      connToMirrorID.Remove(id);
      steamIDToMirrorID.Remove(id);
      ForgetSocket(conn);
      conn.Close(false, 0, "Disconnected");
      try { if (error != null) OnReceivedError?.Invoke(id, error); }
      finally { OnDisconnected?.Invoke(id); }
    }

    public void Disconnect(int id)
    {
      if (connToMirrorID.TryGetValue(id, out Connection conn)) InternalDisconnect(id, conn);
    }

    public void FlushData()
    {
      // Error callbacks can remove peers, including other peers or the whole host.
      foreach (var conn in connToMirrorID.FirstTypes.ToArray())
      {
        if (!connToMirrorID.TryGetValue(conn, out int id)) continue;
        var result = FlushSocket(conn);
        if (result != Result.OK && result != Result.Ignored) FailSend(id, conn, result);
      }
    }

    public void ReceiveData()
    {
      while (!stopped && statusChanges.TryDequeue(out var callback)) callback();
      if (!stopped) listenSocket?.Receive(MAX_MESSAGES);
    }

    private void OnMessageReceived(Connection conn, IntPtr pointer, int size)
    {
      if (stopped || !connToMirrorID.TryGetValue(conn, out int id)) return;
      var packet = ProcessMessage(pointer, size);
      if (packet.Item1 == null) { InternalDisconnect(id, conn); return; }
      OnReceivedData?.Invoke(id, packet.Item1, packet.Item2);
    }

    public void Send(int id, byte[] data, int channelId)
    {
      // Mirror can finish flushing an already-built batch after a disconnect.
      if (!connToMirrorID.TryGetValue(id, out Connection conn)) return;
      var result = SendSocket(conn, data, channelId);
      if (result == Result.OK || result == Result.Ignored ||
          (channelId == Channels.Unreliable && result == Result.LimitExceeded)) return;
      FailSend(id, conn, result);
    }

    private void FailSend(int id, Connection conn, Result result)
    {
      Debug.LogWarning($"Steam send failed: {result}, connection={id}. Closing connection.");
      InternalDisconnect(id, conn, new Exception("Steam send failed: " + result));
    }

    public string ServerGetClientAddress(int id)
    {
      return steamIDToMirrorID.TryGetValue(id, out SteamId steamId) ? steamId.ToString() : string.Empty;
    }

    public void Shutdown()
    {
      if (stopped) return;
      stopped = true;
      if (listenSocket != null)
      {
        listenSocket.ForwardState = null;
        listenSocket.ForwardMessage = null;
      }
      foreach (var conn in connToMirrorID.FirstTypes.ToArray())
        if (connToMirrorID.TryGetValue(conn, out int id)) InternalDisconnect(id, conn);
      foreach (var conn in pendingConnections) conn.Close(false, 0, "Server stopped");
      pendingConnections.Clear();
      ForgetAllSockets();
      while (statusChanges.TryDequeue(out _)) { }
      listenSocket?.Close();
      listenSocket = null;
    }
  }
}
