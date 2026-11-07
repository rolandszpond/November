using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Runs the build-tool FSM while in Build mode (enabled/disabled by BuildModeState) and gives the tools
// shared helpers: mouse input, ground raycasts, ghost previews and placement checks.
// Shortcuts: 1 Wall, 2 Floor, 3 Object, 4 Demolish. Esc/right-click cancels a drag, or drops the tool.
public class BuildController : MonoBehaviour
{
    [SerializeField] private BuildWorld world;
    [SerializeField] private BuildToolbarUI toolbar;
    [Tooltip("Transparent URP material used for previews; tinted green (valid) / red (invalid) at runtime.")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.4f, 0.45f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.25f, 0.2f, 0.45f);
    [Tooltip("Objects snap to this grid (metres).")]
    [SerializeField] private float objectSnap = 0.25f;

    private Camera cam;
    private Material validMaterial;
    private Material invalidMaterial;
    private readonly Dictionary<GameObject, Bounds> localBoundsCache = new Dictionary<GameObject, Bounds>();
    private readonly Collider[] overlapResults = new Collider[8];

    public BuildWorld World => world;
    public GhostPool Ghosts { get; private set; }
    public Material InvalidMaterial => invalidMaterial;
    public float ObjectSnap => objectSnap;

    public StateMachine Tools { get; private set; }
    public IBuildTool CurrentTool => (IBuildTool)Tools.CurrentState;
    public NoTool NoTool { get; private set; }
    public WallTool WallTool { get; private set; }
    public FloorTool FloorTool { get; private set; }
    public ObjectTool ObjectTool { get; private set; }
    public DemolishTool DemolishTool { get; private set; }
    public IReadOnlyList<IBuildTool> SelectableTools { get; private set; }

    public event Action ToolChanged;

    // ---- Input (read by tools) ----

    public bool PointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    public bool PrimaryPressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !PointerOverUI;
    public bool PrimaryReleased => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    public bool RotatePressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
    public bool CancelPressed =>
        (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);

    private void Awake()
    {
        cam = Camera.main;

        validMaterial = new Material(ghostMaterial) { name = "Ghost (Valid)" };
        validMaterial.SetColor("_BaseColor", validColor);
        invalidMaterial = new Material(ghostMaterial) { name = "Ghost (Invalid)" };
        invalidMaterial.SetColor("_BaseColor", invalidColor);

        var ghostRoot = new GameObject("Build Ghosts").transform;
        ghostRoot.SetParent(transform, false);
        Ghosts = new GhostPool(ghostRoot, validMaterial, invalidMaterial);

        Tools = new StateMachine();
        NoTool = new NoTool();
        WallTool = new WallTool(this);
        FloorTool = new FloorTool(this);
        ObjectTool = new ObjectTool(this);
        DemolishTool = new DemolishTool(this);
        SelectableTools = new IBuildTool[] { WallTool, FloorTool, ObjectTool, DemolishTool };
    }

    private void OnEnable()
    {
        SetTool(NoTool);
        toolbar.Show(this);
    }

    private void OnDisable()
    {
        SetTool(NoTool);
        toolbar.Hide();
    }

    private void Update()
    {
        HandleShortcuts();
        Tools.Update();
    }

    public void SetTool(IBuildTool tool)
    {
        Tools.ChangeState(tool);
        Ghosts.HideAll();
        ToolChanged?.Invoke();
    }

    public void SelectItem(int index)
    {
        CurrentTool.SelectedIndex = index;
        ToolChanged?.Invoke();
    }

    private void HandleShortcuts()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame) SetTool(WallTool);
        else if (keyboard.digit2Key.wasPressedThisFrame) SetTool(FloorTool);
        else if (keyboard.digit3Key.wasPressedThisFrame) SetTool(ObjectTool);
        else if (keyboard.digit4Key.wasPressedThisFrame) SetTool(DemolishTool);
    }

    // ---- Helpers for tools ----

    // Where the mouse ray meets the build plane.
    public bool TryGetPointerOnGround(out Vector3 point)
    {
        point = default;
        if (Mouse.current == null)
        {
            return false;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        var plane = new Plane(Vector3.up, new Vector3(0f, world.GroundHeight, 0f));
        if (!plane.Raycast(ray, out float distance) || distance > 500f)
        {
            return false;
        }

        point = ray.GetPoint(distance);
        return true;
    }

    // The built piece (wall, floor or object) under the mouse, if any.
    public BuiltPiece PieceUnderPointer()
    {
        if (Mouse.current == null || PointerOverUI)
        {
            return null;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out RaycastHit hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? hit.collider.GetComponentInParent<BuiltPiece>()
            : null;
    }

    // True if the prefab's bounding box at this pose doesn't overlap any collider (walls, other objects,
    // the player...). The bottom of the box is raised a little so floors/the ground underneath don't count.
    public bool CanPlaceObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Bounds local = GetLocalBounds(prefab);
        const float lift = 0.1f;
        Vector3 min = local.min + Vector3.up * lift;
        Vector3 max = local.max;
        if (max.y <= min.y)
        {
            return true;
        }

        Vector3 center = position + rotation * ((min + max) * 0.5f);
        Vector3 halfExtents = (max - min) * 0.5f * 0.95f;
        int hits = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapResults, rotation,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        return hits == 0;
    }

    // Mesh bounds of a prefab in its own root space, cached per prefab.
    private Bounds GetLocalBounds(GameObject prefab)
    {
        if (localBoundsCache.TryGetValue(prefab, out Bounds bounds))
        {
            return bounds;
        }

        bool first = true;
        Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }

            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        localBoundsCache.Add(prefab, bounds);
        return bounds;
    }
}
