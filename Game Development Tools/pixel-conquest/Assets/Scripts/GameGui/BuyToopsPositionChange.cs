using Managers;
using UnityEngine;
using static Utils.Static;

namespace GameGUI
{
    public class BuyToopsPositionChange : StateMachineBehaviour
    {
        // Puts the BuyTroops PopUp at the right place, so it doesnt show up at the wrong place for a frame
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            BuyTroops buyTroopsScript = Assets.BuyTroopPopUp.GetComponent<BuyTroops>();
            buyTroopsScript.UpdatePosition();
            buyTroopsScript.UpdateAmount();
            Camera.main.transform.position = Assets.BaseTilemap.GetCellCenterWorld(
                PlayerManager.GetLocalPlayer().GetBaseTile()
            );    
        }
    }
}
