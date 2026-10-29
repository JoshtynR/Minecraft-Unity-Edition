using System.Collections;
using Mirror;

public partial class Player
{
    private bool playerIdentityPublished;

    // Start can run while Mirror is still completing a scene/host transition.
    // Commands sent then are dropped, even on the in-process single-player host.
    // Unity stops this coroutine on object destruction; also stop waiting when
    // disconnected or no longer the local player. Never force Ready() here.
    private IEnumerator PublishPlayerIdentityWhenReady()
    {
        if(NetworkServer.active&&isLocalPlayer)
        {
            PlayerName=PlayerIdentity.DisplayName;SteamId=PlayerIdentity.Id;
            playerIdentityPublished=true;yield break;
        }
        while (NetworkClient.active && isLocalPlayer && !playerIdentityPublished)
        {
            if (NetworkClient.ready && NetworkClient.isConnected &&
                !NetworkClient.isLoadingScene && NetworkClient.connection != null && netId != 0)
            {
                playerIdentityPublished = true;
                CmdSetPlayerServerData(PlayerIdentity.DisplayName, PlayerIdentity.Id);
                yield break;
            }
            yield return null;
        }
    }
}
