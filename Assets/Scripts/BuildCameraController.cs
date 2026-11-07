using UnityEngine;
using UnityEngine.InputSystem;

// Sims-style top-down camera for Build mode. Lives on a CinemachineCamera with no Position/Rotation
// Control, so Cinemachine just uses this transform; the Brain handles the blend in and out.
// WASD/arrows pan, Q/E rotate, mouse wheel zooms. Uses unscaled time so it still works if the game is paused.
public class BuildCameraController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private float pitch = 55f;
    [SerializeField] private float startDistance = 15f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 40f;

    [Header("Controls")]
    [Tooltip("Pan speed (m/s) at startDistance; scales with zoom.")]
    [SerializeField] private float panSpeed = 12f;
    [SerializeField] private float rotateSpeed = 90f;
    [Tooltip("Fraction of the current distance zoomed per scroll notch.")]
    [SerializeField] private float zoomStep = 0.15f;
    [Tooltip("Higher = camera catches up with its target faster.")]
    [SerializeField] private float smoothing = 10f;

    private Vector3 pivot;
    private float yaw;
    private float distance;

    // Centre the view on a point, facing the given heading. Snaps there; the Cinemachine blend does the transition.
    public void Begin(Vector3 focus, float heading)
    {
        pivot = focus;
        yaw = heading;
        distance = startDistance;
        GetTargetPose(out Vector3 position, out Quaternion rotation);
        transform.SetPositionAndRotation(position, rotation);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        float dt = Time.unscaledDeltaTime;

        Vector2 move = Vector2.zero;
        float rotate = 0f;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.eKey.isPressed) rotate -= 1f;
            if (keyboard.qKey.isPressed) rotate += 1f;
        }

        if (mouse != null)
        {
            // Raw scroll values differ between platforms/devices, so only the direction is used.
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance *= 1f - Mathf.Sign(scroll) * zoomStep;
            }
        }

        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        yaw += rotate * rotateSpeed * dt;

        Vector3 pan = Quaternion.Euler(0f, yaw, 0f) * new Vector3(move.x, 0f, move.y);
        pivot += Vector3.ClampMagnitude(pan, 1f) * (panSpeed * (distance / startDistance) * dt);

        GetTargetPose(out Vector3 targetPosition, out Quaternion targetRotation);
        float t = 1f - Mathf.Exp(-smoothing * dt);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, targetPosition, t),
            Quaternion.Slerp(transform.rotation, targetRotation, t));
    }

    private void GetTargetPose(out Vector3 position, out Quaternion rotation)
    {
        rotation = Quaternion.Euler(pitch, yaw, 0f);
        position = pivot - rotation * Vector3.forward * distance;
    }
}
