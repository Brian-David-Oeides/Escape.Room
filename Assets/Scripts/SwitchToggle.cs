using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToggle : MonoBehaviour
{
    public Animator switchAnimator; // reference to Animator component

    private bool isOn = false; // track switch state

    public void ToggleSwitch() // method to toggle switch
    {
        isOn = !isOn; // toggle state
        switchAnimator.SetBool("SwitchOn", isOn); // set animator parameter
        Debug.Log("ToggleSwitch called. SwitchOn = " + isOn);
    }
}
