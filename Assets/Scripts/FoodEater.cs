using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FoodEater : MonoBehaviour
{

    [SerializeField] private XRSocketInteractor socketInteractor;

    public void EatFood()
    {
        var currentFood = socketInteractor.interactablesHovered[0];
        Destroy(currentFood.transform.gameObject);
    }
}
