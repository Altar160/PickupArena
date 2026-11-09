
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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Seul le propriétaire contrôle son personnage.
        if (!IsSpawned || !IsOwner) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 input = Vector2.zero;

        // Avancer (Z ou W).
        if (keyboard.zKey.isPressed || keyboard.wKey.isPressed)
            input.y += 1f;

        // Reculer (S).
        if (keyboard.sKey.isPressed)
            input.y -= 1f;

        // Gauche (Q ou A).
        if (keyboard.qKey.isPressed || keyboard.aKey.isPressed)
            input.x -= 1f;

        // Droite (D).
        if (keyboard.dKey.isPressed)
            input.x += 1f;

        // Normaliser pour éviter une vitesse supérieure
        // lors des déplacements en diagonale.
        input = input.normalized;

        // Envoyer la direction au serveur si elle change.
        if (input != lastSentInput)
        {
            lastSentInput = input;
            SendMovementRpc(input);
        }
    }

    [Rpc(SendTo.Server)]
    private void SendMovementRpc(Vector2 input)
    {
        // Le serveur valide la direction.
        serverInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void FixedUpdate()
    {
        // Seul le serveur déplace les personnages.
        if (!IsSpawned || !IsServer) return;

        Vector3 direction = new Vector3(
            serverInput.x,
            0f,
            serverInput.y
        );

        // Déplacement avec gestion des collisions.
        controller.Move(
            direction * moveSpeed * Time.fixedDeltaTime
        );
    }

    public override void OnNetworkDespawn()
    {
        // Réinitialiser les mouvements.
        serverInput = Vector2.zero;
        lastSentInput = Vector2.zero;
    }
}
