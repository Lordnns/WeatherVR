using UnityEngine;

public class WeatherSystem : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private float baseSunIntensity = 1;
    [SerializeField] private float baseMoonIntensity = 0.2f;
    [SerializeField] private float rainySunIntensity = 0.5f;
    [SerializeField] private float rainyMoonIntensity = 0.1f;
    [SerializeField] private float snowySunIntensity = 0.4f;
    [SerializeField] private float snowyMoonIntensity = 0.1f;

    [SerializeField] private GameObject ground;
    [SerializeField] private Material groundGrass;
    [SerializeField] private Material groundSnow;

    [SerializeField] private GameObject snow;
    [SerializeField] private GameObject rain;

    private Renderer groundRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (ground != null)
        {
            groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null && groundGrass != null)
            {
                groundRenderer.material = groundGrass;
            }
        }

        if (sun != null)
        {
            sun.intensity = baseSunIntensity;
        }

        if (moon != null)
        {
            moon.intensity = baseMoonIntensity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            EnableSnow(true);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            EnableSnow(false);
            EnableRain(false);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            EnableRain(true);
        }
    }

    void EnableSnow(bool enable)
    {
        if (enable)
        {
            EnableRain(false);
            FindAnyObjectByType<SnowSystem>().SetSnow(1f);
            if (groundRenderer != null && groundSnow != null)
            {
                groundRenderer.material = groundSnow;
            }
            if (snow != null)
            {
                snow.SetActive(true);
            }

            if (sun != null)
            {
                sun.intensity = snowySunIntensity;
            }

            if (moon != null)
            {
                moon.intensity = snowyMoonIntensity;
            }
        }
        else
        {
            FindAnyObjectByType<SnowSystem>().SetSnow(0f);
            if (groundRenderer != null && groundGrass != null)
            {
                groundRenderer.material = groundGrass;
            }
            if (snow != null)
            {
                snow.SetActive(false);
            }

            if (sun != null)
            {
                sun.intensity = baseSunIntensity;
            }

            if (moon != null)
            {
                moon.intensity = baseMoonIntensity;
            }
        }
    }
    void EnableRain(bool enable)
    {
        if (enable)
        {
            EnableSnow(false);
            if (rain != null)
            {
                rain.SetActive(true);
            }

            if (sun != null)
            {
                sun.intensity = rainySunIntensity;
            }

            if (moon != null)
            {
                moon.intensity = rainyMoonIntensity;
            }
        }
        else
        {
            if (rain != null)
            {
                rain.SetActive(false);
            }

            if (sun != null)
            {
                sun.intensity = baseSunIntensity;
            }

            if (moon != null)
            {
                moon.intensity = baseMoonIntensity;
            }
        }
    }
}
