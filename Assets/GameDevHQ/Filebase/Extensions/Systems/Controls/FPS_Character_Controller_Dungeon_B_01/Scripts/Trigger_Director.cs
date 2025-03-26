using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Trigger_Director : MonoBehaviour
{
    [Header("Place your game object with director here")]
    public GameObject Playable;
    [Header("Can you interact with this object more than once?")]
    public bool InteractOnce;

    private bool _canReset = true;

    // Start is called before the first frame update
    void Start()
    {
   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy" || other.tag == "Player")
        {

            if (_canReset == true)
            {
                PlayAnimation();
                Debug.Log("Walked into trap");
                
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" || other.tag == "Enemy")
        {
            if (InteractOnce == true)
            {
                _canReset = false;
            }
        }
    }


    public void PlayAnimation()
    {
        PlayableDirector _pd = Playable.GetComponent<PlayableDirector>();
        {
            if (_pd != null)
            {
                _pd.Play();//Play's the playable's timeline animation
            }
        }
    }
}
