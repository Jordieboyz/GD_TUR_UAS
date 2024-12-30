
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameGUI {
    
    /*------------------------------------------------------------------
        HomeScreenActions class
            Implementations for the MainScreen button functionality
    --------------------------------------------------------------------*/    
    public class HomeScreenActions : MonoBehaviour
    {
        public Animator homeScreenAnimator; 

        // AnimationTrigger handles
        private string staticSettings;
        private string gameSettings;
        private string mainMenu;

        private void Start()
        {
            mainMenu = homeScreenAnimator.parameters[0].name;
            gameSettings = homeScreenAnimator.parameters[1].name;
            staticSettings = homeScreenAnimator.parameters[2].name;
        }

        private void Update()
        {
            if(!homeScreenAnimator.GetBool(mainMenu)){
                if(Input.anyKeyDown && Input.GetKeyDown(KeyCode.Escape) == false)
                {
                    homeScreenAnimator.SetBool(mainMenu, true);
                }
            } else 
            {
                HandleBackMotion();
            }
        }
        
        public void PlayGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        
        public void ToggleSettigs()
        {
            Toggle(staticSettings);
        }

        public void ToggleStartGameSettigs()
        {
            Toggle(gameSettings);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void Apply()
        {
            Debug.Log("Implement Apply Button!");
        }

        private void Toggle(string pName)
        {
            homeScreenAnimator.SetBool(pName, !homeScreenAnimator.GetBool(pName));
        }

        private void HandleBackMotion(KeyCode defaultKey = KeyCode.Escape)
        {
            if(Input.GetKeyDown(defaultKey))
            {
                if(homeScreenAnimator.GetBool(gameSettings))
                {
                    Toggle(gameSettings);
                } 
                else if(homeScreenAnimator.GetBool(staticSettings))
                {
                    Toggle(staticSettings);
                }
                else {
                    homeScreenAnimator.SetBool(mainMenu, false);
                }
            }
        }
    }
}
