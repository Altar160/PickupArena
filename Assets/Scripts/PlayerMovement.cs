using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController controller;

    private Vector2 serverInput;
    private Vector2 lastSentInput;

    // Speed Boost
    private float currentSpeedMultiplier = 1f;
    private float speedBoostEndTime = 0f;

    // Freeze Bomb
    private float freezeEndTime = 0f;

    public readonly NetworkVariable<bool> IsFrozen =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        IsFrozen.OnValueChanged += OnFreezeChanged;
    }

    private void Update()
    {
        // Seul le propriétaire lit son clavier.
        if (!IsSpawned || !IsOwner) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 input = Vector2.zero;

        if (keyboard.zKey.isPressed || keyboard.wKey.isPressed)
            input.y += 1f;

        if (keyboard.sKey.isPressed)
            input.y -= 1f;

        if (keyboard.qKey.isPressed || keyboard.aKey.isPressed)
            input.x -= 1f;

        if (keyboard.dKey.isPressed)
            input.x += 1f;

        input = input.normalized;

        if (input != lastSentInput)
        {
            lastSentInput = input;
            SendMovementRpc(input);
        }
    }

    [Rpc(SendTo.Server)]
    private void SendMovementRpc(Vector2 input)
    {
        serverInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void FixedUpdate()
    {
        // Seul le serveur contrôle les déplacements.
        if (!IsSpawned || !IsServer) return;

        // Vérifier la fin du Speed Boost.
        if (currentSpeedMultiplier > 1f &&
            Time.time >= speedBoostEndTime)
        {
            currentSpeedMultiplier = 1f;
            speedBoostEndTime = 0f;
        }

        // Vérifier la fin du Freeze.
        if (IsFrozen.Value && Time.time >= freezeEndTime)
        {
            IsFrozen.Value = false;
            freezeEndTime = 0f;
        }

        // IMPORTANT :
        // Aucun déplacement en dehors de Playing.
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsSpawned ||
            GameManager.Instance.State.Value != GamePhase.Playing)
        {
            return;
        }

        // Un joueur gelé ne peut pas bouger.
        if (IsFrozen.Value)
            return;

        Vector3 direction = new Vector3(
            serverInput.x,
            0f,
            serverInput.y
        );

        float currentSpeed =
            moveSpeed * currentSpeedMultiplier;

        controller.Move(
            direction * currentSpeed * Time.fixedDeltaTime
        );
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (!IsSpawned || !IsServer) return;
        if (multiplier <= 1f || duration <= 0f) return;

        currentSpeedMultiplier = multiplier;
        speedBoostEndTime = Time.time + duration;
    }

    public void ApplyFreeze(float duration)
    {
        if (!IsSpawned || !IsServer) return;
        if (duration <= 0f) return;

        freezeEndTime = Mathf.Max(
            freezeEndTime,
            Time.time + duration
        );

        IsFrozen.Value = true;
    }

    private void OnFreezeChanged(bool oldValue, bool newValue)
    {
        if (newValue)
            Debug.Log($"Joueur {OwnerClientId} gelé !");
        else
            Debug.Log($"Joueur {OwnerClientId} libéré !");
    }

    public override void OnNetworkDespawn()
    {
        IsFrozen.OnValueChanged -= OnFreezeChanged;

        serverInput = Vector2.zero;
        lastSentInput = Vector2.zero;

        currentSpeedMultiplier = 1f;
        speedBoostEndTime = 0f;
        freezeEndTime = 0f;
    }
}