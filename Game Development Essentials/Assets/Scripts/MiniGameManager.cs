
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public enum MiniGames { Cross, Maze, BombsOut, ColorBoard, };

public class MiniGameManager : NetworkBehaviour
{
    // MiniGame Prefabs
    private GameObject SelectedPrefab;

    public GameObject CrossPrefab;
    public GameObject MazePrefab;
    public GameObject BombsPrefab;

    // General Game prefabs
    public GameObject PlayerPrefab;

    // tilemap tiles
    public Tilemap mainTileGrid;
    public Tile tile;
    public Tile CrossWrongTile;

    public TMP_Text TimerTextField;

    public Button CrossButton;
    public Button MazeButton;
    public Button bombsButton;

    public GameObject GameSelectionMenu;
    public GameObject GameSelectionTilemap;

    public bool currGameDone = true;
    public bool SinglePlayer = true;
    void Start()
    {   
        if(!(SinglePlayer = NetworkManager == null)){
            SceneManager.MoveGameObjectToScene(NetworkManager.gameObject, SceneManager.GetActiveScene());
            if(IsHost){
                CrossButton.onClick.AddListener(() => { SelectedPrefab = CrossPrefab; PlayGameServerRpc(); });
                MazeButton.onClick.AddListener(() => { SelectedPrefab = MazePrefab; PlayGameServerRpc(); });
                bombsButton.onClick.AddListener(() => { SelectedPrefab = BombsPrefab; PlayGameServerRpc(); });
            } else {
                CrossButton.interactable = false;
                MazeButton.interactable = false;
                bombsButton.interactable = false;
            }
        } else {
            var singlePlayer = Instantiate(PlayerPrefab);
            
            // signleplayer
            CrossButton.onClick.AddListener(() => { 
                    SelectedPrefab = CrossPrefab; 
                    CreateGame();
                    PlayGame(); 
            });
            MazeButton.onClick.AddListener(() => { 
                    SelectedPrefab = CrossPrefab; 
                    CreateGame();
                    PlayGame(); 
            });
            bombsButton.onClick.AddListener(() => { 
                    SelectedPrefab = CrossPrefab; 
                    CreateGame();
                    PlayGame(); 
            });
        }

        // setup the collision for every PlayerController
        mainTileGrid =  AddAndGetTilemap("mainGrid");
        PlayerController.borders = mainTileGrid;
    }

    
    private void Update(){
        if(currGameDone){
            ClearAllTilemapTiles(mainTileGrid);
            GameSelectionMenu.SetActive(true);
            GameSelectionTilemap.SetActive(true);
            currGameDone = false;
        }
    }

    public async void QuitGame(){
        if(SinglePlayer){
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        } else {
            if(NetworkManager.LocalClientId == 0){
                QuitGameServerRpc();
            }

            NetworkManager.Shutdown();
            while(NetworkManager.ShutdownInProgress){
                await Task.Yield();
            }

            Destroy(NetworkManager.gameObject);

            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }

    // should only be run by the host bcs of spawn
    public void CreateGame()
    { 
        if(SinglePlayer){
            Instantiate(SelectedPrefab);
        } else{ 
            Instantiate(SelectedPrefab).GetComponent<NetworkObject>().Spawn();
        }
    }
    
    
    // should be run on all clients
    public async void PlayGame()
    {
        GameSelectionMenu.SetActive(false);
        GameSelectionTilemap.SetActive(false);

        PlayerController clientPlayer;
        if(SinglePlayer){
            clientPlayer = PlayerController.players[0];
        } else {
            clientPlayer = PlayerController.players[(int)NetworkManager.LocalClientId];
        }
        MiniGame mGame = GameObject.FindGameObjectWithTag(nameof(Cross)).GetComponent<MiniGame>();
        mGame.Initialize(this, clientPlayer);
        await mGame.StartGame();

        // only host is allowed to destroy networkobjects
        // gotta make sure the game is done for all clients first!
        if(IsHost || SinglePlayer){
            Destroy(mGame);
            Destroy(mGame.gameObject);
        }
    }

    // host = server = client logic!
    [ServerRpc]
    public void PlayGameServerRpc()
    {
        CreateGame();
        PlayGame();
        PlayGameClientRpc();
    }

    [ClientRpc]
    private void PlayGameClientRpc()
    {
        if(!IsHost)
        {
            PlayGame();
        }
    }


    // General helper functions
    public Tilemap AddAndGetTilemap(string name)
    {
        GameObject newTM = new(name, typeof(Tilemap), typeof(TilemapRenderer));
        newTM.transform.SetParent(GameObject.Find("Grid").transform);
        return newTM.GetComponent<Tilemap>();
    }

    private void ClearAllTilemapTiles(Tilemap tm){
        tm.ClearAllTiles();
    }

    [ServerRpc]
    public void QuitGameServerRpc(){
        QuitGameClientRpc();
    }

    [ClientRpc]
    public void QuitGameClientRpc(){
        if(!IsHost){
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }
}