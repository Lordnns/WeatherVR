using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class TimeSystem : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private Transform sunTilt;
    [SerializeField] private Transform sunRotation;

    [SerializeField] private Material skyboxBase;

    [SerializeField] private Cubemap daySky;
    [SerializeField] private Cubemap eveningSky;
    [SerializeField] private Cubemap nightSky;

    [SerializeField] private int minutesBeforeSunset = 60;
    [SerializeField] private int sunTemp = 6570;
    [SerializeField] private int sunSetTemp = 1500;

    private void OnEnable() => WeatherUIManager.OnCurrentDisplayUpdated += OnWeatherChanged;
    private void OnDisable() => WeatherUIManager.OnCurrentDisplayUpdated -= OnWeatherChanged;

    // Use p.IsDay, p.Time, p.Sunrise and p.Sunset for time management
    // It is night time if p.IsDay is false
    // Otherwise it is evening if p.Time is minutesBeforeSunset minutes before sunset (or after sunrise)
    // Otherwise it is day time
    // Sun temperature only changes on evening
    private void OnWeatherChanged(WeatherUIManager.CurrentWeatherPayload p)
    {
        p.IsDay = false;
        if (string.IsNullOrEmpty(p.Sunrise) || string.IsNullOrEmpty(p.Sunset))
        {
            Debug.LogWarning("Sunet/Sunrise are null");
            p.Sunrise = "6:00 AM";
            p.Sunset = "8:00 PM";
        }

        double dayDuration = (DateTime.Parse(p.Sunset) - DateTime.Parse(p.Sunrise)).TotalMinutes;
        double nightDuration = 60 * 24 - dayDuration;
        float angle;
        float tilt = (float)(dayDuration / (60 * 12));

        if (p.IsDay)
        {
            float progress = (float)((p.Time - DateTime.Parse(p.Sunrise)).TotalMinutes / dayDuration);
            angle = progress * 180f;
            sun.enabled = true;
        }
        else
        {
            double minutesSinceSunset = (p.Time > DateTime.Parse(p.Sunset))
                ? (p.Time - DateTime.Parse(p.Sunset)).TotalMinutes
                : (p.Time.AddDays(1) - DateTime.Parse(p.Sunset)).TotalMinutes;
            float progress = (float)(minutesSinceSunset / nightDuration);
            angle = progress * 180f + 180f;
            sun.enabled = false;
        }

        double minutesFromSunset = Math.Min(
            (p.Time - DateTime.Parse(p.Sunrise)).TotalMinutes,
            (DateTime.Parse(p.Sunset) - p.Time).TotalMinutes
        );

        // rotate and tilt sun
        sunTilt.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);
        sunRotation.transform.localRotation = Quaternion.Euler(angle, 90f, 0f);

        // set skybox
        skyboxBase.SetTexture("_Tex", !p.IsDay ? nightSky : minutesFromSunset < minutesBeforeSunset ? eveningSky : daySky);

        moon.enabled = !sun.enabled;

        // manage sun temperature
        sun.colorTemperature = (float) Math.Clamp(sunTemp * minutesFromSunset / minutesBeforeSunset, sunSetTemp, sunTemp);
    }
}
