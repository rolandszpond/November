using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Top-level FSM: which mode the game is in (Play or Build). Separate from the player's locomotion FSM,
// which keeps running inside PlayerController while in Play mode and is paused in Build mode.
public class GameModeController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [Tooltip("Mouse-orbit input of the third-person camera. Disabled in Build mode so it doesn't orbit in the background.")]
    [SerializeField] private CinemachineInputAxisController playerCameraInput;
    [Tooltip("Top-down Cinemachine camera. Its GameObject is only active in Build mode; the Brain blends to it.")]
    [SerializeField] private BuildCameraController buildCamera;
    [Tooltip("Build tools + toolbar. Only enabled in Build mode.")]
    [SerializeField] private BuildController build;
    [SerializeField] private InputAction toggleBuildAction = new InputAction("ToggleBuild", InputActionType.Button, "<Keyboard>/b");

    public PlayerController Player => player;
    public CinemachineInputAxisController PlayerCameraInput => playerCameraInput;
    public BuildCameraController BuildCamera => buildCamera;
    public BuildController Build => build;

    public StateMachine StateMachine { get; private set; }
    public PlayModeState PlayMode { get; private set; }
    public BuildModeState BuildMode { get; private set; }

    public bool ToggleBuildPressed => toggleBuildAction.WasPressedThisFrame();

    private void Awake()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (playerCameraInput == null) playerCameraInput = FindAnyObjectByType<CinemachineInputAxisController>();
        if (buildCamera == null) buildCamera = FindAnyObjectByType<BuildCameraController>(FindObjectsInactive.Include);

        if (build == null) build = FindAnyObjectByType<BuildController>(FindObjectsInactive.Include);

        buildCamera.gameObject.SetActive(false);
        build.enabled = false;

        StateMachine = new StateMachine();
        PlayMode = new PlayModeState(this);
        BuildMode = new BuildModeState(this);
    }

    // Start, not Awake: PlayerController builds its own FSM in Awake, and that has to exist first.
    private void Start()
    {
        StateMachine.ChangeState(PlayMode);
    }

    private void OnEnable() => toggleBuildAction.Enable();
    private void OnDisable() => toggleBuildAction.Disable();

    private void Update()
    {
        StateMachine.Update();
    }
}
