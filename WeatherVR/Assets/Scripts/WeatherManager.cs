using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    [SerializeField] private float LoadDelay = 3.5f;

    [Header("Systems to Bridge")]
    [SerializeField] private WeatherSystem weatherSystem;
    
    [Header("Teleportation")]
    [SerializeField] private Transform xrRigTransform;
    [SerializeField] private Transform startLocation;
    [SerializeField] private GameObject loadingBoxPrefab;
    
    private bool _isTeleported = false;

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
        
        if (_isTeleported) return;

        StartCoroutine(DelayedHideRoutine());
    }

    private IEnumerator DelayedHideRoutine()
    {
        // Wait for the specified time
        yield return new WaitForSeconds(LoadDelay);

        // This is where you will eventually map WeatherAPI.instance.WeatherCode 
        // to WeatherSystem.SetWeather()

        Teleport();

        CleanupLoadingAssets();
    }

    public void Teleport()
    {
        if (xrRigTransform != null && startLocation != null)
        {
            // Sync the position and rotation of the XR Rig to the start location
            xrRigTransform.position = startLocation.position;
            xrRigTransform.rotation = startLocation.rotation;
            _isTeleported = true;
            Debug.Log("Player Teleported to Start Location.");
        }
    }
    
    private void CleanupLoadingAssets()
    {
        // Reveal the world
        VRSeamlessLoader.IsMainSceneReady = true;

        // Destroy the loading box, removing all meshes and scripts from memory
        if (loadingBoxPrefab != null)
        {
            Destroy(loadingBoxPrefab);
            Debug.Log("Loading Box Destroyed.");
        }
    }
}