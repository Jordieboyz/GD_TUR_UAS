using System.Collections.Generic;
using DataStructures;
using Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Color = Enums.Color;
using static Utils.Static;
using GameGUI;
using Utils;

namespace Controllers
{
    public class GameController : MonoBehaviour
    {
        private readonly WorldManager _worldManager = new();

        public Resource GameTime { get; } = new();
        private Grid _grid;
        private float _time;

        // max is 6, because of the color limit.
        private float PlayersAmount;
        private static bool isRunning = true;
        private void Start()
        {
            _grid = FindObjectOfType<Grid>();
            _worldManager.Init();

            _worldManager.GeneratePerlinNoiseWorld();
            PlayersAmount = StaticGameSettings.Get<float>(nameof(PlayersAmount));
            List<Color> playerColors = GetRandColors((int)PlayersAmount);

            for (var i = 0; i < _worldManager.baseTiles.Count; i++)
            {
                CreatePlayer(playerColors[i], _worldManager.baseTiles[i],
                    GetAdjacentTiles(_worldManager.baseTiles[i]), i != 0);
            }

            Assets.GUIManager.GetComponent<GUIManager>().InitGame(this, PlayerManager.GetLocalPlayer());
            Assets.BuyTroopPopUp.GetComponent<BuyTroops>().Init();
            SetTime(0f);
        }

        private void Update()
        {
            if(isRunning)
                Tick();
        }
        
        private static void EndGame()
        {
            PlayerManager.ResetGame();
        }

        private static void ShowEndScreen(bool won, Color localPlayerColor){  
            Assets.EndScreenParent.SetActive(true);
            Assets.Banner.GetComponent<MergeSprite>().troopColor = ConvertColor(localPlayerColor);    
            Assets.Banner.SetActive(true); 

            Assets.VictoryBanner.SetActive(won);
            Assets.DefeatBanner.SetActive(!won);
        }

        public void GoToMainMenu(){
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
        
        private void Tick()
        {
            _time += Time.deltaTime;
            GameTime.Set((int)_time);
        }

        private void SetTime(float newTime)
        {
            _time = newTime;
            GameTime.Set((int)_time);
        }

        private void CreatePlayer(Color color, Vector3Int baseTile, List<Vector3Int> startingTiles,
            bool isAI = false)
        {
            var playerObject = Instantiate(
                isAI ? GameResources.Instance.EnemyPrefab : GameResources.Instance.PlayerPrefab,
                _grid.CellToWorld(baseTile), Quaternion.identity);

            var player = playerObject.GetComponent<PlayerController>();

            player.Color = color;
            player.SetStartTerritory(baseTile, startingTiles);

            PlayerManager.RegisterPlayer(player);
        }

        public static void RemovePlayer(PlayerController player)
        {
            if(PlayerManager.GetLocalPlayer() == player){
                ShowEndScreen(false, player.Color);
            }

            player.Defeat();
        
            PlayerManager.UnregisterPlayer(player);

            if (PlayerManager.GetPlayers().Count == 1)
            {
                isRunning = false;
                if(PlayerManager.GetPlayers()[0] == PlayerManager.GetLocalPlayer()){
                    ShowEndScreen(true, PlayerManager.GetPlayers()[0].Color);
                }
                EndGame();
            }
        }

        public void AddResourceListener(string res, UnityAction<int> method)
        {
            switch (res)
            {
                case nameof(GameTime):
                    GameTime.OnValueChanged.AddListener(method);
                    break;
                default: break;
            }
        }
    }
}