using UnityEngine;
using UnityEngine.InputSystem;

public class WeatherSystem : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private GameObject ground;
    [SerializeField] private Material groundGrass;
    [SerializeField] private Material groundSnow;

    [SerializeField] private ParticleSystem snowParticles;
    [SerializeField] private ParticleSystem rainParticles;

    [SerializeField] private AudioSource rainAudio;
    [SerializeField] private AudioSource thunderAudio;

    [SerializeField] private float maxRainRate = 1000f;
    [SerializeField] private float maxSnownRate = 2000f;

    private ParticleSystem.EmissionModule _snowEmission;
    private ParticleSystem.VelocityOverLifetimeModule _snowVelocityOverLifetime;
    private ParticleSystem.EmissionModule _rainEmission;
    private ParticleSystem.VelocityOverLifetimeModule _rainVelocityOverLifetime;
    public enum WeatherIntensity {Clear, Cloudy, Rainy, Snowy, Stormy}

    private void OnEnable() => WeatherUIManager.OnCurrentDisplayUpdated += OnWeatherChanged;
    private void OnDisable() => WeatherUIManager.OnCurrentDisplayUpdated -= OnWeatherChanged;

    private void Awake()
    {
        _snowEmission = snowParticles.emission;
        _snowVelocityOverLifetime = snowParticles.velocityOverLifetime;
        _rainEmission = rainParticles.emission;
        _rainVelocityOverLifetime = rainParticles.velocityOverLifetime;
    }

    // Use p.WeatherCode, p.Temperature, p.Humidity, p.WindSpeed and p.RainChance for weather management
    // WeatherCode determines the base weather with GetIntensity()
    // Temperature sets snows if under 0°C
    // Humidity manages fog
    // WindSpeed manages particles and outside objetcs moving
    // RainChance adds clouds
    private void OnWeatherChanged(WeatherUIManager.CurrentWeatherPayload p)
    {
        // reset everything
        snowParticles.gameObject.SetActive(false);
        _snowEmission.rateOverTime = 0f;
        _snowVelocityOverLifetime.x = p.WindSpeed;
        FindAnyObjectByType<SnowSystem>().SetSnow(0f);
        ground.GetComponent<Renderer>().material = groundGrass;

        rainParticles.gameObject.SetActive(false);
        _rainEmission.rateOverTime = 0f;
        _rainVelocityOverLifetime.x = p.WindSpeed;

        // audio
        rainAudio.Stop();

        // add weather
        WeatherIntensity intensity = GetIntensity(p.WeatherCode);
        switch (intensity)
        {
            // clear, default weather
            case WeatherIntensity.Clear:
                {
                    break;
                }
            // some clouds in the sky
            case WeatherIntensity.Cloudy:
                {
                    break;
                }
            // raining
            case WeatherIntensity.Rainy:
                {
                    rainParticles.gameObject.SetActive(true);
                    _rainEmission.rateOverTime = maxRainRate * GetRainIntensity(p.WeatherCode);

                    // audio
                    if (!rainAudio.isPlaying) rainAudio.Play();
                    rainAudio.volume = Mathf.Lerp(rainAudio.volume, GetRainIntensity(p.WeatherCode), Time.deltaTime);
                    break;
                }
            // snowing, the floor is snowy too
            case WeatherIntensity.Snowy:
                {
                    snowParticles.gameObject.SetActive(true);
                    _snowEmission.rateOverTime = maxSnownRate * GetSnowIntensity(p.WeatherCode);
                    FindAnyObjectByType<SnowSystem>().SetSnow(1f);
                    ground.GetComponent<Renderer>().material = groundSnow;
                    break;
                }
            // rain and thunder
            case WeatherIntensity.Stormy:
                {
                    rainParticles.gameObject.SetActive(true);
                    _rainEmission.rateOverTime = maxRainRate;

                    if (!rainAudio.isPlaying) rainAudio.Play();
                    rainAudio.volume = Mathf.Lerp(rainAudio.volume, 1f, Time.deltaTime);
                    break;
                }
            default:
                break;
        }

        // add snow on the floor
        if (p.Temperature <= 0)
        {
            FindAnyObjectByType<SnowSystem>().SetSnow(1f);
            ground.GetComponent<Renderer>().material = groundSnow;
        }
        if (p.Humidity >= 50)
        {
            // add fog
        }
        if (p.WindSpeed >= 20)
        {
            // add wind
        }
        if (p.RainChance >= 30)
        {
            // add clouds
        }
    }

    private WeatherIntensity GetIntensity(int code)
    {
        return code switch
        {
            0 => WeatherIntensity.Clear,
            1 or 2 or 3 or 45 or 48 => WeatherIntensity.Cloudy,
            51 or 53 or 55 or 61 or 63 or 65 or 80 or 81 or 82 => WeatherIntensity.Rainy,
            71 or 73 or 75 or 77 or 85 or 86 => WeatherIntensity.Snowy,
            95 or 96 or 99 => WeatherIntensity.Stormy,
            _ => WeatherIntensity.Clear
        };
    }

    private float GetRainIntensity(int code)
    {
        return code switch
        {
            51 or 61 or 80 => 0.2f,
            53 or 63 or 81 => 0.6f,
            55 or 65 or 82 => 1.0f,
            _ => 0f
        };
    }
    private float GetSnowIntensity(int code)
    {
        return code switch
        {
            71 or 77 or 85 => 0.2f,
            73 => 0.6f,
            75 or 86 => 1.0f,
            _ => 0f
        };
    }
}
