# SteamSockets transport regression checks

Run `python Tools/SteamTransportChecks/run.py --mcs <compiler> --mono <runtime>`.
The runner compiles the production SteamSockets client, server, shared framing,
Facepunch manager adapters, transport entry point and Mirror Transport base against
the **bundled** Facepunch Win64 DLL. It then runs those same sources with native
Steam and Unity doubles. Legacy P2P entry points are stubbed; their implementation
is unchanged and is not executed by this suite.

The previous adapter advertised a 1 MiB reliable limit and used that size as its
normal Mirror batching threshold. SteamSockets only permits 512 KiB per message;
the adapter also appends a channel byte. This could produce `InvalidParam` during
startup broadcasts. Modern reliable payloads are now limited to 524287 bytes,
unreliable payloads to 1199 bytes, and normal batches to 1199 bytes. Large individual
reliable messages can still use Steam's built-in fragmentation within the maximum.
Oversize packets log their length, channel and permitted payload size.

The old client/server also subscribed to global connection-status callbacks.
Facepunch runs these asynchronously, while Mirror's connection dictionaries and
Unity APIs require the main thread. The adapters now receive status from their
own SocketManager/ConnectionManager, queue it, and drain in the transport's early
update before delivering messages. The listener's OnConnecting overrides automatic
SDK acceptance, so each incoming connection is accepted once, with capacity checks.
Closed peers and locally failed routes retire both transport mappings and Mirror
connections once. Disconnect removes mappings before callbacks, tolerates reentry,
and ignores remaining already-built batches. Client timeout/cancel and failed
initialization cannot deliver duplicate disconnections or revive an old connection.

Reliable batches encountering temporary `LimitExceeded` are copied into a bounded
4 MiB queue per connection and retried in order during late updates. Unreliable
packets may be dropped under pressure. Permanent send failures or queue overflow
close the connection explicitly, rather than silently dropping reliable state.
Shutdown releases queued packets, pending handshakes and established sockets.
Transport client/server fields now belong to the transport instance; destroying
an unused scene-local copy cannot shut down the persistent instance's session.

Checks cover the packet boundaries/framing, reliable retries/order/queue bounds,
callbacks arriving from background threads, duplicate/late status events, the
reported `InvalidParam` followed by repeated sends, exactly-once error/disconnect
ordering, reentrant disconnects, local route failures, pending connection capacity,
client cancellation/timeouts/startup failure, and instance isolation. These are
native-free checks, not a real Steam relay test or Unity scene-loading test.

For player validation, update **both builds**, restart them, and repeat the same
friend-host join. Check that scene loading and initial entity replication finish,
then disconnect and rejoin. Try quitting the host during a guest connection and
hosting again. If it still fails, collect the first error from both Player.log
files; the new transport diagnostics include send failures and packet limits.

Steam's documented limit/result meanings:
- https://partner.steamgames.com/doc/api/steamnetworkingtypes
- https://partner.steamgames.com/doc/api/ISteamNetworkingSockets
