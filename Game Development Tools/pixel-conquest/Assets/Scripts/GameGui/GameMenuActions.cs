// Unity Libraries
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameGUI 
{
    
    /*------------------------------------------------------------------
        GameMenuActions class
            Implementations for the in-game button functionality
    --------------------------------------------------------------------*/
    public class GameMenuActions : MonoBehaviour
    {
        // For now this works, will rework when there is enough time.
        public static bool isPaused = false;

        public Animator pauseScreenAnimator; 
        public GameObject pauseMenuPopUp;
        public GameObject pauseButton;

        // AnimationTrigger handles
        private string settingsPopUpName;


        private void Start()
        {
            settingsPopUpName = pauseScreenAnimator.parameters[0].name;
        }

        private void Update()
        {
            
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenuPopUp();
            }
        }
        
        private void UpdatePauseState(){
            isPaused = pauseScreenAnimator.GetBool(settingsPopUpName);
        }

        public void TogglePauseMenuPopUp()
        {
            pauseScreenAnimator.SetBool(settingsPopUpName, !pauseScreenAnimator.GetBool(settingsPopUpName));
            UpdatePauseState();
        }

        public void Resume()
        {
            pauseScreenAnimator.SetBool(settingsPopUpName, false);
            UpdatePauseState();
        }

        public void Quit()
        {
            isPaused = false;
            PlayerManager.ResetGame();
            SceneManager.LoadScene(0);
        }
    }
}
