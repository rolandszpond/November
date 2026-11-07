using UnityEngine;
using UnityEngine.InputSystem;

// Places a placeable item (e.g. a crafting table) in front of the player: a ghost follows the camera's centre
// ray on the ground, R rotates, left click places it and uses up one item, right click / Esc cancels.
public class ItemPlacer : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [Tooltip("Transparent material for the placement preview (the build ghost material works).")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.4f, 0.4f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.4f);
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float rotateStep = 45f;
    [SerializeField] private LayerMask groundLayers = 1;

    private ItemDefinition item;
    private GameObject ghost;
    private Renderer[] ghostRenderers;
    private MaterialPropertyBlock block;
    private float yaw;
    private bool valid;
    private Camera cam;

    public bool IsPlacing => item != null;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        block = new MaterialPropertyBlock();
    }

    private void OnDisable() => Cancel();

    public void Begin(ItemDefinition placeable)
    {
        Cancel();
        item = placeable;
        yaw = transform.eulerAngles.y + 180f; // face the player
        ghost = Instantiate(placeable.placeable.placedPrefab);
        ghost.name = "PlacementGhost";
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var behaviour in ghost.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
        if (ghostMaterial != null)
        {
            foreach (var r in ghostRenderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                r.sharedMaterials = mats;
            }
        }
        HudMessages.Show($"Placing {placeable.DisplayName}: LMB place, R rotate, RMB cancel");
    }

    public void Cancel()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        item = null;
    }

    private void Update()
    {
        if (!IsPlacing) return;
        if (inventory.Count(item) <= 0)
        {
            Cancel();
            return;
        }

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
        {
            Cancel();
            return;
        }
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) yaw += rotateStep;

        UpdateGhost();

        if (valid && mouse != null && mouse.leftButton.wasPressedThisFrame && !UIUtil.IsPointerOverUI())
        {
            Instantiate(item.placeable.placedPrefab, ghost.transform.position, ghost.transform.rotation);
            inventory.Remove(item, 1);
            Cancel();
        }
    }

    private void UpdateGhost()
    {
        if (cam == null) cam = Camera.main;
        valid = false;
        Vector3 position = transform.position + transform.forward * 2f;

        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float rayLength = Vector3.Distance(cam.transform.position, transform.position) + maxDistance;
            var hits = Physics.RaycastAll(ray, rayLength, groundLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= best) continue;
                best = hit.distance;
                position = hit.point;
                valid = hit.normal.y > 0.7f && Vector3.Distance(hit.point, transform.position) <= maxDistance;
            }
        }

        ghost.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        block.SetColor("_BaseColor", valid ? validColor : invalidColor);
        foreach (var r in ghostRenderers) r.SetPropertyBlock(block);
    }
}
