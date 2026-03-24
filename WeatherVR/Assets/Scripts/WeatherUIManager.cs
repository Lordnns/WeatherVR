using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
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
    
    // ── Button state ──────────────────────────────────────────────────────────
    private enum ButtonState { Idle, Loading, Error }

    private static readonly Color _colorIdle    = Color.green;
    private static readonly Color _colorLoading = Color.yellow;
    private static readonly Color _colorError   = new Color(1f, 0.2f, 0.2f); // bright red

    [Header("Button Feedback")]
    [SerializeField] private float errorLightDuration = 3f;

    [Header("Button Sounds")]
    [SerializeField] private AudioSource feedbackAudio;  // dedicated AudioSource for UI sounds
    [SerializeField] private AudioClip   successClip;    // played on green (success)
    [SerializeField] private AudioClip   errorClip;      // played on red (failure)

    // Tracks whether the search button is waiting for a result so we don't
    // react to unrelated OnWeatherUpdated calls (e.g. the initial load).
    private bool _citySearchPending = false;

    // ── Search UI ─────────────────────────────────────────────────────────────
    [Header("Search UI")]
    public TMP_InputField citySearchInput;
    public Image searchButtonImage;
    public Button searchButton;
    
    [Header("Slot ID Lookup")]
    public TMP_InputField slotIdInput;  // user types e.g. "MO14"
    public Image slotButtonImage;
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
        WeatherAPI.OnWeatherUpdated  += UpdateCurrentWeatherUI;
        WeatherAPI.OnWeatherUpdated  += OnCitySearchSucceeded;
        WeatherAPI.OnForecastUpdated += UpdateHourlyForecastUI;
        WeatherAPI.OnCitySearchFailed += OnCitySearchFailed;
        
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchClicked);
        }

        if (slotIdButton != null)
        {
            slotIdButton.onClick.AddListener(OnSlotIdSubmitted);
        }

        // Both buttons start green (idle)
        SetButtonLight(searchButtonImage, ButtonState.Idle);
        SetButtonLight(slotButtonImage,   ButtonState.Idle);
    }

    private void OnDisable()
    {
        WeatherAPI.OnWeatherUpdated  -= UpdateCurrentWeatherUI;
        WeatherAPI.OnWeatherUpdated  -= OnCitySearchSucceeded;
        WeatherAPI.OnForecastUpdated -= UpdateHourlyForecastUI;
        WeatherAPI.OnCitySearchFailed -= OnCitySearchFailed;
        
        if (searchButton != null)
            searchButton.onClick.RemoveListener(OnSearchClicked);
        
        if (slotIdButton != null)
            slotIdButton.onClick.RemoveListener(OnSlotIdSubmitted);
    }

    // ── City search ───────────────────────────────────────────────────────────

    private void OnSearchClicked()
    {
        if (string.IsNullOrEmpty(citySearchInput.text)) return;

        // Mark that we're waiting for a city-search result
        _citySearchPending = true;

        // Yellow while the API call is in flight
        SetButtonLight(searchButtonImage, ButtonState.Loading);

        WeatherAPI.instance.UpdateCityManual(citySearchInput.text);
    }

    /// <summary>Called when OnWeatherUpdated fires after a city search.</summary>
    private void OnCitySearchSucceeded()
    {
        if (!_citySearchPending) return;   // not our result, ignore
        _citySearchPending = false;

        SetButtonLight(searchButtonImage, ButtonState.Idle);
        PlaySound(successClip);
    }

    /// <summary>Called when geocoding returns no results or fails.</summary>
    private void OnCitySearchFailed()
    {
        _citySearchPending = false;

        SetButtonLight(searchButtonImage, ButtonState.Error);
        PlaySound(errorClip);

        // Auto-reset to green after errorLightDuration seconds
        StartCoroutine(ResetLightAfterDelay(searchButtonImage, errorLightDuration));
    }

    // ── Slot-ID lookup ────────────────────────────────────────────────────────

    private void OnSlotIdSubmitted()
    {
        if (string.IsNullOrEmpty(slotIdInput.text)) return;

        // Brief yellow flash so the user knows the button was pressed
        SetButtonLight(slotButtonImage, ButtonState.Loading);

        string id = slotIdInput.text.ToUpper().Trim();

        if (_slots.TryGetValue(id, out SlotData data))
        {
            _selectedSlot = data;
            RefreshCurrentDisplay();
            Debug.Log($"Slot [{id}] selected — WMO:{data.WeatherCode} Temp:{data.Temperature}°C");

            // Success: back to green + sound
            SetButtonLight(slotButtonImage, ButtonState.Idle);
            PlaySound(successClip);
        }
        else
        {
            Debug.LogWarning($"Slot ID '{id}' not found. Valid format: MO14, TU09, SU00...");

            // Error: red + sound, then auto-reset
            SetButtonLight(slotButtonImage, ButtonState.Error);
            PlaySound(errorClip);
            StartCoroutine(ResetLightAfterDelay(slotButtonImage, errorLightDuration));
        }
    }

    // ── Light helpers ─────────────────────────────────────────────────────────

    private void SetButtonLight(Image light, ButtonState state)
    {
        if (light == null) return;
        light.color = state switch
        {
            ButtonState.Idle    => _colorIdle,
            ButtonState.Loading => _colorLoading,
            ButtonState.Error   => _colorError,
            _                   => _colorIdle
        };
    }

    private IEnumerator ResetLightAfterDelay(Image light, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetButtonLight(light, ButtonState.Idle);
    }

    // ── Sound helpers ─────────────────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (feedbackAudio == null || clip == null) return;
        feedbackAudio.PlayOneShot(clip);
    }

    // ── Rest of the existing methods (unchanged) ──────────────────────────────

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
 
            // Spawn and populate display
            GameObject   newSlotObj = Instantiate(hourlySlotPrefab, hourlyContentParent);
            HourlyUISlot slotUI     = newSlotObj.GetComponent<HourlyUISlot>();
 
            slotUI.idText.text         = slotId;
            slotUI.timeText.text       = parsedTime.ToString("h tt");
            slotUI.tempText.text       = $"{Mathf.RoundToInt(temp)}°";
            slotUI.rainChanceText.text = $"{rain}%";
            slotUI.iconImage.texture   = GetTextureForWeatherCode(code, isDayHour);
 
            // Store full data silently
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
    private Texture GetTextureForWeatherCode(int code, bool isDay)
    {
        switch (code)
        {
            case 0:  return isDay ? clearDay : clearNight;
            case 1:
            case 2:  return isDay ? partlyCloudyDay : partlyCloudyNight;
            case 3:  return isDay ? overcastDay : overcastNight;
            case 45: return isDay ? fogDay : fogNight;
            case 48: return isDay ? overcastDayFog : overcastNightFog;
            case 51: return isDay ? partlyCloudyDayDrizzle : partlyCloudyNightDrizzle;
            case 53: return isDay ? overcastDayDrizzle : overcastNightDrizzle;
            case 55: return isDay ? extremeDayDrizzle : extremeNightDrizzle;
            case 56: return isDay ? overcastDaySleet : overcastNightSleet;
            case 57: return isDay ? extremeDaySleet : extremeNightSleet;
            case 61: return isDay ? partlyCloudyDayRain : partlyCloudyNightRain;
            case 63: return isDay ? overcastDayRain : overcastNightRain;
            case 65: return isDay ? extremeDayRain : extremeNightRain;
            case 66: return isDay ? overcastDaySleet : overcastNightSleet;
            case 67: return isDay ? extremeDaySleet : extremeNightSleet;
            case 71: return isDay ? partlyCloudyDaySnow : partlyCloudyNightSnow;
            case 73: return isDay ? overcastDaySnow : overcastNightSnow;
            case 75: return isDay ? extremeDaySnow : extremeNightSnow;
            case 77: return isDay ? partlyCloudyDaySnow : partlyCloudyNightSnow;
            case 80: return isDay ? partlyCloudyDayRain : partlyCloudyNightRain;
            case 81: return isDay ? overcastDayRain : overcastNightRain;
            case 82: return isDay ? extremeDayRain : extremeNightRain;
            case 85: return isDay ? partlyCloudyDaySnow : partlyCloudyNightSnow;
            case 86: return isDay ? extremeDaySnow : extremeNightSnow;
            case 95: return isDay ? thunderstormsDayOvercast : thunderstormsNightOvercast;
            case 96: return isDay ? thunderstormsDayOvercastRain : thunderstormsNightOvercastRain;
            case 99: return isDay ? thunderstormsDayExtremeRain : thunderstormsNightExtremeRain;
            default: return isDay ? clearDay : clearNight;
        }
    }
}