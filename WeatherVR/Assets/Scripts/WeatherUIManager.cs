using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class WeatherUIManager : MonoBehaviour
{
    [Header("Search UI")]
    public TMP_InputField citySearchInput;
    public Button searchButton;

    [Header("Current Weather (Left Column)")]
    public TextMeshProUGUI currentCityText;
    public TextMeshProUGUI currentTempText;
    public RawImage currentWeatherIcon;

    [Header("Hourly Forecast (Right Column)")]
    public int hoursToShow = 48;
    public GameObject hourlySlotPrefab;
    public Transform hourlyContentParent; 

    [Header("Clear & Clouds Sprites")]
    public Texture clearDay;
    public Texture clearNight;
    public Texture partlyCloudyDay;
    public Texture partlyCloudyNight;
    public Texture cloudy;
    public Texture overcastDay;
    public Texture overcastNight;

    [Header("Fog & Mist Sprites")]
    public Texture fogDay;
    public Texture fogNight;

    [Header("Rain & Drizzle Sprites")]
    public Texture drizzle;
    public Texture rain;
    public Texture partlyCloudyDayRain;
    public Texture partlyCloudyNightRain;

    [Header("Snow & Sleet Sprites")]
    public Texture sleet;
    public Texture snow;
    public Texture partlyCloudyDaySnow;
    public Texture partlyCloudyNightSnow;

    [Header("Thunderstorm Sprites")]
    public Texture thunderstormsDay;
    public Texture thunderstormsNight;
    public Texture thunderstormsRain;

    private void OnEnable()
    {
        WeatherAPI.OnWeatherUpdated += UpdateCurrentWeatherUI;
        WeatherAPI.OnForecastUpdated += UpdateHourlyForecastUI;
        
        if(searchButton != null)
            searchButton.onClick.AddListener(OnSearchClicked);
    }

    private void OnDisable()
    {
        WeatherAPI.OnWeatherUpdated -= UpdateCurrentWeatherUI;
        WeatherAPI.OnForecastUpdated -= UpdateHourlyForecastUI;
        
        if(searchButton != null)
            searchButton.onClick.RemoveListener(OnSearchClicked);
    }

    private void OnSearchClicked()
    {
        if (!string.IsNullOrEmpty(citySearchInput.text))
        {
            WeatherAPI.instance.UpdateCityManual(citySearchInput.text);
        }
    }

    private void UpdateCurrentWeatherUI()
    {
        currentCityText.text = WeatherAPI.instance.CityName;
        currentTempText.text = $"{Mathf.RoundToInt(WeatherAPI.instance.Temperature)}°C";
        currentWeatherIcon.texture = GetTextureForWeatherCode(WeatherAPI.instance.WeatherCode, WeatherAPI.instance.IsDay);
    }

    private void UpdateHourlyForecastUI()
    {
        ForecastData hourly = WeatherAPI.instance.HourlyForecast;
        if (hourly == null || hourly.time == null || hourly.time.Length == 0) return;

        foreach (Transform child in hourlyContentParent)
        {
            Destroy(child.gameObject);
        }

        int startIndex = FindCurrentHourIndex(hourly.time);

        for (int i = 0; i < hoursToShow; i++)
        {
            int dataIndex = startIndex + i;
            if (dataIndex >= hourly.time.Length) break; 

            GameObject newSlotObj = Instantiate(hourlySlotPrefab, hourlyContentParent);
            HourlyUISlot slotUI = newSlotObj.GetComponent<HourlyUISlot>();
            
            DateTime parsedTime = DateTime.Parse(hourly.time[dataIndex]);
            slotUI.timeText.text = parsedTime.ToString("h tt"); 
            slotUI.tempText.text = $"{Mathf.RoundToInt(hourly.temperature_2m[dataIndex])}°";
            slotUI.rainChanceText.text = $"{hourly.precipitation_probability[dataIndex]}%";

            bool isDayHour = parsedTime.Hour > 6 && parsedTime.Hour < 18;
            slotUI.iconImage.texture = GetTextureForWeatherCode(hourly.weather_code[dataIndex], isDayHour);
        }
    }

    private int FindCurrentHourIndex(string[] times)
    {
        DateTime now = DateTime.Now;
        for (int i = 0; i < times.Length; i++)
        {
            DateTime time = DateTime.Parse(times[i]);
            if (time >= new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0))
            {
                return i;
            }
        }
        return 0;
    }

    // Highly precise mapping based on Meteocons
    private Texture GetTextureForWeatherCode(int code, bool isDay)
    {
        switch (code)
        {
            // Clear
            case 0: 
                return isDay ? clearDay : clearNight;
            
            // Mainly clear, partly cloudy
            case 1: 
            case 2: 
                return isDay ? partlyCloudyDay : partlyCloudyNight;
            
            // Overcast
            case 3: 
                return isDay ? overcastDay : overcastNight;
            
            // Fog
            case 45: 
            case 48: 
                return isDay ? fogDay : fogNight;
            
            // Drizzle (Light to dense)
            case 51: 
            case 53: 
            case 55: 
                return drizzle;
            
            // Freezing Drizzle
            case 56: 
            case 57: 
                return sleet;
            
            // Rain (Slight to heavy)
            case 61: 
            case 63: 
            case 65: 
                return rain;
            
            // Freezing Rain
            case 66: 
            case 67: 
                return sleet;
            
            // Snow fall
            case 71: 
            case 73: 
            case 75: 
            case 77: 
                return snow;
            
            // Rain showers
            case 80: 
            case 81: 
            case 82: 
                return isDay ? partlyCloudyDayRain : partlyCloudyNightRain;
            
            // Snow showers
            case 85: 
            case 86: 
                return isDay ? partlyCloudyDaySnow : partlyCloudyNightSnow;
            
            // Thunderstorms
            case 95: 
                return isDay ? thunderstormsDay : thunderstormsNight;
            
            // Thunderstorms with heavy precipitation
            case 96: 
            case 99: 
                return thunderstormsRain;

            default: 
                return isDay ? clearDay : clearNight;
        }
    }
}