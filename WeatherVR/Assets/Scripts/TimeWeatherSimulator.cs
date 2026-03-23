using UnityEngine;

public class TimeWeatherSimulator : MonoBehaviour
{
    [Header("Simulation Settings")]
    [Range(0, 100)] public int weatherCode;
    [Range(-20, 40)] public float temperature;
    [Range(0, 100)] public float humidity;
    [Range(0, 100)] public float windSpeed;
    [Range(0, 100)] public float rainChance;
    public bool isDay = true;

    [ContextMenu("Force update")]
    public void SendFakeWeather()
    {
        var fakeData = new WeatherUIManager.CurrentWeatherPayload
        {
            WeatherCode = this.weatherCode,
            Temperature = this.temperature,
            Humidity = this.humidity,
            WindSpeed = this.windSpeed,
            RainChance = this.rainChance,
            IsDay = this.isDay,
            Time = System.DateTime.Now,
            Sunrise = "6:00 AM",
            Sunset = "8:00 PM"
        };

        WeatherUIManager.TriggerWeatherUpdate(fakeData);

        Debug.Log($"<color=cyan>Simulation envoyée : Code {weatherCode}</color>");
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            SendFakeWeather();
        }
    }
}
