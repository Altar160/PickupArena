using UnityEngine;
using Unity.Netcode;

public abstract class Pickup : NetworkBehaviour
{
    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        // Seul le serveur gère les ramassages.
        if (!IsSpawned || !IsServer || isCollected)
            return;

        // Les pickups fonctionnent uniquement pendant Playing.
        if (GameManager.Instance == null ||
            GameManager.Instance.State.Value != GamePhase.Playing)
            return;

        // Identifier le joueur.
        NetworkObject player =
            other.GetComponentInParent<NetworkObject>();

        if (player == null || !player.IsPlayerObject)
            return;

        // Éviter les doubles ramassages.
        isCollected = true;

        // Appliquer l'effet spécifique.
        ApplyEffect(player);

        // Supprimer le pickup pour tous les joueurs.
        NetworkObject.Despawn();
    }

    protected abstract void ApplyEffect(NetworkObject player);
}