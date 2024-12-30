using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
 
public class MainMenuActions : MonoBehaviour
{

    public void SinglePlayer(){
        SceneManager.MoveGameObjectToScene(NetworkManager.Singleton.gameObject, SceneManager.GetActiveScene());
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame(){
        Application.Quit();
    }

    
}
