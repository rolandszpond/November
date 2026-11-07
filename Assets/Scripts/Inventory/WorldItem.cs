using UnityEngine;

// An item stack lying in the world. Interact picks up as much as fits; the rest stays here.
// Place one in a scene (set item + count) or create one at runtime with Spawn (drops, overflow).
// The visual is the item's worldPrefab; its own colliders are disabled and a box fitted to it is used instead, so
// any Synty mesh works with a Rigidbody.
[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(1)] private int count = 1;

    public ItemStack Stack => new ItemStack(item, count);

    public static WorldItem Spawn(ItemStack stack, Vector3 position, Vector3 impulse = default)
    {
        var go = new GameObject($"WorldItem_{stack.item.id}");
        go.transform.position = position;
        go.SetActive(false); // so Awake runs after item/count are set
        var worldItem = go.AddComponent<WorldItem>();
        worldItem.item = stack.item;
        worldItem.count = stack.count;
        go.SetActive(true);

        var body = go.GetComponent<Rigidbody>();
        if (impulse != Vector3.zero) body.AddForce(impulse, ForceMode.VelocityChange);
        return worldItem;
    }

    private void Awake()
    {
        if (item == null)
        {
            Debug.LogWarning($"{name}: WorldItem has no item.", this);
            return;
        }
        BuildVisual();

        var body = GetComponent<Rigidbody>();
        body.mass = Mathf.Max(0.1f, item.weight * count);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void BuildVisual()
    {
        // Scene-placed items may already have their visual as a child; only add one if there's none.
        if (transform.childCount == 0)
        {
            GameObject visual;
            if (item.worldPrefab != null)
            {
                visual = Instantiate(item.worldPrefab, transform);
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(transform, false);
                visual.transform.localScale = Vector3.one * 0.3f;
            }
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale *= item.worldScale;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            if (col.gameObject != gameObject) col.enabled = false;
        }

        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                box.center = transform.InverseTransformPoint(bounds.center);
                Vector3 size = transform.InverseTransformVector(bounds.size);
                box.size = new Vector3(Mathf.Max(0.1f, Mathf.Abs(size.x)), Mathf.Max(0.1f, Mathf.Abs(size.y)), Mathf.Max(0.1f, Mathf.Abs(size.z)));
            }
        }
    }

    public bool CanInteract(PlayerInteractor interactor) => item != null && count > 0 && interactor.Inventory != null;

    public string GetPrompt(PlayerInteractor interactor) =>
        count > 1 ? $"Pick up {item.DisplayName} x{count}" : $"Pick up {item.DisplayName}";

    public void Interact(PlayerInteractor interactor)
    {
        int leftover = interactor.Inventory.Add(item, count);
        if (leftover == count)
        {
            HudMessages.Show("Inventory full");
            return;
        }

        count = leftover;
        if (count <= 0) Destroy(gameObject);
    }
}
