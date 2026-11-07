using UnityEngine;
using UnityEngine.InputSystem;

// Finds the interactable the player is looking at and uses it on E. Looks along the camera's centre ray first; if
// that hits nothing usable, falls back to the closest interactable in front of the player, since aiming at small
// pickups with a third-person camera is fiddly. Only enabled in Play mode (GameModeController toggles it).
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameModeController game;
    [Tooltip("Max distance from the player to an interactable.")]
    [SerializeField] private float range = 2.5f;
    [SerializeField] private LayerMask layers = ~0;
    [SerializeField] private InputAction interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");

    private Camera cam;
    private readonly RaycastHit[] hits = new RaycastHit[16];
    private readonly Collider[] overlaps = new Collider[32];

    public PlayerInventory Inventory => inventory;
    public GameModeController Game => game;
    public IInteractable Current { get; private set; }
    public string CurrentPrompt => Current != null ? Current.GetPrompt(this) : null;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (game == null) game = FindAnyObjectByType<GameModeController>();
    }

    private void OnEnable() => interactAction.Enable();

    private void OnDisable()
    {
        interactAction.Disable();
        Current = null;
    }

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        Current = FindTarget();

        if (Current != null && interactAction.WasPressedThisFrame())
        {
            Current.Interact(this);
        }
    }

    private IInteractable FindTarget()
    {
        Vector3 center = transform.position + Vector3.up;

        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float maxDistance = Vector3.Distance(cam.transform.position, center) + range;
            int count = Physics.RaycastNonAlloc(ray, hits, maxDistance, layers, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            IInteractable result = null;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= best) continue;
                // The nearest non-player hit decides: if it's a wall, don't look through it.
                best = hit.distance;
                result = Usable(hit.collider, hit.point, center);
            }
            if (result != null) return result;
        }

        // Fallback: closest usable thing in a cone in front of the player.
        int n = Physics.OverlapSphereNonAlloc(center, range, overlaps, layers, QueryTriggerInteraction.Collide);
        IInteractable closest = null;
        float closestScore = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var col = overlaps[i];
            if (col.transform.IsChildOf(transform)) continue;
            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;
            // Bounds, not Collider.ClosestPoint: that only supports primitive and convex colliders.
            Vector3 point = col.bounds.ClosestPoint(center);
            Vector3 to = point - center;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, to) > 60f) continue;
            var usable = Usable(col, point, center);
            if (usable == null) continue;
            float score = to.sqrMagnitude;
            if (score < closestScore)
            {
                closestScore = score;
                closest = usable;
            }
        }
        return closest;
    }

    private IInteractable Usable(Collider col, Vector3 point, Vector3 center)
    {
        if (Vector3.Distance(point, center) > range) return null;
        var interactable = col.GetComponentInParent<IInteractable>();
        return interactable != null && interactable.CanInteract(this) ? interactable : null;
    }
}
