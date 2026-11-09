
using UnityEngine;
using Unity.Netcode;

public abstract class Pickup : NetworkBehaviour
{
    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        // Seul le serveur gère les ramassages.
        if (!IsServer || !IsSpawned || isCollected)
            return;

        // Vérifier que l'objet touché est un joueur.
        NetworkObject player =
            other.GetComponentInParent<NetworkObject>();

        if (player == null || !player.IsPlayerObject)
            return;

        // Empêcher un double ramassage.
        isCollected = true;

        // Appliquer l'effet spécifique au pickup.
        ApplyEffect(player);

        // Supprimer le pickup pour tous les joueurs.
        NetworkObject.Despawn();
    }

    protected abstract void ApplyEffect(NetworkObject player);
}
