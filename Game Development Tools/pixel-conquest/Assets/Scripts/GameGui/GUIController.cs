// Unity Classes
using TMPro;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;

// Local Classes
using Utils;
using Controllers;

namespace GameGUI 
{
    /*------------------------------------------------------------------
        GUIManager class
            Implementation to dynamically link an UI element with 
            their respective source component
    --------------------------------------------------------------------*/
    public class GUIManager : MonoBehaviour
    {
        public PairedGameObject[] StaticGUIElements;
        
        // we need this to force an initialisation order...
        private bool init_done = false;
        private void Update(){
            if(!init_done){ InitGUI(); init_done = true;};
        }

        // Link GUI elements to the "main" game based on a player
        // TODO: Add more display types, only supported: TMP_Text
        public void InitGame(GameController game, PlayerController localPlayer)
        {
            foreach(var item in StaticGUIElements)
            {
                if(item.PPSetting)
                {
                    continue;
                }
                
                // Manage UI elements to direct Players Resources
                if(item.obj.TryGetComponent<TMP_Text>(out var text))
                {
                    PropertyInfo prop;
                    if( (prop = Static.GetPropertyOfClass<PlayerController>(item.obj.name)) != null  )
                    {
                        text.text = prop.GetValue(localPlayer).ToString();
                        localPlayer.AddResourceListener(item.obj.name, val => { text.text = val.ToString();});
                    } 
                    else if((prop = Static.GetPropertyOfClass<GameController>(item.obj.name)) != null)
                    {
                        text.text = prop.GetValue(game).ToString();
                        game.AddResourceListener(item.obj.name, val => { text.text = val.ToString();});
                    }
                }
            }
        }

        // Link GUI elements to the global settings variables
        // TODO: Add more display types, only supported: TMP_InputField, TMP_Slider
        public void InitGUI()
        {
            // get at start, add listener on change or click
            foreach(var item in StaticGUIElements)
            {
                if(!item.PPSetting)
                {
                    continue;
                }
                
                if(item.obj.TryGetComponent<TMP_InputField>(out var inputField))
                {   
                    if(item.StaticPlayerSetting)
                    {
                        inputField.text = StaticSettings.Get<string>(item.obj.name);
                        inputField.onValueChanged.AddListener(input => { StaticSettings.Set(input, item.obj.name);});
                    } 
                    else if(item.StaticGameSetting){
                        inputField.text = StaticGameSettings.Get<string>(item.obj.name);
                        inputField.onValueChanged.AddListener(input => { StaticGameSettings.Set(input, item.obj.name);});
                    }
                }
                // used for PlayerPrefs Settings
                else if(item.obj.TryGetComponent<Slider>(out var slider))
                {
                    if(item.StaticPlayerSetting){
                        slider.value = StaticSettings.Get<float>(item.obj.name);
                        slider.onValueChanged.AddListener(sliderval => { StaticSettings.Set(slider.value, item.obj.name);});
                    }
                    else if(item.StaticGameSetting){
                        slider.value = StaticGameSettings.Get<float>(item.obj.name);
                        slider.onValueChanged.AddListener(sliderval => { StaticGameSettings.Set(sliderval, item.obj.name);});
                    }
                }
                // used for PlayerPrefs Settings
                else if(item.obj.TryGetComponent<TMP_Text>(out var text))
                {   
                    if(item.StaticPlayerSetting){
                        text.text = StaticSettings.Get<string>(item.obj.name);
                    }
                    else if(item.StaticGameSetting){
                        text.text = StaticGameSettings.Get<string>(item.obj.name);
                    }

                }
                if(item.obj.TryGetComponent<Toggle>(out var toggle))
                {   
                    if(item.StaticPlayerSetting){
                        toggle.isOn = StaticSettings.Get<bool>(item.obj.name);
                        toggle.onValueChanged.AddListener(input => { StaticSettings.Set(input, item.obj.name);});
                    }
                    else if(item.StaticGameSetting){
                        toggle.isOn = StaticGameSettings.Get<bool>(item.obj.name);
                        toggle.onValueChanged.AddListener(input => { StaticGameSettings.Set(input, item.obj.name);});
                    }
                }
            }
        }        
    }
}