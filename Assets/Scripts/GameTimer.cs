using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    private float _elapsedTime = 0f;
    private bool _isRunning = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (_isRunning)
        {
            _elapsedTime += Time.deltaTime;
        }
    }

    public void StopTimer()
    {
        _isRunning = false;
    }

    public string GetFormattedTime()
    {
        int hours = Mathf.FloorToInt(_elapsedTime / 3600f);
        int minutes = Mathf.FloorToInt((_elapsedTime % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(_elapsedTime % 60f);
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
}
