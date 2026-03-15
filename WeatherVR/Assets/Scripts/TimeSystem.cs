using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeSystem : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private Material skyboxBase;

    [SerializeField] private Cubemap daySky;
    [SerializeField] private Cubemap eveningSky;
    [SerializeField] private Cubemap nightSky;

    [SerializeField] private int aboveAngle = 10;
    [SerializeField] private int belowAngle = 10;
    
    [SerializeField] private InputActionReference vrToggleButton;

    private readonly int sunTemp = 6570;
    private readonly int sunSetTemp = 1500;

    // Day times, excluded
    public int endNight = 5;    // 0 -> 4
    public int startDay = 8;    // 5 -> 7
    public int endDay = 17;     // 8 -> 16
    public int startNight = 20; // 17 -> 19
    // night                    // 20 -> 23

    private int currentHour = 12;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // DEBUG
        Debug.Log("Press SPACE to change the weather");
        Debug.Log("Press ENTER to go go forward in time");

        skyboxBase.SetTexture("_Tex", daySky);
        sun.enabled = true;
        moon.enabled = false;
        sun.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        // Moon light points downward
        moon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        currentHour -= 1;
        Forward();
        SetSky();
    }

    // Update is called once per frame
    void Update()
    {
        if (vrToggleButton != null && vrToggleButton.action.WasPressedThisFrame())
        {
            Forward();
            SetSky();
            sun.colorTemperature = GetTemp();
        }

        //int tempAngle = (int) (sunRotation + belowAngle) % 360;
        //sun.colorTemperature = Mathf.Min(GetTemp(
        //    tempAngle,
        //    aboveAngle,
        //    belowAngle,
        //    sunTemp,
        //    sunSetTemp
        //    ), sunTemp);
    }

    void Forward()
    {
        //sun.transform.Rotate(rotationSpeed * Time.deltaTime * Vector3.right);
        currentHour += 1;
        currentHour %= 24;
        float angle = HourToAngle(currentHour);
        SetSunAngle(angle);
    }

    float HourToAngle(int hour)
    {
        return (hour * 15 - 90) % 360;
    }

    void SetSunAngle(float angle)
    {
        sun.transform.rotation = Quaternion.Euler(angle, 0, 0);
    }

    void SetSky()
    {
        if (currentHour < endNight)
        {
            skyboxBase.SetTexture("_Tex", nightSky);
            sun.enabled = false;
            moon.enabled = true;
        }
        else if (currentHour < startDay)
        {
            skyboxBase.SetTexture("_Tex", eveningSky);
            sun.enabled = true;
            moon.enabled = false;
        }
        else if (currentHour < endDay)
        {
            skyboxBase.SetTexture("_Tex", daySky);
            sun.enabled = true;
            moon.enabled = false;
        }
        else if (currentHour < startNight)
        {
            skyboxBase.SetTexture("_Tex", eveningSky);
            sun.enabled = true;
            moon.enabled = false;
        }
        else
        {
            skyboxBase.SetTexture("_Tex", nightSky);
            sun.enabled = false;
            moon.enabled = true;
        }
    }

    /* Parameters : 
     * x: int, sun angle formatted
     * a: int, above angle
     * b: int, below angle
     * A: int, maximum temp value (white)
     * B: int, minimum temp value (red)
     * A > B
     * 
     * Return : 
     * temperature: int
     */
    float GetTemp()
    {
        float angle = HourToAngle(currentHour);
        if (angle > 90) angle = 180 - angle;
        return (sunTemp - sunSetTemp) / (aboveAngle + belowAngle) * angle + sunSetTemp;
    }
}
