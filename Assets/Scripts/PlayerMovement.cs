
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector2 serverInput;
    private Vector2 lastSentInput;

    private void Update()
    {
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
        if (!IsSpawned || !IsServer) return;

        Vector3 direction = new Vector3(
            serverInput.x,
            0f,
            serverInput.y
        );

        transform.position +=
            direction * moveSpeed * Time.fixedDeltaTime;
    }
}
