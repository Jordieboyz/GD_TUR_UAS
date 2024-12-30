using UnityEngine;
using static Utils.Static;

namespace GameGUI
{
    public class SplitTroopsPositionChange : StateMachineBehaviour
    {
        /*
        * Puts the BuyTroops PopUp at the right place, so it doesnt show up at the wrong place for a frame
        */
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            SplitTroops splitTroopsScript = Assets.SplitTroopPopUp.GetComponent<SplitTroops>();
            splitTroopsScript.UpdatePosition();
        }
    }
}
