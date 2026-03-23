using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeatherSystem : MonoBehaviour
{
    private static readonly WaitForSeconds _waitForSeconds0_1 = new(0.1f);

    [Header("Lights")]
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [Header("Objects")]
    [SerializeField] private GameObject ground;

    [Header("Materials")]
    [SerializeField] private Material groundGrass;
    [SerializeField] private Material groundSnow;
    [SerializeField] private Material clouds;
    [SerializeField] private Material sky;

    [Header("ParticleSystems")]
    [SerializeField] private ParticleSystem snowParticles;
    [SerializeField] private ParticleSystem rainParticles;

    [Header("AudioSources")]
    [SerializeField] private AudioSource dayAudio;
    [SerializeField] private AudioSource nightAudio;
    [SerializeField] private AudioSource rainAudio;
    [SerializeField] private AudioSource windAudio;
    [SerializeField] private AudioSource thunderAudio;

    private float _targetDayAudioVolume = 0f;
    private float _targetRainAudioVolume = 0f;
    private float _targetWindAudioVolume = 0f;
    private float _targetThunderAudioVolume = 0f;

    [Header("Audio")]
    [SerializeField] private float _volumeFadeSpeed = 0.5f;
    [SerializeField] AudioClip[] thunderSounds;

    private float _targetFogDensity = 0f;

    [Header("Fog")]
    [SerializeField] private float _fogFadeSpeed = 0.5f;

    [Header("Clouds")]
    [SerializeField] private float couldsSpeed = 1f;
    private float _cloudsSpeed = 0f;
    private float _targetCloudsAlpha = 0f;
    private float _targetSkyAlpha = 0f;
    private float _targetCloudsColor = 0f;

    [Header("Sky")]
    [SerializeField] private float _skyFadeSpeed = 0.5f;

    [Header("Particles")]
    [SerializeField] private float _maxRainRate = 10000f;
    [SerializeField] private float _maxSnownRate = 5000f;

    private ParticleSystem.EmissionModule _snowEmission;
    private ParticleSystem.VelocityOverLifetimeModule _snowVelocityOverLifetime;
    private ParticleSystem.EmissionModule _rainEmission;
    private ParticleSystem.VelocityOverLifetimeModule _rainVelocityOverLifetime;

    private bool _fog;
    private Color _fogColor;
    private float _fogDensity;

    private bool _thunderActive;
    private bool _isFlashing;
    private float _nextThunderTimer;

    [Header("Thunder")]
    [SerializeField] private float _minThunderCooldown = 5f;
    [SerializeField] private float _maxThunderCooldown = 20f;

    public enum WeatherIntensity {Clear, Cloudy, Rainy, Snowy, Stormy}

    private void OnEnable() => WeatherUIManager.OnCurrentDisplayUpdated += OnWeatherChanged;
    private void OnDisable() => WeatherUIManager.OnCurrentDisplayUpdated -= OnWeatherChanged;

    private void Awake()
    {
        _snowEmission = snowParticles.emission;
        _snowVelocityOverLifetime = snowParticles.velocityOverLifetime;
        _rainEmission = rainParticles.emission;
        _rainVelocityOverLifetime = rainParticles.velocityOverLifetime;

        _fog = RenderSettings.fog;
        _fogColor = RenderSettings.fogColor;
        _fogDensity = RenderSettings.fogDensity;

        _isFlashing = false;
    }

    private void Update()
    {
        //day / night
        dayAudio.volume = Mathf.MoveTowards(
            dayAudio.volume,
            _targetDayAudioVolume,
            _volumeFadeSpeed * Time.deltaTime
        );
        nightAudio.volume = Mathf.MoveTowards(
            nightAudio.volume,
            _targetDayAudioVolume,
            _volumeFadeSpeed * Time.deltaTime
        );
        // rain
        rainAudio.volume = Mathf.MoveTowards(
            rainAudio.volume,
            _targetRainAudioVolume,
            _volumeFadeSpeed * Time.deltaTime
        );
        // wind
        windAudio.volume = Mathf.MoveTowards(
            windAudio.volume,
            _targetWindAudioVolume,
            _volumeFadeSpeed * Time.deltaTime
        );
        // thunder
        thunderAudio.volume = Mathf.MoveTowards(
            thunderAudio.volume,
            _targetThunderAudioVolume,
            _volumeFadeSpeed * Time.deltaTime
        );

        // random lightnings
        if (_thunderActive)
        {
            _nextThunderTimer -= Time.deltaTime;
            if (_nextThunderTimer <= 0)
            {
                thunderAudio.pitch = Random.Range(0.85f, 1.15f);
                thunderAudio.PlayOneShot(thunderSounds[Random.Range(0, thunderSounds.Length)]);

                _nextThunderTimer = Random.Range(_minThunderCooldown, _maxThunderCooldown);

                StartCoroutine(ThunderFlash());
            }
        }

        // fog management
        _fogDensity = Mathf.MoveTowards(
            _fogDensity,
            _targetFogDensity,
            _fogFadeSpeed * Time.deltaTime
        );

        RenderSettings.fog = _fog;
        RenderSettings.fogColor = _fogColor;
        RenderSettings.fogDensity = _fogDensity;

        // clouds
        Vector2 offset = clouds.GetTextureOffset("_BaseMap");
        offset.x += _cloudsSpeed * Time.deltaTime;
        clouds.SetTextureOffset("_BaseMap", offset);

        Color cc = clouds.color;
        cc.r = Mathf.MoveTowards(cc.r, _targetCloudsColor, _skyFadeSpeed * Time.deltaTime);
        cc.g = Mathf.MoveTowards(cc.g, _targetCloudsColor, _skyFadeSpeed * Time.deltaTime);
        cc.b = Mathf.MoveTowards(cc.b, _targetCloudsColor, _skyFadeSpeed * Time.deltaTime);
        cc.a = Mathf.MoveTowards(cc.a, _targetCloudsAlpha, _skyFadeSpeed * Time.deltaTime);
        clouds.color = cc;

        if (!_isFlashing)
        {
            Color cs = sky.color;
            cs.r = Mathf.MoveTowards(cs.r, Mathf.Min(_targetCloudsColor - 0.1f, 0.5f), _skyFadeSpeed * Time.deltaTime);
            cs.g = Mathf.MoveTowards(cs.g, Mathf.Min(_targetCloudsColor - 0.1f, 0.5f), _skyFadeSpeed * Time.deltaTime);
            cs.b = Mathf.MoveTowards(cs.b, Mathf.Min(_targetCloudsColor - 0.1f, 0.5f), _skyFadeSpeed * Time.deltaTime);
            cs.a = Mathf.MoveTowards(cs.a, _targetSkyAlpha, _skyFadeSpeed * Time.deltaTime);
            sky.color = cs;
        }        

    }

    // Use p.WeatherCode, p.Temperature, p.Humidity, p.WindSpeed and p.RainChance for weather management
    // WeatherCode determines the base weather with GetIntensity()
    // Temperature sets snows if under 0°C
    // Humidity manages fog
    // WindSpeed manages particles and outside objetcs moving
    // ---
    // RainChance adds clouds -  not used
    private void OnWeatherChanged(WeatherUIManager.CurrentWeatherPayload p)
    {
        // reset everything
        // snow
        snowParticles.gameObject.SetActive(false);
        _snowEmission.rateOverTime = 0f;
        _snowVelocityOverLifetime.x = new ParticleSystem.MinMaxCurve(p.WindSpeed * 0.15f * 0.7f, p.WindSpeed * 0.15f * 1.3f);
        FindAnyObjectByType<SnowSystem>().SetSnow(0f);
        ground.GetComponent<Renderer>().material = groundGrass;

        // rain
        rainParticles.gameObject.SetActive(false);
        _rainEmission.rateOverTime = 0f;
        _rainVelocityOverLifetime.x = p.WindSpeed * 0.15f;

        // audio
        dayAudio.Stop();
        nightAudio.Stop();
        rainAudio.Stop();
        windAudio.Stop();
        thunderAudio.Stop();
        _targetRainAudioVolume = 0f;
        _targetDayAudioVolume = 0f;
        _targetWindAudioVolume = 0f;
        _targetThunderAudioVolume = 0f;

        // fog
        _fog = true;
        _fogColor = new Color(0.5f, 0.5f, 0.5f); // gray
        _targetFogDensity = 0f;

        // clouds
        _cloudsSpeed = p.WindSpeed * couldsSpeed / 1000;
        _targetCloudsAlpha = 0f;
        _targetSkyAlpha = 0f;
        _targetCloudsColor = 1f;

        // thunder
        _thunderActive = false;

        // add weather
        WeatherIntensity intensity = GetIntensity(p.WeatherCode);
        switch (intensity)
        {
            // clear, default weather
            case WeatherIntensity.Clear:
                {
                    if (p.IsDay)
                    {
                        _targetDayAudioVolume = 0.5f;
                        if (!dayAudio.isPlaying) dayAudio.Play();
                    } else
                    {
                        _targetDayAudioVolume = 1f;
                        if (!nightAudio.isPlaying) nightAudio.Play();
                    }
                    
                    break;
                }
            // clouds in the sky
            case WeatherIntensity.Cloudy:
                {
                    _targetCloudsAlpha = GetCloudsIntensity(p.WeatherCode);
                    if (p.WeatherCode == 45) { _targetSkyAlpha = 0.4f; }
                    if (p.WeatherCode == 48) { _targetSkyAlpha = 0.8f; }

                    if (p.IsDay)
                    {
                        _targetDayAudioVolume = 0.5f;
                        if (!dayAudio.isPlaying) dayAudio.Play();
                    }
                    else
                    {
                        _targetDayAudioVolume = 1f;
                        if (!nightAudio.isPlaying) nightAudio.Play();
                    }
                    break;
                }
            // raining
            case WeatherIntensity.Rainy:
                {
                    rainParticles.gameObject.SetActive(true);
                    _rainEmission.rateOverTime = _maxRainRate * GetRainIntensity(p.WeatherCode);

                    // audio
                    if (!rainAudio.isPlaying) rainAudio.Play();
                    _targetRainAudioVolume = GetRainIntensity(p.WeatherCode);

                    _targetCloudsColor = 0.5f;
                    _targetCloudsAlpha = 1.0f;
                    _targetSkyAlpha = 0.8f;
                    break;
                }
            // snowing, the floor is snowy too
            case WeatherIntensity.Snowy:
                {
                    snowParticles.gameObject.SetActive(true);
                    _snowEmission.rateOverTime = _maxSnownRate * GetSnowIntensity(p.WeatherCode);
                    FindAnyObjectByType<SnowSystem>().SetSnow(1f);
                    ground.GetComponent<Renderer>().material = groundSnow;
                    _fogColor = Color.white;
                    break;
                }
            // rain and thunder
            case WeatherIntensity.Stormy:
                {
                    rainParticles.gameObject.SetActive(true);
                    _rainEmission.rateOverTime = _maxRainRate;

                    if (!rainAudio.isPlaying) rainAudio.Play();
                    _targetRainAudioVolume = 1f;
                    _thunderActive = true;
                    _targetThunderAudioVolume = 1f;

                    _targetCloudsColor = 0.4f;
                    _targetCloudsAlpha = 1.0f;
                    _targetSkyAlpha = 0.9f;
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
        if (p.Humidity >= 10)
        {
            _targetFogDensity = p.Humidity * 0.0005f;
        }
        if (p.WindSpeed >= 20)
        {
            if (!windAudio.isPlaying) windAudio.Play();
            _targetWindAudioVolume = p.WindSpeed * 0.01f;
        }
        //if (p.RainChance >= 30)
        //{
        //    // add clouds
        //}
    }

    IEnumerator ThunderFlash()
    {
        _isFlashing = true;

        Color beginColor = sky.color;
        int nFlashes = 3;
        float dColor = (1 - beginColor.r) / nFlashes;
        for (int i = 0; i < nFlashes; i++)
        {
            float fadeDuration = Random.Range(0.3f, 0.6f);
            float randColor = Random.Range((1.0f - (i + 1) * dColor), (1.0f - i * dColor));
            Color flashColor = new(randColor, randColor, randColor);
            sky.SetColor("_BaseColor", flashColor);

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                Color currentSkyColor = Color.Lerp(flashColor, beginColor, elapsedTime / fadeDuration);
                sky.SetColor("_BaseColor", currentSkyColor);

                yield return null;
            }

            if (i < nFlashes - 1) yield return _waitForSeconds0_1;
        }

        sky.SetColor("_BaseColor", beginColor);

        _isFlashing = false;
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
    private float GetCloudsIntensity(int code)
    {
        return code switch
        {
            1 or 2 or 3 => 0.2f,
            45 => 0.5f,
            48 => 0.8f,
            _ => 0f
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
