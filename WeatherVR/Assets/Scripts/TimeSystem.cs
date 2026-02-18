using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeSystem : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    [SerializeField] private Material daySky;
    [SerializeField] private Material eveningSky;
    [SerializeField] private Material nightSky;

    [SerializeField] private int aboveAngle = 10;
    [SerializeField] private int belowAngle = 10;
    [SerializeField] private float rotationSpeed = 20f;
    
    [SerializeField] private InputActionReference vrToggleButton;

    private int sunTemp = 6570;
    private int sunSetTemp = 1500;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // DEBUG
        Debug.Log("Press H to set to current time");
        Debug.Log("Press RIGHT_AROW to set go forward");
        Debug.Log("Press LEFT_AROW to set go backward");
        Debug.Log("Press R to set rain");
        Debug.Log("Press S to set snow");
        Debug.Log("Press N to set normal");
    }

    // Update is called once per frame
    void Update()
    {
        DateTime now = DateTime.Now;
        int hour = now.Hour;
        int angle = hour * 15 - 90; // 360 / 24
        /*if (Input.GetKeyDown(KeyCode.H))
        {
            SetRotation(angle);
            Debug.Log(hour + "h => " + angle + "°");
        }
        if (Input.GetKey(KeyCode.RightArrow))
            Forward();
        if (Input.GetKey(KeyCode.LeftArrow))
            Backward();
        */
        if (vrToggleButton != null && vrToggleButton.action.IsPressed())
        {
            Forward();
        }

        float sunRotation = sun.transform.eulerAngles.x;
        if (sunRotation < aboveAngle || sunRotation > 360 - belowAngle)
            SetSky(eveningSky, sun);
        else if (sunRotation < 180)
            SetSky(daySky, sun);
        else
            SetSky(nightSky, moon);

        int tempAngle = (int) (sunRotation + belowAngle) % 360;
        sun.colorTemperature = Mathf.Min(GetTemp(
            tempAngle,
            aboveAngle,
            belowAngle,
            sunTemp,
            sunSetTemp
            ), sunTemp);

        moon.transform.rotation = sun.transform.rotation * Quaternion.Euler(180f, 0f, 0f);
    }

    void Forward()
    {
        sun.transform.Rotate(rotationSpeed * Time.deltaTime * Vector3.right);
    }

    void Backward()
    {
        sun.transform.Rotate(-rotationSpeed * Time.deltaTime * Vector3.right);
    }

    void SetRotation(float rotation)
    {
        sun.transform.rotation = Quaternion.Euler(rotation, 0f, 0f);
    }

    void SetSky(Material sky, Light light)
    {
        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();

        sun.enabled = false;
        moon.enabled = false;
        light.enabled = true;
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
    int GetTemp(int x, int a, int b, int A, int B)
    {
        return (A - B) / (a + b) * x + B;
    }
}
