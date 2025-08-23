using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public struct TimeData
{
    public int hour;
    public int minute;
    public int second;
    public float totalSeconds;
    public float milliseconds;

    public TimeData(float totalSecondsValue)
    {
        totalSeconds = totalSecondsValue;
        hour = UnityEngine.Mathf.FloorToInt(totalSecondsValue / 3600f) % 24;
        minute = UnityEngine.Mathf.FloorToInt((totalSecondsValue % 3600f) / 60f);
        second = UnityEngine.Mathf.FloorToInt(totalSecondsValue % 60f);
        milliseconds = (totalSecondsValue % 1f) * 1000f;
    }

    public string ToString(bool use24Hour = false)
    {
        if (!use24Hour)
        {
            string ampm = hour >= 12 ? "PM" : "AM";
            int displayHour = hour % 12;
            if (displayHour == 0) displayHour = 12;
            return $"{displayHour:D2}:{minute:D2}:{second:D2} {ampm}";
        }

        return $"{hour:D2}:{minute:D2}:{second:D2}";
    }

    public bool Equals(TimeData other, bool compareSeconds = true)
    {
        if (compareSeconds)
            return hour == other.hour && minute == other.minute && second == other.second;
        else
            return hour == other.hour && minute == other.minute;
    }
}
