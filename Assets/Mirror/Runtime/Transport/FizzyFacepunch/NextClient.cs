using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Mirror.FizzySteam
{
  public class NextClient : NextCommon, IClient
  {
    public bool Connected { get; private set; }
    public bool Error { get; private set; }
    private event Action<byte[], int> OnReceivedData;
    private event Action OnConnected;
    private event Action OnDisconnected;
    private readonly ConcurrentQueue<ConnectionInfo> statusChanges = new ConcurrentQueue<ConnectionInfo>();
    private readonly Stopwatch timer = Stopwatch.StartNew();
    private readonly double timeout;
    private FizzyConnectionManager manager;
    private bool stopped;
    private string startupError;

    private NextClient(FizzyFacepunch transport) { timeout = Math.Max(1, transport.Timeout); }

    public static NextClient CreateClient(FizzyFacepunch transport, string host)
    {
      var client = new NextClient(transport);
      client.OnConnected += () => transport.OnClientConnected.Invoke();
      client.OnDisconnected += () => transport.OnClientDisconnected.Invoke();
      client.OnReceivedData += (data, ch) => transport.OnClientDataReceived.Invoke(new ArraySegment<byte>(data), ch);
      // Delay failure notification until ReceiveData: the transport must assign
      // its client field before a Mirror callback can synchronously shut it down.
      try
      {
        if (!SteamClient.IsValid) throw new InvalidOperationException("SteamWorks not initialized.");
        ulong steamId;
        if (!ulong.TryParse(host, out steamId) || steamId == 0) throw new FormatException("Expected a host Steam ID.");
        client.manager = SteamNetworkingSockets.ConnectRelay<FizzyConnectionManager>(steamId);
        client.manager.ForwardMessage = client.OnMessageReceived;
        client.manager.ForwardState = info => client.statusChanges.Enqueue(info);
      }
      catch (Exception error) { client.startupError = error.Message; }
      return client;
    }

    private void OnMessageReceived(IntPtr pointer, int size)
    {
      if (!Connected || stopped) return;
      var packet = ProcessMessage(pointer, size);
      if (packet.Item1 == null) { Fail("Invalid Steam packet received."); return; }
      OnReceivedData?.Invoke(packet.Item1, packet.Item2);
    }

    public void ReceiveData()
    {
      if (stopped) return;
      if (startupError != null) { Fail(startupError); return; }
      // Steam's async callbacks never call Mirror/Unity or edit our connection
      // state. Drain only this connection manager's events on the Unity thread.
      while (!stopped && statusChanges.TryDequeue(out var info))
      {
        if (info.State == ConnectionState.Connected && !Connected)
        {
          Connected = true;
          OnConnected?.Invoke();
        }
        else if (info.State == ConnectionState.ClosedByPeer || info.State == ConnectionState.ProblemDetectedLocally || info.State == ConnectionState.None)
        { Fail("Steam connection closed: " + info.State + " (" + info.EndReason + ")."); }
      }
      if (stopped) return;
      if (!Connected && timer.Elapsed.TotalSeconds >= timeout) { Fail("Steam connection timed out."); return; }
      if (Connected) manager?.Receive(MAX_MESSAGES);
    }

    public void Send(byte[] data, int channelId)
    {
      if (!Connected || stopped || manager == null) return;
      var result = SendSocket(manager.Connection, data, channelId);
      if (result == Result.OK || result == Result.Ignored ||
          (channelId == Mirror.Channels.Unreliable && result == Result.LimitExceeded)) return;
      Fail("Steam send failed: " + result);
    }

    public void FlushData()
    {
      if (!Connected || stopped || manager == null) return;
      var result = FlushSocket(manager.Connection);
      if (result != Result.OK && result != Result.Ignored) Fail("Steam flush failed: " + result);
    }

    private void Fail(string message)
    {
      if (stopped) return;
      Error = true;
      Debug.LogWarning(message);
      Disconnect();
      OnDisconnected?.Invoke();
    }

    public void Disconnect()
    {
      if (stopped) return;
      stopped = true;
      Connected = false;
      timer.Stop();
      var oldManager = manager;
      manager = null;
      if (oldManager != null)
      {
        oldManager.ForwardState = null;
        oldManager.ForwardMessage = null;
        oldManager.Close();
      }
      ForgetAllSockets();
      while (statusChanges.TryDequeue(out _)) { }
    }
  }
}
