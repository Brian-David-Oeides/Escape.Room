using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeDial : MonoBehaviour
{
    [Header("Combination Settings")]
    [Range(0, 99)] public int[] correctCombination = new int[3];
    public float numberTolerance = 2f;

    [Header("References")]
    public Animator doorAnimator;
    public AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip dialTickSound;
    public AudioClip correctNumberSound;
    public AudioClip unlockSound;

    [Header("Audio Settings")]
    public float minTickVolume = 0.2f;
    public float maxTickVolume = 1f;
    public float maxRotationSpeed = 720f;

    [Header("Haptics")]
    public float tickHapticAmplitude = 0.2f;
    public float tickHapticDuration = 0.02f;
    public float correctHapticAmplitude = 0.6f;
    public float correctHapticDuration = 0.1f;
    public float unlockHapticAmplitude = 1f;
    public float unlockHapticDuration = 0.3f;

    private float previousAngle;
    private int lastPassedNumber = -1;
    private int currentCombinationIndex = 0;
    private bool isUnlocked = false;

    private float correctNumberCooldown = 1.0f;
    private float correctNumberTimer = 0f;

    private XRGrabInteractable grabInteractable;
    private XRBaseController controller;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        controller = args.interactorObject.transform.GetComponent<XRBaseController>();

        // Lock the object's position while allowing rotation
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        controller = null;

        // Reset the interactable (optional)
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = true;
    }

    private void Update()
    {
        if (isUnlocked) return;

        float currentAngle = transform.localEulerAngles.z;
        float deltaAngle = Mathf.DeltaAngle(previousAngle, currentAngle);

        float rotationSpeed = Mathf.Abs(deltaAngle) / Time.deltaTime;
        float dialValue = Mathf.Repeat(currentAngle, 360f);
        int currentNumber = Mathf.RoundToInt(dialValue / 3.6f);

        if (currentNumber != lastPassedNumber)
        {
            PlayTickSound(rotationSpeed);
            SendHapticImpulse(tickHapticAmplitude, tickHapticDuration);

            lastPassedNumber = currentNumber;
        }

        correctNumberTimer -= Time.deltaTime;

        if (correctNumberTimer <= 0f)
        {
            if (Mathf.Abs(currentNumber - correctCombination[currentCombinationIndex]) <= numberTolerance)
            {
                PlaySound(correctNumberSound);
                SendHapticImpulse(correctHapticAmplitude, correctHapticDuration);

                correctNumberTimer = correctNumberCooldown;
                currentCombinationIndex++;

                if (currentCombinationIndex >= correctCombination.Length)
                {
                    UnlockSafe();
                }
            }
        }
        Debug.Log("Current Number: " + currentNumber);

        previousAngle = currentAngle;
    }

    private void UnlockSafe()
    {
        isUnlocked = true;
        PlaySound(unlockSound);
        SendHapticImpulse(unlockHapticAmplitude, unlockHapticDuration);

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("OpenDoor");
        }
    }

    private void PlayTickSound(float rotationSpeed)
    {
        if (audioSource != null && dialTickSound != null)
        {
            float volume = Mathf.Lerp(minTickVolume, maxTickVolume, Mathf.Clamp01(rotationSpeed / maxRotationSpeed));
            audioSource.PlayOneShot(dialTickSound, volume);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SendHapticImpulse(float amplitude, float duration)
    {
        if (controller != null)
        {
            controller.SendHapticImpulse(amplitude, duration);
        }
    }
}
