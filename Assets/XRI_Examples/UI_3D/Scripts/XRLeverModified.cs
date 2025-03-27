using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class XRLeverModified : XRBaseInteractable
{
    const float k_LeverDeadZone = 0.1f;

    [SerializeField]
    [Tooltip("The object that is visually grabbed and manipulated")]
    Transform m_Handle = null;

    // use enum for the three positions
    public enum LeverPosition
    {
        Backward = -1,
        Neutral = 0,
        Forward = 1
    }

    [SerializeField]
    [Tooltip("The current position of the lever")]
    LeverPosition m_Position = LeverPosition.Neutral;

    [SerializeField]
    [Tooltip("If enabled, the lever will snap to the neutral position when released")]
    bool m_LockToNeutral = true;

    [SerializeField]
    [Tooltip("Angle of the lever in the 'forward' position")]
    [Range(0.0f, 90.0f)]
    float m_ForwardAngle = 50.0f;

    [SerializeField]
    [Tooltip("Angle of the lever in the 'backward' position")]
    [Range(-90.0f, 0.0f)]
    float m_BackwardAngle = -50.0f;

    [SerializeField]
    [Tooltip("Angle of the lever in the 'neutral' position")]
    float m_NeutralAngle = 0.0f;

    [SerializeField]
    [Tooltip("Events to trigger when the lever moves forward")]
    UnityEvent m_OnLeverForward = new UnityEvent();

    [SerializeField]
    [Tooltip("Events to trigger when the lever moves backward")]
    UnityEvent m_OnLeverBackward = new UnityEvent();

    [SerializeField]
    [Tooltip("Events to trigger when the lever returns to neutral")]
    UnityEvent m_OnLeverNeutral = new UnityEvent();

    IXRSelectInteractor m_Interactor;

    // Tracking the last non-neutral position for snap-back behavior
    private LeverPosition m_LastActivePosition = LeverPosition.Neutral;

    /// <summary>  
    /// The object that is visually grabbed and manipulated  
    /// </summary>  
    public Transform handle
    {
        get => m_Handle;
        set => m_Handle = value;
    }

    /// <summary>  
    /// The current position of the lever  
    /// </summary>  
    public LeverPosition position
    {
        get => m_Position;
        set => SetPosition(value, true);
    }

    /// <summary>  
    /// If enabled, the lever will snap to the neutral position when released  
    /// </summary>  
    public bool lockToNeutral
    {
        get => m_LockToNeutral;
        set => m_LockToNeutral = value;
    }

    /// <summary>  
    /// Angle of the lever in the 'forward' position  
    /// </summary>  
    public float forwardAngle
    {
        get => m_ForwardAngle;
        set => m_ForwardAngle = value;
    }

    /// <summary>  
    /// Angle of the lever in the 'backward' position  
    /// </summary>  
    public float backwardAngle
    {
        get => m_BackwardAngle;
        set => m_BackwardAngle = value;
    }

    /// <summary>
    /// Angle of the lever in the 'neutral' position
    /// </summary>
    public float neutralAngle
    {
        get => m_NeutralAngle;
        set => m_NeutralAngle = value;
    }

    /// <summary>  
    /// Events to trigger when the lever moves forward  
    /// </summary>  
    public UnityEvent onLeverForward => m_OnLeverForward;

    /// <summary>  
    /// Events to trigger when the lever moves backward  
    /// </summary>  
    public UnityEvent onLeverBackward => m_OnLeverBackward;

    /// <summary>
    /// Events to trigger when the lever returns to neutral
    /// </summary>
    public UnityEvent onLeverNeutral => m_OnLeverNeutral;

    void Start()
    {
        // Initialize to neutral position
        SetPosition(m_Position, true);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        selectEntered.AddListener(StartGrab);
        selectExited.AddListener(EndGrab);
    }

    protected override void OnDisable()
    {
        selectEntered.RemoveListener(StartGrab);
        selectExited.RemoveListener(EndGrab);
        base.OnDisable();
    }

    void StartGrab(SelectEnterEventArgs args)
    {
        m_Interactor = args.interactorObject;
    }

    void EndGrab(SelectExitEventArgs args)
    {
        // when released, if lockToNeutral is true, return to neutral position
        if (m_LockToNeutral && m_Position != LeverPosition.Neutral)
        {
            m_LastActivePosition = m_Position; // remember the last active position
            SetPosition(LeverPosition.Neutral, true);
        }

        m_Interactor = null;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected)
            {
                UpdatePosition();
            }
        }
    }

    Vector3 GetLookDirection()
    {
        Vector3 direction = m_Interactor.GetAttachTransform(this).position - m_Handle.position;
        direction = transform.InverseTransformDirection(direction);
        direction.x = 0;

        return direction.normalized;
    }

    void UpdatePosition()
    {
        var lookDirection = GetLookDirection();
        var lookAngle = Mathf.Atan2(lookDirection.z, lookDirection.y) * Mathf.Rad2Deg;

        // Clamp angle between backward and forward positions
        lookAngle = Mathf.Clamp(lookAngle, m_BackwardAngle, m_ForwardAngle);

        SetHandleAngle(lookAngle);

        // Determine position based on angle
        LeverPosition newPosition;

        // Create thresholds with deadzone
        float forwardThreshold = m_NeutralAngle + (m_ForwardAngle - m_NeutralAngle) * k_LeverDeadZone;
        float backwardThreshold = m_NeutralAngle + (m_BackwardAngle - m_NeutralAngle) * k_LeverDeadZone;

        if (lookAngle > forwardThreshold)
            newPosition = LeverPosition.Forward;
        else if (lookAngle < backwardThreshold)
            newPosition = LeverPosition.Backward;
        else
            newPosition = LeverPosition.Neutral;

        SetPosition(newPosition);
    }

    void SetPosition(LeverPosition newPosition, bool forceRotation = false)
    {
        if (m_Position == newPosition)
        {
            if (forceRotation)
            {
                switch (newPosition)
                {
                    case LeverPosition.Forward:
                        SetHandleAngle(m_ForwardAngle);
                        break;
                    case LeverPosition.Backward:
                        SetHandleAngle(m_BackwardAngle);
                        break;
                    case LeverPosition.Neutral:
                    default:
                        SetHandleAngle(m_NeutralAngle);
                        break;
                }
            }

            return;
        }

        // Store the previous position before changing
        var previousPosition = m_Position;
        m_Position = newPosition;

        // Trigger appropriate events based on the new position
        switch (newPosition)
        {
            case LeverPosition.Forward:
                m_OnLeverForward.Invoke();
                break;
            case LeverPosition.Backward:
                m_OnLeverBackward.Invoke();
                break;
            case LeverPosition.Neutral:
                m_OnLeverNeutral.Invoke();
                break;
        }

        // If not selected or force rotation is true, snap to the correct angle
        if (!isSelected || forceRotation)
        {
            switch (newPosition)
            {
                case LeverPosition.Forward:
                    SetHandleAngle(m_ForwardAngle);
                    break;
                case LeverPosition.Backward:
                    SetHandleAngle(m_BackwardAngle);
                    break;
                case LeverPosition.Neutral:
                default:
                    SetHandleAngle(m_NeutralAngle);
                    break;
            }
        }
    }

    void SetHandleAngle(float angle)
    {
        if (m_Handle != null)
            m_Handle.localRotation = Quaternion.Euler(angle, 0.0f, 0.0f);
    }

    void OnDrawGizmosSelected()
    {
        var angleStartPoint = transform.position;

        if (m_Handle != null)
            angleStartPoint = m_Handle.position;

        const float k_AngleLength = 0.25f;

        var forwardPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(m_ForwardAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
        var backwardPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(m_BackwardAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
        var neutralPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(m_NeutralAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(angleStartPoint, forwardPoint);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(angleStartPoint, backwardPoint);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(angleStartPoint, neutralPoint);
    }

    void OnValidate()
    {
        // Set the handle angle based on the current position
        switch (m_Position)
        {
            case LeverPosition.Forward:
                SetHandleAngle(m_ForwardAngle);
                break;
            case LeverPosition.Backward:
                SetHandleAngle(m_BackwardAngle);
                break;
            case LeverPosition.Neutral:
            default:
                SetHandleAngle(m_NeutralAngle);
                break;
        }
    }
}
