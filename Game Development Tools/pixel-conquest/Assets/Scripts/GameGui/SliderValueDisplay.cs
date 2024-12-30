// Unity Libraries
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameGUI 
{     
    /*------------------------------------------------------------------
        SliderValueDisplay class
            Use a TMP_Inputfield to display and change value of the slider 
    --------------------------------------------------------------------*/
    [RequireComponent(typeof(Slider))]
    public class SliderValueDisplay : MonoBehaviour
    {
        private Slider slider;
        public TMP_InputField textDisplay;

        void Awake()
        {
            slider = GetComponent<Slider>();
            textDisplay.text = slider.value.ToString();

            slider.onValueChanged.AddListener(val => { textDisplay.text =  val.ToString("0"); });
            textDisplay.onValueChanged.AddListener(val => { slider.value =  int.Parse(val); });
        }
    }
}