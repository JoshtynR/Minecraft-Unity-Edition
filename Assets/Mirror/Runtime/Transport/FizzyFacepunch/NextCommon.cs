using Mirror;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public abstract class NextCommon
{
  protected const int MAX_MESSAGES = 256;
  // SteamSockets' limit includes our trailing channel byte. Legacy P2P has
  // different limits and does not use this framing.
  public const int ReliablePayloadLimit = 512 * 1024 - 1;
  public const int UnreliablePayloadLimit = 1200 - 1;
  private const int MaxQueuedBytes = 4 * 1024 * 1024;
  private sealed class Pending
  {
    public readonly Queue<byte[]> packets = new Queue<byte[]>();
    public int bytes;
  }
  private readonly Dictionary<Connection, Pending> outgoing = new Dictionary<Connection, Pending>();

  protected Result SendSocket(Connection conn, byte[] data, int channelId)
  {
    int limit = channelId == Channels.Reliable ? ReliablePayloadLimit : UnreliablePayloadLimit;
    if (data == null || data.Length == 0 || data.Length > limit ||
        (channelId != Channels.Reliable && channelId != Channels.Unreliable))
    {
      Debug.LogError($"Invalid Steam packet: bytes={data?.Length}, channel={channelId}, payload limit={limit}.");
      return Result.InvalidParam;
    }
    var packet = new byte[data.Length + 1];
    Buffer.BlockCopy(data, 0, packet, 0, data.Length);
    packet[data.Length] = (byte)channelId;
    // Never discard a reliable Mirror batch when Steam's native send buffer is
    // temporarily full, or let later batches overtake it.
    if (channelId == Channels.Reliable && outgoing.TryGetValue(conn, out var pending))
      return Enqueue(pending, packet);
    var result = SendPacket(conn, packet, channelId);
    if (result == Result.LimitExceeded && channelId == Channels.Reliable)
    {
      pending = new Pending();
      outgoing.Add(conn, pending);
      return Enqueue(pending, packet);
    }
    return result;
  }

  private static Result Enqueue(Pending pending, byte[] packet)
  {
    if (pending.bytes + packet.Length > MaxQueuedBytes) return Result.LimitExceeded;
    pending.packets.Enqueue(packet);
    pending.bytes += packet.Length;
    return Result.OK;
  }

  private static Result SendPacket(Connection conn, byte[] packet, int channelId)
  {
    var pinned = GCHandle.Alloc(packet, GCHandleType.Pinned);
    try
    {
      return conn.SendMessage(pinned.AddrOfPinnedObject(), packet.Length,
        channelId == Channels.Unreliable ? SendType.Unreliable : SendType.Reliable);
    }
    finally { pinned.Free(); }
  }

  protected Result FlushSocket(Connection conn)
  {
    if (outgoing.TryGetValue(conn, out var pending))
    {
      while (pending.packets.Count > 0)
      {
        var packet = pending.packets.Peek();
        var result = SendPacket(conn, packet, Channels.Reliable);
        if (result == Result.LimitExceeded) return conn.Flush(); // retry next frame
        if (result != Result.OK) return result;
        pending.packets.Dequeue();
        pending.bytes -= packet.Length;
      }
      outgoing.Remove(conn);
    }
    return conn.Flush();
  }

  protected void ForgetSocket(Connection conn) { outgoing.Remove(conn); }
  protected void ForgetAllSockets() { outgoing.Clear(); }

  protected (byte[], int) ProcessMessage(IntPtr ptrs, int size)
  {
    if (size < 2 || size > ReliablePayloadLimit + 1) return (null, -1);
    int channel = Marshal.ReadByte(ptrs, size - 1);
    if ((channel != Channels.Reliable && channel != Channels.Unreliable) ||
        (channel == Channels.Unreliable && size > UnreliablePayloadLimit + 1)) return (null, -1);
    var data = new byte[size - 1];
    Marshal.Copy(ptrs, data, 0, data.Length);
    return (data, channel);
  }
}
