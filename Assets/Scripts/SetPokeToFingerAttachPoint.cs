using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SetPokeToFingerAttachPoint : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Transform pokeAttachPoint;

    private XRPokeInteractor _xrPokeInteractor;

    void Start()
    {
        _xrPokeInteractor = transform.parent.parent.GetComponentInChildren<XRPokeInteractor>();
        SetPokeAttachPoint();
    }

    void SetPokeAttachPoint()
    {
        if (pokeAttachPoint == null)
        {
            Debug.LogWarning("Poke Attach Point is null");
            return;
        }

        if (_xrPokeInteractor == null)
        {
            Debug.LogWarning("XR Poke Interactor is null");
            return;
        }

        _xrPokeInteractor.attachTransform = pokeAttachPoint;
    }
}