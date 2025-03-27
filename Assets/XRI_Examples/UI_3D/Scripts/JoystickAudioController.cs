using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float threshold = 0.3f;

    private bool isPlaying = false;
    private float lastXValue = 0f;
    private float lastYValue = 0f;

    // Method for handling X-axis input
    public void HandleJoystickInputX(float value)
    {
        lastXValue = value;
        CheckAndPlaySound();
    }

    // Method for handling Y-axis input
    public void HandleJoystickInputY(float value)
    {
        lastYValue = value;
        CheckAndPlaySound();
    }

    // Combined method to check both axes and play or stop sound if needed
    private void CheckAndPlaySound()
    {
        // Calculate the combined movement magnitude from both axes
        float magnitude = Mathf.Sqrt(lastXValue * lastXValue + lastYValue * lastYValue);

        // If joystick is moved significantly in any direction and sound isn't playing yet
        if (magnitude > threshold && !isPlaying)
        {
            audioSource.Play();
            isPlaying = true;
        }
        // If joystick returns near center position or is released
        else if (magnitude < threshold && isPlaying)
        {
            audioSource.Stop(); // Stop audio immediately when joystick is released
            isPlaying = false;
        }
    }
}
