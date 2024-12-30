using UnityEngine;

namespace GameGUI {
    public class HideAllPopUps : StateMachineBehaviour

    {
        /*
        * hide all objects with the tag: popup, when the pause button is pressed
        **/
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //find method is ok here, since this method only runs when pause button is pressed
            GameObject[] popUps = GameObject.FindGameObjectsWithTag("popup"); 
            foreach (GameObject popUp in popUps)
            {
                try
                {
                    popUp.GetComponent<Animator>().SetBool("show", false);
                }
                catch
                {
                    Debug.Log("the popup " + popUp.name + " does not have a show bool in its animator. please add a way to close it, when pause button is pressed");
                }
            }
        }
    }
}
