using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class WeatherAPI : MonoBehaviour
{
    public static WeatherAPI instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private string proxyBaseUrl = "https://lordnns.myftp.org/api";
    [SerializeField] private string secretToken = "123";
    [SerializeField] private string authHeaderName = "x-vr-app-secret";

    [Header("State")]
    // Public Getters (Read-only for other scripts)
    public float Latitude => _latitude;
    public float Longitude => _longitude;
    public string CityName => _cityName;
    public int WeatherCode => _weatherCode;
    public bool IsDay => _isDay;
    public float Temperature => _temperature;
    public float Humidity => _humidity;
    public float WindSpeed => _windSpeed;
    public float ApparentTemp => _apparentTemp;

    [SerializeField, ReadOnly] private float _latitude;
    [SerializeField, ReadOnly] private float _longitude;
    [SerializeField, ReadOnly] private string _cityName;
    [SerializeField, ReadOnly] private int _weatherCode;
    [SerializeField, ReadOnly] private bool _isDay;
    [SerializeField, ReadOnly] private float _temperature;
    [SerializeField, ReadOnly] private float _humidity;
    [SerializeField, ReadOnly] private float _windSpeed;
    [SerializeField, ReadOnly] private float _apparentTemp;
    
    private ForecastData _hourlyForecast;

    // Delegate for global broadcast
    public static event Action OnWeatherUpdated;
    public static event Action OnForecastUpdated;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(GetLocationByIP());
        yield return StartCoroutine(GetWeatherCurrent());
        StartCoroutine(GetWeatherForecast());
    }

    #region Public API

    public void UpdateCityManual(string newCityName)
    {
        if (string.IsNullOrEmpty(newCityName)) return;
        StartCoroutine(GeocodeAndRefresh(newCityName));
    }
    
    public void RefreshWeather()
    {
        StartCoroutine(PerformFullRefresh());
    }
    
    private IEnumerator PerformFullRefresh()
    {
        yield return StartCoroutine(GetWeatherCurrent());
        StartCoroutine(GetWeatherForecast());
    }

    #endregion

    #region Internal Logic

    private IEnumerator GetLocationByIP()
    {
        string url = $"{proxyBaseUrl}/locate-me"; 
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader(authHeaderName, secretToken);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<IPGeolocationData>(www.downloadHandler.text);
                _latitude = data.latitude;
                _longitude = data.longitude;
                _cityName = data.city;
                Debug.Log($"Located in {_cityName} via Proxy.");
            }
            else
            {
                Debug.LogError($"Proxy lookup failed: {www.error}. Falling back to default.");
                _latitude = 48.41f; _longitude = -71.06f; _cityName = "Saguenay";
            }
        }
    }

    private IEnumerator GeocodeAndRefresh(string cityNameInput)
    {
        string url = $"{proxyBaseUrl}/geo?name={UnityWebRequest.EscapeURL(cityNameInput)}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader(authHeaderName, secretToken);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var geoData = JsonUtility.FromJson<GeoResponse>(www.downloadHandler.text);
                if (geoData.results != null && geoData.results.Length > 0)
                {
                    _latitude = geoData.results[0].latitude;
                    _longitude = geoData.results[0].longitude;
                    _cityName = geoData.results[0].name;

                    yield return StartCoroutine(PerformFullRefresh());
                }
            }
        }
    }

    private IEnumerator GetWeatherCurrent()
    {
        string url = $"{proxyBaseUrl}/weather/current?lat={_latitude}&lon={_longitude}&units=metric";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader(authHeaderName, secretToken);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<WeatherResponseCurrent>(www.downloadHandler.text);
                
                // Map the essentials
                _weatherCode = data.current.weather_code;
                _isDay = data.current.is_day == 1;
                _temperature = data.current.temperature_2m;
                _humidity = data.current.relative_humidity_2m;
                _windSpeed = data.current.wind_speed_10m;
                _apparentTemp = data.current.apparent_temperature;

                Debug.Log("Current Weather Loaded. Triggering Map Update.");
                OnWeatherUpdated?.Invoke();
            }
        }
    }

    // 2. SLOW CALL: Background task for the 5-day / Hourly UI
    private IEnumerator GetWeatherForecast()
    {
        string url = $"{proxyBaseUrl}/weather/forecast?lat={_latitude}&lon={_longitude}&units=metric";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader(authHeaderName, secretToken);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<WeatherResponseForecast>(www.downloadHandler.text);
                _hourlyForecast = data.hourly;
                
                Debug.Log("Forecast Loaded.");
                OnForecastUpdated?.Invoke();
            }
        }
    }

    #endregion
}

#region Data Models

[Serializable]
public class WeatherResponseCurrent 
{
    public CurrentData current;
}

[Serializable]
public class WeatherResponseForecast 
{
    public ForecastData hourly;
}

[Serializable]
public class CurrentData 
{
    public int weather_code;
    public int is_day;
    public float temperature_2m;
    public float relative_humidity_2m;
    public float wind_speed_10m;
    public float apparent_temperature;
}

[Serializable]
public class ForecastData 
{
    public string[] time;
    public float[] temperature_2m;
    public int[] weather_code;
    public float[] precipitation_probability;
}

[Serializable]
public class GeoResponse { public GeoResult[] results; }

[Serializable]
public class GeoResult { public string name; public float latitude; public float longitude; }

[Serializable]
public class IPGeolocationData { public float latitude; public float longitude; public string city; }

#endregion