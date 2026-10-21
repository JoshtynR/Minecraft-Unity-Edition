using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EscapeMenuManager : MonoBehaviour
{
    private void Start()
    {
        transform.Find("Layout/Return").GetComponent<Button>().onClick.AddListener(Return);
        transform.Find("Layout/Quit").GetComponent<Button>().onClick.AddListener(Quit);
    }
    
    private void Quit()
    {ExitToTitle();}

    private static bool exitInProgress;
    public static async void ExitToTitle()
    {
        if(exitInProgress)return;
        exitInProgress=true;
        try
        {
        if(NetworkServer.active||!NetworkClient.active)await World.SaveAllDimensionsAsync();
        if(LobbyManager.currentLobby.HasValue)  LobbyManager.currentLobby.Value.Leave();
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(NetworkManager.singleton!=null&&NetworkServer.active)NetworkManager.singleton.StopHost();
        else if(NetworkManager.singleton!=null&&NetworkClient.active)NetworkManager.singleton.StopClient();
        else SceneManager.LoadScene("MainMenu");
        }
        catch(System.Exception error){Debug.LogException(error);}
        finally{exitInProgress=false;}
    }
    
    private void Return()
    {
        Cursor.lockState = CursorLockMode.Locked;
        GameManager.Instance.localPlayer.escapeMenuOpen = false;
    }
}
