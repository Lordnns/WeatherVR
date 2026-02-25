using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    [SerializeField] private float LoadDelay = 3.5f;

    [Header("Systems to Bridge")]
    [SerializeField] private WeatherAPI weatherApi;
    [SerializeField] private WeatherSystem weatherSystem;

    private void OnEnable()
    {
        // Subscribe to the API event you defined in WeatherAPI.cs
        WeatherAPI.OnWeatherUpdated += HandleFirstWeatherLoad;
    }

    private void OnDisable()
    {
        WeatherAPI.OnWeatherUpdated -= HandleFirstWeatherLoad;
    }

    void Start()
    {

    }


    private void HandleFirstWeatherLoad()
    {
        Debug.Log("LoadingManager: Weather data received! Bridging systems and hiding splash.");

        // This is where we bridge the two systems
        // For now, we just log, but later you will call weatherSystem.SetWeather()
        // based on weatherApi.WeatherCode

        StartCoroutine(DelayedHideRoutine());
    }

    private IEnumerator DelayedHideRoutine()
    {
        // Wait for the specified time
        yield return new WaitForSeconds(LoadDelay);

        // This is where you will eventually map WeatherAPI.instance.WeatherCode 
        // to WeatherSystem.SetWeather()

        HideLoading();
    }

    public void HideLoading()
    {
        VRAsyncLoader.SceneSetupComplete = true;
    }
}