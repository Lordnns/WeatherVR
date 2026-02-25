using UnityEngine;

public class VRCenteredThrobber : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private RectTransform throbberIcon;
    [SerializeField] private float rotationSpeed = 180f;
    
    void LateUpdate()
    {
        // 3. Spin the throbber icon itself
        if (throbberIcon != null)
        {
            throbberIcon.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
}