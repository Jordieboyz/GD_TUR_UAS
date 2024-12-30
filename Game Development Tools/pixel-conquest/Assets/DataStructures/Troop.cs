using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Utils.Static;

namespace DataStructures
{
    public class Troop : MonoBehaviour
    {
        public static List<Troop> Troops = new();
        public int OwnerID { get; set; }
        
        public int TroopCount { get; set; }
        public Vector3Int Position { get; set; }

        private TMP_Text troopCount;
                
        private void Start()
        {            
            Troops.Add(this);
            troopCount = Instantiate(Assets.TroopCountPrefab, Assets.TroopCountParent.transform).GetComponent<TMP_Text>();
            UpdateTroopCounter();
        }

        private void Update()
        {
            troopCount.transform.position = Camera.main.WorldToScreenPoint(transform.position);
        }

        public void AddTroops(int amount)
        {
            TroopCount += amount;
            UpdateTroopCounter();
        }
        
        public void RemoveTroops(int amount)
        {
            TroopCount -= amount;
            
            if (TroopCount <= 0)
            {
                Destroy(gameObject);
            }
            
            UpdateTroopCounter();
        }

        private void UpdateTroopCounter(){
            troopCount.text = TroopCount.ToString();
        }
        
        private void OnDestroy()
        {
            Destroy(troopCount);
            Troops.Remove(this);
        }
    }
}