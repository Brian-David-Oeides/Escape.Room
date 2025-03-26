using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

public class FrameLock : MonoBehaviour
{
    [SerializeField] int[] _lockCode = new int[3] { 1, 2, 3 };
    int[] _enteredCode = { 0, 0, 0 };

    private XRSocketInteractor[] _interactors;
    [SerializeField] UnityEvent _onCheck;

    private void Start()
    {
        _interactors = GetComponentsInChildren<XRSocketInteractor>();
    }

    public void EnterSocket0()
    {
        var interactables = _interactors[0].interactablesSelected;
        FrameID id = interactables[0]?.transform.GetComponent<FrameID>();
        if (id != null)
        {
            _enteredCode[0] = id.GetID();
            if (CheckCode())
                _onCheck.Invoke();
        }
    }

    public void EnterSocket1()
    {
        var interactables = _interactors[1].interactablesSelected;
        FrameID id = interactables[0]?.transform.GetComponent<FrameID>();
        if (id != null)
        {
            _enteredCode[1] = id.GetID();
            if (CheckCode())
                _onCheck.Invoke();
        }
    }

    public void EnterSocket2()
    {
        var interactables = _interactors[2].interactablesSelected;
        FrameID id = interactables[0]?.transform.GetComponent<FrameID>();
        if (id != null)
        {
            _enteredCode[2] = id.GetID();
            if (CheckCode())
                _onCheck.Invoke();
        }
    }


    private bool CheckCode()
    {
        if (_enteredCode.Length == _lockCode.Length)
        {
            for(int i = 0;  i < _enteredCode.Length; i++)
            {
                if (_enteredCode[i] != _lockCode[i])
                    return false;
            }
            return true;
        }
        else return false;
    }
}
