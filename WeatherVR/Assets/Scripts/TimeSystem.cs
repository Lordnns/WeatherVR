using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeSystem : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

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
        double minutesFromSunset = Math.Min(
            (p.Time - DateTime.Parse(p.Sunrise)).TotalMinutes,
            (DateTime.Parse(p.Sunset) - p.Time).TotalMinutes
            );

        // rotate sun
        sun.transform.rotation = Quaternion.Euler((p.Time.Hour * 15 - 90) % 360, 0, 0);

        // set skybox
        skyboxBase.SetTexture("_Tex", !p.IsDay ? nightSky : minutesFromSunset < minutesBeforeSunset ? eveningSky : daySky);

        // enable or disable light sources
        sun.enabled = p.IsDay;
        moon.enabled = !p.IsDay;

        // manage sun temperature
        sun.colorTemperature = (float) Math.Clamp(sunTemp * minutesFromSunset / minutesBeforeSunset, sunSetTemp, sunTemp);
    }
}
