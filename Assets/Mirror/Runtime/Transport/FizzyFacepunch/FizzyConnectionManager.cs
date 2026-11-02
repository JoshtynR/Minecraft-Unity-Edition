using Steamworks;
using Steamworks.Data;
using System;

public class FizzyConnectionManager : ConnectionManager
{
  public Action<IntPtr, int> ForwardMessage;
  public Action<ConnectionInfo> ForwardState;

  public override void OnConnected(ConnectionInfo info)
  {
    base.OnConnected(info);
    ForwardState?.Invoke(info);
  }
  public override void OnDisconnected(ConnectionInfo info)
  {
    base.OnDisconnected(info);
    ForwardState?.Invoke(info);
  }
  public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
  {
    ForwardMessage?.Invoke(data, size);
  }
}
