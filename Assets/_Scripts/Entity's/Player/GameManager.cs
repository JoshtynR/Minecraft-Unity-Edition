using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GameObject playerPrefab;
    public Player localPlayer;
    public List<Player> players = new List<Player>();
    public Vector3Int playerChunkPosition;
    private Vector3Int currentChunkCenter = Vector3Int.zero;
    private Coroutine chunkLoadingCoroutine;
    private World chunkLoadingWorld;
    public bool playerSpawned = false;

    public World world;

    public float detectionTime = 1;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (chunkLoadingCoroutine != null) StopCoroutine(chunkLoadingCoroutine);
        chunkLoadingCoroutine = null;
        chunkLoadingWorld = null;
    }

    public void StartCheckingForChunks()
    {
        if (localPlayer == null || world == null) return;
        if (chunkLoadingCoroutine != null) return;
        chunkLoadingWorld = world;
        SetCurrentChunkCenter();
        chunkLoadingCoroutine = StartCoroutine(CheckForChunkLoading());
    }

    private void SetCurrentChunkCenter()
    {
        playerChunkPosition = WorldDataHelper.GetChunkPosition(world, Vector3Int.RoundToInt(localPlayer.transform.position));
        currentChunkCenter.x = playerChunkPosition.x + world.chunkSize / 2;
        currentChunkCenter.y = playerChunkPosition.y + world.chunkHeight / 2;
        currentChunkCenter.z = playerChunkPosition.z + world.chunkSize / 2;
    }

    public Vector3Int GetCurrentChunkCenter()
    {
        return WorldDataHelper.GetChunkPosition(world, Vector3Int.RoundToInt(localPlayer.transform.position));
    }

    private IEnumerator CheckForChunkLoading()
    {
        while (localPlayer != null && world != null)
        {
            if (chunkLoadingWorld != world)
            {
                chunkLoadingWorld = world;
                SetCurrentChunkCenter();
            }

            if (Mathf.Abs(currentChunkCenter.x - localPlayer.transform.position.x) > world.chunkSize / 2 ||
                Mathf.Abs(currentChunkCenter.z - localPlayer.transform.position.z) > world.chunkSize / 2)
            {
                SetCurrentChunkCenter();
                world.LoadAdditionalChunks(localPlayer.gameObject);
            }
            yield return null;
        }
        chunkLoadingCoroutine = null;
    }
}
