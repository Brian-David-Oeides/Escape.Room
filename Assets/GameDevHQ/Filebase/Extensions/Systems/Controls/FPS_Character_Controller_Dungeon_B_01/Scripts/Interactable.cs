using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using TMPro;

public class Interactable : MonoBehaviour
{
    [Header("Place your game object with director here")]
    public GameObject Playable;
    [Header("Write your UI text here")]
    public string text;
    [Header("Can you interact with this object more than once?")]
    public bool InteractOnce;
    [Header("Is this treasure?")]
    public bool IsThisTreasure;
    [Header("Drop Treasure Game Object below")]
    public GameObject Treasure_Item;
    [Header("Drop spawn location below")]
    public GameObject SpawnLocation;

    private bool _canOpenAgain = true;
    private TextMeshProUGUI _textUI;
    private bool _isClose;
    
    // Start is called before the first frame update
    void Start()
    {
        _textUI = GameObject.Find("UI_Instructions").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && _isClose == true)
        {
            if (InteractOnce == true && _canOpenAgain == true) //if interactOnce is true, you can only interact with this object once
            {
                OpenChest();//if the player is inside the trigger and presses the mouse, the chest will open
                SpawnTreasure();//Treasure is spawned in this location
                _textUI.SetText("");//Sets UI text to nothing
                _canOpenAgain = false;//Unable to open the treasure chest again unless this is true
            }

            if (InteractOnce == false)//You can interact with this object as many times as you want
            {
                OpenChest();//if the player is inside the trigger and presses the mouse, the chest will open
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (_canOpenAgain == true)
            {
                _textUI.SetText(text);//sets the text for the UI

            }
            _isClose = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _textUI.SetText("");//Sets UI text to nothing
            _isClose = false;
        }
    }

    public void OpenChest()
    {
        PlayableDirector _pd = Playable.GetComponent<PlayableDirector>();
        {
            if (_pd != null)
            {
                _pd.Play();//Play's the playable's timeline animation
            }
        }
    }

    public void SpawnTreasure()
    {
        Instantiate(Treasure_Item, SpawnLocation.transform.position, SpawnLocation.transform.rotation, SpawnLocation.transform);
    }
}
