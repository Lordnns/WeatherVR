using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class WeatherUIManager : MonoBehaviour
{
    public static WeatherUIManager instance { get; private set; }
    
    public static event Action<CurrentWeatherPayload> OnCurrentDisplayUpdated;
 
    public struct CurrentWeatherPayload
    {
        public int      WeatherCode;
        public bool     IsDay;
        public float    Temperature;
        public float    ApparentTemp;
        public float    Humidity;
        public float    WindSpeed;
        public float    RainChance;
        public string   HighLow;
        public string   Sunrise;
        public string   Sunset;
        public DateTime Time;
    }
    
    [Header("Search UI")]
    public TMP_InputField citySearchInput;
    public Button searchButton;
    
    [Header("Slot ID Lookup")]
    public TMP_InputField slotIdInput;  // user types e.g. "MO14"
    public Button slotIdButton;

    [Header("Current Weather (Left Column)")]
    public TextMeshProUGUI currentCityText;
    public TextMeshProUGUI currentTempText;
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI currentRainChanceText;
    public RawImage currentWeatherIcon;
    public TextMeshProUGUI currentFeelsLikeText;
    public TextMeshProUGUI currentHumidityText;
    public TextMeshProUGUI currentWindSpeedText;
    public TextMeshProUGUI currentHighLowText;
    public TextMeshProUGUI currentSunriseText;
    public TextMeshProUGUI currentSunsetText;

    [Header("Hourly Forecast (Right Column)")]
    public int hoursToShow = 48;
    public GameObject hourlySlotPrefab;
    public Transform hourlyContentParent;
    
    private static readonly string[] DayAbbr = { "SU", "MO", "TU", "WE", "TH", "FR", "SA" };
    
    private struct SlotData
    {
        public string   Id;
        public DateTime Time;
        public int      WeatherCode;
        public bool     IsDay;
        public float    Temperature;
        public float    ApparentTemp;
        public float    Humidity;
        public float    WindSpeed;
        public float    RainChance;
    }
    
    private Dictionary<string, SlotData> _slots = new Dictionary<string, SlotData>();
    private SlotData? _selectedSlot;

    [Header("Clear Sky  (WMO 0)")]
    public Texture clearDay;                        // clear-day
    public Texture clearNight;                      // clear-night
 
    [Header("Mainly Clear / Partly Cloudy  (WMO 1-2)")]
    public Texture partlyCloudyDay;                 // partly-cloudy-day
    public Texture partlyCloudyNight;               // partly-cloudy-night
 
    [Header("Overcast  (WMO 3)")]
    public Texture overcastDay;                     // overcast-day
    public Texture overcastNight;                   // overcast-night
 
    [Header("Fog  (WMO 45)")]
    public Texture fogDay;                          // fog-day
    public Texture fogNight;                        // fog-night
 
    [Header("Rime Fog  (WMO 48 — denser)")]
    public Texture overcastDayFog;                  // overcast-day-fog
    public Texture overcastNightFog;                // overcast-night-fog
 
    [Header("Drizzle Light  (WMO 51)")]
    public Texture partlyCloudyDayDrizzle;          // partly-cloudy-day-drizzle
    public Texture partlyCloudyNightDrizzle;        // partly-cloudy-night-drizzle
 
    [Header("Drizzle Moderate  (WMO 53)")]
    public Texture overcastDayDrizzle;              // overcast-day-drizzle
    public Texture overcastNightDrizzle;            // overcast-night-drizzle
 
    [Header("Drizzle Dense  (WMO 55)")]
    public Texture extremeDayDrizzle;               // extreme-day-drizzle
    public Texture extremeNightDrizzle;             // extreme-night-drizzle
 
    [Header("Freezing Drizzle / Sleet Light  (WMO 56, 66)")]
    public Texture overcastDaySleet;                // overcast-day-sleet
    public Texture overcastNightSleet;              // overcast-night-sleet
 
    [Header("Freezing Drizzle / Sleet Heavy  (WMO 57, 67)")]
    public Texture extremeDaySleet;                 // extreme-day-sleet
    public Texture extremeNightSleet;               // extreme-night-sleet
 
    [Header("Rain Slight  (WMO 61, 80)")]
    public Texture partlyCloudyDayRain;             // partly-cloudy-day-rain
    public Texture partlyCloudyNightRain;           // partly-cloudy-night-rain
 
    [Header("Rain Moderate  (WMO 63, 81)")]
    public Texture overcastDayRain;                 // overcast-day-rain
    public Texture overcastNightRain;               // overcast-night-rain
 
    [Header("Rain Heavy  (WMO 65, 82)")]
    public Texture extremeDayRain;                  // extreme-day-rain
    public Texture extremeNightRain;                // extreme-night-rain
 
    [Header("Snow Slight  (WMO 71, 77, 85)")]
    public Texture partlyCloudyDaySnow;             // partly-cloudy-day-snow
    public Texture partlyCloudyNightSnow;           // partly-cloudy-night-snow
 
    [Header("Snow Moderate  (WMO 73)")]
    public Texture overcastDaySnow;                 // overcast-day-snow
    public Texture overcastNightSnow;               // overcast-night-snow
 
    [Header("Snow Heavy  (WMO 75, 86)")]
    public Texture extremeDaySnow;                  // extreme-day-snow
    public Texture extremeNightSnow;                // extreme-night-snow
 
    [Header("Hail  (WMO 96)")]
    public Texture overcastDayHail;                 // overcast-day-hail
    public Texture overcastNightHail;               // overcast-night-hail
 
    [Header("Thunderstorm  (WMO 95)")]
    public Texture thunderstormsDayOvercast;        // thunderstorms-day-overcast
    public Texture thunderstormsNightOvercast;      // thunderstorms-night-overcast
 
    [Header("Thunderstorm + Rain  (WMO 96)")]
    public Texture thunderstormsDayOvercastRain;    // thunderstorms-day-overcast-rain
    public Texture thunderstormsNightOvercastRain;  // thunderstorms-night-overcast-rain
 
    [Header("Thunderstorm + Heavy Hail  (WMO 99)")]
    public Texture thunderstormsDayExtremeRain;     // thunderstorms-day-extreme-rain
    public Texture thunderstormsNightExtremeRain;   // thunderstorms-night-extreme-rain

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }
    
    private void OnEnable()
    {
        WeatherAPI.OnWeatherUpdated += UpdateCurrentWeatherUI;
        WeatherAPI.OnForecastUpdated += UpdateHourlyForecastUI;
        
        if(searchButton != null)
            searchButton.onClick.AddListener(OnSearchClicked);
        
        if (slotIdButton != null)
            slotIdButton.onClick.AddListener(OnSlotIdSubmitted);
    }

    private void OnDisable()
    {
        WeatherAPI.OnWeatherUpdated -= UpdateCurrentWeatherUI;
        WeatherAPI.OnForecastUpdated -= UpdateHourlyForecastUI;
        
        if(searchButton != null)
            searchButton.onClick.RemoveListener(OnSearchClicked);
        
        if (slotIdButton != null)
            slotIdButton.onClick.RemoveListener(OnSlotIdSubmitted);
    }

    private void OnSearchClicked()
    {
        if (!string.IsNullOrEmpty(citySearchInput.text))
        {
            WeatherAPI.instance.UpdateCityManual(citySearchInput.text);
        }
    }
    
    private void OnSlotIdSubmitted()
    {
        if (string.IsNullOrEmpty(slotIdInput.text)) return;
 
        string id = slotIdInput.text.ToUpper().Trim();
 
        if (_slots.TryGetValue(id, out SlotData data))
        {
            _selectedSlot = data;
            RefreshCurrentDisplay();
            Debug.Log($"Slot [{id}] selected — WMO:{data.WeatherCode} Temp:{data.Temperature}°C");
        }
        else
        {
            Debug.LogWarning($"Slot ID '{id}' not found. Valid format: MO14, TU09, SU00...");
        }
    }
    
    public void ClearSelection()
    {
        _selectedSlot = null;
        RefreshCurrentDisplay();
    }
    

    private void UpdateCurrentWeatherUI()
    {
        if (_selectedSlot != null) return;
        RefreshCurrentDisplay();
    }
    
     private void RefreshCurrentDisplay()
    {
        // ── High/low and sunrise/sunset — keyed to slot day or today ──────────
        // If a slot is selected use its date, otherwise use today
        string lookupKey = _selectedSlot.HasValue
                            ? _selectedSlot.Value.Time.ToString("yyyy-MM-dd")
                            : DateTime.Now.ToString("yyyy-MM-dd");
        string highLow  = "";
        string sunrise  = "";
        string sunset   = "";
        DailyData daily = WeatherAPI.instance.DailyForecast;
        if (daily != null && daily.time != null)
        {
            for (int d2 = 0; d2 < daily.time.Length; d2++)
            {
                if (daily.time[d2] == lookupKey)
                {
                    highLow = $"H: {Mathf.RoundToInt(daily.temperature_2m_max[d2])}°\n L: {Mathf.RoundToInt(daily.temperature_2m_min[d2])}°";
                    sunrise = DateTime.Parse(daily.sunrise[d2]).ToString("h:mm tt");
                    sunset  = DateTime.Parse(daily.sunset[d2]).ToString("h:mm tt");
                    break;
                }
            }
        }
 
        if (_selectedSlot.HasValue)
        {
            SlotData d           = _selectedSlot.Value;
            DateTime now         = DateTime.Now;
            bool     isPastToday = d.Time.Date == now.Date && d.Time < now;
            bool     isToday     = d.Time.Date == now.Date;
            DateTime displayDt   = isPastToday ? now : d.Time;
            string   displayTime = isToday
                                    ? displayDt.ToString("h:mm tt")
                                    : displayDt.ToString("ddd h:mm tt");
 
            currentCityText.text       = WeatherAPI.instance.CityName;
            currentTimeText.text       = displayTime;
            currentTempText.text       = $"{Mathf.RoundToInt(d.Temperature)}°C";
            currentRainChanceText.text = "Precipitation: " + $"{Mathf.RoundToInt(d.RainChance)}%";
            currentWeatherIcon.texture = GetTextureForWeatherCode(d.WeatherCode, d.IsDay);
 
            // Use the slot's own hourly values for the selected hour
            SetOptionalText(currentFeelsLikeText,  $"{Mathf.RoundToInt(d.ApparentTemp)}°C");
            SetOptionalText(currentHumidityText,   "Humidity:      " + $"{Mathf.RoundToInt(d.Humidity)}%");
            SetOptionalText(currentWindSpeedText,  "Wind Speed:    " + $"{Mathf.RoundToInt(d.WindSpeed)} km/h");
            SetOptionalText(currentHighLowText,    highLow);
            SetOptionalText(currentSunriseText,    "Sunrise: " + sunrise);
            SetOptionalText(currentSunsetText,     "Sunset: "  + sunset);
            
            OnCurrentDisplayUpdated?.Invoke(new CurrentWeatherPayload
            {
                WeatherCode  = d.WeatherCode,
                IsDay        = d.IsDay,
                Temperature  = d.Temperature,
                ApparentTemp = d.ApparentTemp,
                Humidity     = d.Humidity,
                WindSpeed    = d.WindSpeed,
                RainChance   = d.RainChance,
                HighLow      = highLow,
                Sunrise      = sunrise,
                Sunset       = sunset,
                Time         = displayDt
            });
        }
        else
        {
            currentCityText.text       = WeatherAPI.instance.CityName;
            currentTimeText.text       = DateTime.Now.ToString("h:mm tt");
            currentTempText.text       = $"{Mathf.RoundToInt(WeatherAPI.instance.Temperature)}°C";
            currentRainChanceText.text = "Precipitation: " + $"{Mathf.RoundToInt(WeatherAPI.instance.CurrentRainChance)}%";
            currentWeatherIcon.texture = GetTextureForWeatherCode(WeatherAPI.instance.WeatherCode, WeatherAPI.instance.IsDay);
 
            SetOptionalText(currentFeelsLikeText,  $"{Mathf.RoundToInt(WeatherAPI.instance.ApparentTemp)}°C");
            SetOptionalText(currentHumidityText,   "Humidity:      " + $"{Mathf.RoundToInt(WeatherAPI.instance.Humidity)}%");
            SetOptionalText(currentWindSpeedText,  "Wind Speed:    " + $"{Mathf.RoundToInt(WeatherAPI.instance.WindSpeed)} km/h");
            SetOptionalText(currentHighLowText,    highLow);
            SetOptionalText(currentSunriseText,    "Sunrise: " + sunrise);
            SetOptionalText(currentSunsetText,     "Sunset: "  + sunset);
            
            OnCurrentDisplayUpdated?.Invoke(new CurrentWeatherPayload
            {
                WeatherCode  = WeatherAPI.instance.WeatherCode,
                IsDay        = WeatherAPI.instance.IsDay,
                Temperature  = WeatherAPI.instance.Temperature,
                ApparentTemp = WeatherAPI.instance.ApparentTemp,
                Humidity     = WeatherAPI.instance.Humidity,
                WindSpeed    = WeatherAPI.instance.WindSpeed,
                RainChance   = WeatherAPI.instance.CurrentRainChance,
                HighLow      = highLow,
                Sunrise      = sunrise,
                Sunset       = sunset,
                Time         = DateTime.Now
            });
        }
    }
     
    private void SetOptionalText(TextMeshProUGUI field, string value)
    {
        if (field != null) field.text = value;
    }

    private void UpdateHourlyForecastUI()
    {
        ForecastData hourly = WeatherAPI.instance.HourlyForecast;
        if (hourly == null || hourly.time == null || hourly.time.Length == 0) return;
 
        foreach (Transform child in hourlyContentParent)
            Destroy(child.gameObject);
 
        _slots.Clear();
        _selectedSlot = null;
 
        // Build sunrise/sunset lookup keyed by "yyyy-MM-dd"
        DailyData daily = WeatherAPI.instance.DailyForecast;
        var sunTimes = new Dictionary<string, (DateTime rise, DateTime set)>();
        if (daily != null && daily.time != null)
        {
            for (int d = 0; d < daily.time.Length; d++)
                sunTimes[daily.time[d]] = (DateTime.Parse(daily.sunrise[d]), DateTime.Parse(daily.sunset[d]));
        }
 
        int startIndex = FindCurrentHourIndex(hourly.time);
 
        for (int i = 0; i < hoursToShow; i++)
        {
            int dataIndex = startIndex + i;
            if (dataIndex >= hourly.time.Length) break;
 
            DateTime parsedTime = DateTime.Parse(hourly.time[dataIndex]);
            string   dateKey    = parsedTime.ToString("yyyy-MM-dd");
            bool     isDayHour  = sunTimes.TryGetValue(dateKey, out var sun)
                                    ? parsedTime >= sun.rise && parsedTime < sun.set
                                    : parsedTime.Hour > 6 && parsedTime.Hour < 20;
            int      code       = hourly.weather_code[dataIndex];
            float    temp       = hourly.temperature_2m[dataIndex];
            float    rain       = hourly.precipitation_probability[dataIndex];
            string   slotId     = DayAbbr[(int)parsedTime.DayOfWeek] + parsedTime.Hour.ToString("D2");
 
            // Spawn and populate display — bare minimum only
            GameObject   newSlotObj = Instantiate(hourlySlotPrefab, hourlyContentParent);
            HourlyUISlot slotUI     = newSlotObj.GetComponent<HourlyUISlot>();
 
            slotUI.idText.text         = slotId;
            slotUI.timeText.text       = parsedTime.ToString("h tt");
            slotUI.tempText.text       = $"{Mathf.RoundToInt(temp)}°";
            slotUI.rainChanceText.text = $"{rain}%";
            slotUI.iconImage.texture   = GetTextureForWeatherCode(code, isDayHour);
 
            // Store full data silently — available when slot is pulled onto big screen
            _slots[slotId] = new SlotData
            {
                Id           = slotId,
                Time         = parsedTime,
                WeatherCode  = code,
                IsDay        = isDayHour,
                Temperature  = temp,
                ApparentTemp = hourly.apparent_temperature != null ? hourly.apparent_temperature[dataIndex] : temp,
                Humidity     = hourly.relative_humidity_2m != null ? hourly.relative_humidity_2m[dataIndex] : 0f,
                WindSpeed    = hourly.wind_speed_10m       != null ? hourly.wind_speed_10m[dataIndex]       : 0f,
                RainChance   = rain
            };
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

    public static void TriggerWeatherUpdate(CurrentWeatherPayload data)
    {
        OnCurrentDisplayUpdated?.Invoke(data);
    }

    // ─── WMO Code → Texture ───────────────────────────────────────────────────
    //
    //  Intensity tier per WMO severity:
    //
    //  WMO  Description                   Tier
    //  ───  ──────────────────────────    ─────────────
    //   0   Clear sky                     clear
    //   1   Mainly clear                  partly-cloudy
    //   2   Partly cloudy                 partly-cloudy
    //   3   Overcast                      overcast
    //  45   Fog                           fog
    //  48   Rime fog (denser)             overcast-fog
    //  51   Drizzle light                 partly-cloudy
    //  53   Drizzle moderate              overcast
    //  55   Drizzle dense                 extreme
    //  56   Freezing drizzle light        overcast-sleet
    //  57   Freezing drizzle heavy        extreme-sleet
    //  61   Rain slight                   partly-cloudy
    //  63   Rain moderate                 overcast
    //  65   Rain heavy                    extreme
    //  66   Freezing rain light           overcast-sleet
    //  67   Freezing rain heavy           extreme-sleet
    //  71   Snow slight                   partly-cloudy
    //  73   Snow moderate                 overcast
    //  75   Snow heavy                    extreme
    //  77   Snow grains                   partly-cloudy (light, granular)
    //  80   Rain showers slight           partly-cloudy
    //  81   Rain showers moderate         overcast
    //  82   Rain showers violent          extreme
    //  85   Snow showers slight           partly-cloudy
    //  86   Snow showers heavy            extreme
    //  95   Thunderstorm                  thunderstorms-overcast
    //  96   Thunderstorm + slight hail    thunderstorms-overcast-rain
    //  99   Thunderstorm + heavy hail     thunderstorms-extreme-rain
    //
    // ─────────────────────────────────────────────────────────────────────────

    // Highly precise mapping based on Meteocons
    private Texture GetTextureForWeatherCode(int code, bool isDay)
    {
switch (code)
        {
            // ── Clear ──────────────────────────────────────────────────────────
            case 0:
                return isDay ? clearDay : clearNight;
 
            // ── Mainly clear / Partly cloudy ───────────────────────────────────
            case 1:
            case 2:
                return isDay ? partlyCloudyDay : partlyCloudyNight;
 
            // ── Overcast ───────────────────────────────────────────────────────
            case 3:
                return isDay ? overcastDay : overcastNight;
 
            // ── Fog ────────────────────────────────────────────────────────────
            case 45:
                return isDay ? fogDay : fogNight;
 
            // ── Rime fog (denser than regular fog) ─────────────────────────────
            case 48:
                return isDay ? overcastDayFog : overcastNightFog;
 
            // ── Drizzle light → partly-cloudy ──────────────────────────────────
            case 51:
                return isDay ? partlyCloudyDayDrizzle : partlyCloudyNightDrizzle;
 
            // ── Drizzle moderate → overcast ────────────────────────────────────
            case 53:
                return isDay ? overcastDayDrizzle : overcastNightDrizzle;
 
            // ── Drizzle dense → extreme ────────────────────────────────────────
            case 55:
                return isDay ? extremeDayDrizzle : extremeNightDrizzle;
 
            // ── Freezing drizzle light → overcast sleet ────────────────────────
            case 56:
                return isDay ? overcastDaySleet : overcastNightSleet;
 
            // ── Freezing drizzle heavy → extreme sleet ─────────────────────────
            case 57:
                return isDay ? extremeDaySleet : extremeNightSleet;
 
            // ── Rain slight → partly-cloudy ────────────────────────────────────
            case 61:
                return isDay ? partlyCloudyDayRain : partlyCloudyNightRain;
 
            // ── Rain moderate → overcast ───────────────────────────────────────
            case 63:
                return isDay ? overcastDayRain : overcastNightRain;
 
            // ── Rain heavy → extreme ───────────────────────────────────────────
            case 65:
                return isDay ? extremeDayRain : extremeNightRain;
 
            // ── Freezing rain light → overcast sleet ───────────────────────────
            case 66:
                return isDay ? overcastDaySleet : overcastNightSleet;
 
            // ── Freezing rain heavy → extreme sleet ────────────────────────────
            case 67:
                return isDay ? extremeDaySleet : extremeNightSleet;
 
            // ── Snow slight → partly-cloudy ────────────────────────────────────
            case 71:
                return isDay ? partlyCloudyDaySnow : partlyCloudyNightSnow;
 
            // ── Snow moderate → overcast ───────────────────────────────────────
            case 73:
                return isDay ? overcastDaySnow : overcastNightSnow;
 
            // ── Snow heavy → extreme ───────────────────────────────────────────
            case 75:
                return isDay ? extremeDaySnow : extremeNightSnow;
 
            // ── Snow grains (light granular) → partly-cloudy ───────────────────
            case 77:
                return isDay ? partlyCloudyDaySnow : partlyCloudyNightSnow;
 
            // ── Rain showers slight → partly-cloudy ────────────────────────────
            case 80:
                return isDay ? partlyCloudyDayRain : partlyCloudyNightRain;
 
            // ── Rain showers moderate → overcast ───────────────────────────────
            case 81:
                return isDay ? overcastDayRain : overcastNightRain;
 
            // ── Rain showers violent → extreme ─────────────────────────────────
            case 82:
                return isDay ? extremeDayRain : extremeNightRain;
 
            // ── Snow showers slight → partly-cloudy ────────────────────────────
            case 85:
                return isDay ? partlyCloudyDaySnow : partlyCloudyNightSnow;
 
            // ── Snow showers heavy → extreme ───────────────────────────────────
            case 86:
                return isDay ? extremeDaySnow : extremeNightSnow;
 
            // ── Thunderstorm → thunderstorms-overcast ──────────────────────────
            case 95:
                return isDay ? thunderstormsDayOvercast : thunderstormsNightOvercast;
 
            // ── Thunderstorm + slight hail → thunderstorms-overcast-rain ────────
            case 96:
                return isDay ? thunderstormsDayOvercastRain : thunderstormsNightOvercastRain;
 
            // ── Thunderstorm + heavy hail → thunderstorms-extreme-rain ──────────
            case 99:
                return isDay ? thunderstormsDayExtremeRain : thunderstormsNightExtremeRain;
 
            default:
                return isDay ? clearDay : clearNight;
        }
    }
}