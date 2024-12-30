using System.Collections.Generic;
using Controllers;
using UnityEngine;

namespace Managers
{
    public class PlayerManager : MonoBehaviour
    {
        private static List<PlayerController> _players;
        private static int _nextPlayerID = 0;

        private void Awake()
        {
            _players = new();
        }
        
        public static PlayerController RegisterPlayer(PlayerController player)
        {
            player.SetID(_nextPlayerID);
            _players.Add(player);
            _nextPlayerID++;
            return player;
        }
        
        public static PlayerController GetLocalPlayer(){
            return _players[0];
        }
        
        public static void UnregisterPlayer(PlayerController player)
        {
            _players.Remove(player);
        }
        
        public static List<PlayerController> GetPlayers()
        {
            return _players;
        }
        
        public static PlayerController GetPlayer(int id)
        {
            return _players.Find(player => player.Id == id);
        }

        public static void ResetGame()
        {
            _players.Clear();
            _nextPlayerID = 0;
        }
    }
}
