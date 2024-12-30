// Unity Libraries
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameGUI 
{
    /*------------------------------------------------------------------
        ButtonStateHandler class
            Determine states of buttons based on their respective events 
    --------------------------------------------------------------------*/
    public class ButtonStateHandler : MonoBehaviour
    {
        private Animator buttonAnimator;
        
        public void Awake()
        {
            buttonAnimator = GetComponent<Animator>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            buttonAnimator.SetBool("IsHovered", true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            buttonAnimator.SetBool("IsHovered", false);
        }
        
        public void OnMouseDown()
        {
            buttonAnimator.SetBool("IsPressed", true);
        }
    }
}