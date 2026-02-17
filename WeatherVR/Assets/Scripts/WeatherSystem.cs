using UnityEngine;
using UnityEngine.InputSystem;

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

    [SerializeField] private InputActionReference vrToggleButton;

    private Renderer groundRenderer;
    private int weatherIndex = 0;

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

    void Update()
    {
        // Keyboard Shortcuts (Direct Access)
        if (Input.GetKeyDown(KeyCode.S)) SetWeather(1);
        if (Input.GetKeyDown(KeyCode.R)) SetWeather(2);
        if (Input.GetKeyDown(KeyCode.N)) SetWeather(0);

        // VR Toggle Logic (Cycling)
        // Replace 'yourVRButtonAction' with your XRI Action reference

        if (vrToggleButton != null && vrToggleButton.action.WasPressedThisFrame())
        {
            CycleWeather();
        }
    }

    void CycleWeather()
    {
        // Moves 0 -> 1 -> 2 -> 0
        weatherIndex = (weatherIndex + 1) % 3;
        SetWeather(weatherIndex);
    }

    void SetWeather(int index)
    {
        weatherIndex = index;

        // Reset all first
        EnableSnow(false);
        EnableRain(false);

        // Enable based on index
        if (index == 1) EnableSnow(true);
        else if (index == 2) EnableRain(true);
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
