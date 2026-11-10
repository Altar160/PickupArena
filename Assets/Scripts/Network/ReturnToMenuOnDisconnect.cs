using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class ReturnToMenuOnDisconnect : MonoBehaviour
{
    private NetworkManager manager;
    private bool returnScheduled;

    private void Awake()
    {
        manager = GetComponent<NetworkManager>();

        manager.OnClientStopped += OnClientStopped;
        manager.OnServerStopped += OnServerStopped;
    }

    // Appelé quand un client quitte la session.
    private void OnClientStopped(bool wasHost)
    {
        // Le Host attend l'arrêt complet du serveur.
        if (wasHost)
            return;

        ScheduleReturn();
    }

    // Appelé quand le Host arrête le serveur.
    private void OnServerStopped(bool wasHost)
    {
        if (!wasHost)
            return;

        ScheduleReturn();
    }

    private void ScheduleReturn()
    {
        if (returnScheduled)
            return;

        // Ne pas recharger inutilement le menu.
        if (SceneManager.GetActiveScene().name == "MainMenu")
            return;

        returnScheduled = true;

        // Créer un objet temporaire indépendant
        // pour terminer le nettoyage de la session.
        GameObject transition =
            new GameObject("ReturnToMenuTransition");

        DontDestroyOnLoad(transition);

        transition.AddComponent<LocalMenuReturn>()
            .Begin(manager);
    }

    private void OnDestroy()
    {
        if (manager == null)
            return;

        manager.OnClientStopped -= OnClientStopped;
        manager.OnServerStopped -= OnServerStopped;
    }
}

// Cet objet termine le retour au menu
// sans dépendre de l'ancien NetworkManager.
public class LocalMenuReturn : MonoBehaviour
{
    private NetworkManager previousManager;

    public void Begin(NetworkManager manager)
    {
        previousManager = manager;
        StartCoroutine(ReloadMenu());
    }

    private IEnumerator ReloadMenu()
    {
        // Laisser Netcode terminer son arrêt.
        yield return null;

        // Éviter de conserver un ancien NetworkManager
        // lorsque MainMenu sera rechargé.
        if (previousManager != null)
            Destroy(previousManager.gameObject);

        yield return null;

        // Chaque ordinateur recharge son menu localement.
        SceneManager.LoadScene(
            "MainMenu",
            LoadSceneMode.Single
        );

        Destroy(gameObject);
    }
}