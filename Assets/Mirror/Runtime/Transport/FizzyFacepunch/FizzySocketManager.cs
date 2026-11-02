using Steamworks;
using Steamworks.Data;
using System;

public class FizzySocketManager : SocketManager
{
  public Action<Connection, IntPtr, int> ForwardMessage;
  public Action<Connection, ConnectionInfo> ForwardState;

  // SocketManager routes only this listener's connections. Do not also subscribe
  // to the global Steam event: its default OnConnecting already accepts peers.
  public override void OnConnecting(Connection connection, ConnectionInfo info)
  {
    ForwardState?.Invoke(connection, info);
  }
  public override void OnConnected(Connection connection, ConnectionInfo info)
  {
    base.OnConnected(connection, info); // attach Steam's receive poll group
    ForwardState?.Invoke(connection, info);
  }
  public override void OnDisconnected(Connection connection, ConnectionInfo info)
  {
    base.OnDisconnected(connection, info);
    ForwardState?.Invoke(connection, info);
  }
  public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
  {
    ForwardMessage?.Invoke(connection, data, size);
  }
}
