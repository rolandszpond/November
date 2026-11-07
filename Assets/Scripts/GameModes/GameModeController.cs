using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Top-level FSM: which mode the game is in (Play, Build or Inventory). Separate from the player's locomotion FSM,
// which keeps running inside PlayerController while in Play mode and is paused in the other modes.
public class GameModeController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [Tooltip("Mouse-orbit input of the third-person camera. Disabled in Build mode so it doesn't orbit in the background.")]
    [SerializeField] private CinemachineInputAxisController playerCameraInput;
    [Tooltip("Top-down Cinemachine camera. Its GameObject is only active in Build mode; the Brain blends to it.")]
    [SerializeField] private BuildCameraController buildCamera;
    [Tooltip("Build tools + toolbar. Only enabled in Build mode.")]
    [SerializeField] private BuildController build;
    [Tooltip("Inventory / crafting / loot screen. Shown in Inventory mode.")]
    [SerializeField] private InventoryScreen inventoryScreen;
    [Tooltip("Player components that only run in Play mode (interactor, hotbar input, item placer).")]
    [SerializeField] private Behaviour[] playModeBehaviours;
    [SerializeField] private InputAction toggleBuildAction = new InputAction("ToggleBuild", InputActionType.Button, "<Keyboard>/b");
    [SerializeField] private InputAction toggleInventoryAction = new InputAction("ToggleInventory", InputActionType.Button, "<Keyboard>/tab");
    [SerializeField] private InputAction closeAction = new InputAction("CloseMenu", InputActionType.Button, "<Keyboard>/escape");

    public PlayerController Player => player;
    public CinemachineInputAxisController PlayerCameraInput => playerCameraInput;
    public BuildCameraController BuildCamera => buildCamera;
    public BuildController Build => build;
    public InventoryScreen InventoryScreen => inventoryScreen;

    public StateMachine StateMachine { get; private set; }
    public PlayModeState PlayMode { get; private set; }
    public BuildModeState BuildMode { get; private set; }
    public InventoryModeState InventoryMode { get; private set; }

    public bool ToggleBuildPressed => toggleBuildAction.WasPressedThisFrame();
    public bool ToggleInventoryPressed => toggleInventoryAction.WasPressedThisFrame();
    public bool ClosePressed => closeAction.WasPressedThisFrame();

    private void Awake()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (playerCameraInput == null) playerCameraInput = FindAnyObjectByType<CinemachineInputAxisController>();
        if (buildCamera == null) buildCamera = FindAnyObjectByType<BuildCameraController>(FindObjectsInactive.Include);
        if (build == null) build = FindAnyObjectByType<BuildController>(FindObjectsInactive.Include);
        if (inventoryScreen == null) inventoryScreen = FindAnyObjectByType<InventoryScreen>(FindObjectsInactive.Include);
        if (playModeBehaviours == null || playModeBehaviours.Length == 0)
        {
            playModeBehaviours = new Behaviour[]
            {
                player.GetComponent<PlayerInteractor>(),
                player.GetComponent<HotbarController>(),
                player.GetComponent<ItemPlacer>(),
            };
        }

        buildCamera.gameObject.SetActive(false);
        build.enabled = false;

        StateMachine = new StateMachine();
        PlayMode = new PlayModeState(this);
        BuildMode = new BuildModeState(this);
        InventoryMode = new InventoryModeState(this);
        SetPlayModeBehaviours(false);
    }

    // Start, not Awake: PlayerController builds its own FSM in Awake, and that has to exist first.
    private void Start()
    {
        StateMachine.ChangeState(PlayMode);
    }

    private void OnEnable()
    {
        toggleBuildAction.Enable();
        toggleInventoryAction.Enable();
        closeAction.Enable();
    }

    private void OnDisable()
    {
        toggleBuildAction.Disable();
        toggleInventoryAction.Disable();
        closeAction.Disable();
    }

    private void Update()
    {
        StateMachine.Update();
    }

    // Opens the inventory screen from Play mode (Tab, a crafting station or a loot chest).
    public void OpenInventory(InventoryContext context)
    {
        if (StateMachine.CurrentState != PlayMode || inventoryScreen == null)
        {
            return;
        }
        InventoryMode.Context = context;
        StateMachine.ChangeState(InventoryMode);
    }

    public void SetPlayModeBehaviours(bool enabled)
    {
        foreach (var behaviour in playModeBehaviours)
        {
            if (behaviour != null) behaviour.enabled = enabled;
        }
    }
}
