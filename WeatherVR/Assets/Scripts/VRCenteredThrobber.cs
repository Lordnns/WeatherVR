using UnityEngine;

public class VRCenteredThrobber : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Transform vrCamera; // Assign Main Camera / CenterEyeAnchor
    [SerializeField] private float distanceFromCamera = 2.0f; // Meters in front of face

    [Header("Animation")]
    [SerializeField] private RectTransform throbberIcon;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Smoothing (Optional)")]
    [SerializeField] private bool smoothFollow = true;
    [SerializeField] private float followSpeed = 5f;

    void LateUpdate()
    {
        if (vrCamera == null) return;

        // 1. Calculate the target position in front of the VR Camera
        Vector3 targetPosition = vrCamera.position + (vrCamera.forward * distanceFromCamera);

        // 2. Calculate the target rotation to face the player
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - vrCamera.position);

        if (smoothFollow)
        {
            // Smoothly move the UI so it doesn't "jitter" with small head movements
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
        else
        {
            // Hard lock to the face
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        // 3. Spin the throbber icon itself
        if (throbberIcon != null)
        {
            throbberIcon.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
}