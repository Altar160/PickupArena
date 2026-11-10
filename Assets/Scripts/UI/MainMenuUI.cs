using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;

    // Créer une partie en tant que Host.
    public void HostGame()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (!CanConnect(manager))
            return;

        if (!TryGetPort(out ushort port))
            return;

        UnityTransport transport =
            manager.GetComponent<UnityTransport>();

        // Préparer l'adresse du serveur.
        transport.SetConnectionData(
            "127.0.0.1",
            port,
            "0.0.0.0"
        );

        if (manager.StartHost())
        {
            Debug.Log("Host démarré !");
            StartCoroutine(OpenLobby(manager));
        }
        else
        {
            Debug.LogError("Échec du démarrage du Host.");
        }
    }

    // Rejoindre une partie en tant que Client.
    public void JoinGame()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (!CanConnect(manager))
            return;

        if (!TryGetPort(out ushort port))
            return;

        if (ipInput == null)
        {
            Debug.LogError("Champ IP non assigné !");
            return;
        }

        string ip = ipInput.text.Trim();

        if (string.IsNullOrWhiteSpace(ip))
        {
            Debug.LogWarning("Entre une adresse IP !");
            return;
        }

        UnityTransport transport =
            manager.GetComponent<UnityTransport>();

        // Configurer l'adresse du Host.
        transport.SetConnectionData(ip, port);

        if (manager.StartClient())
        {
            Debug.Log(
                $"Connexion demandée à {ip}:{port}"
            );
        }
        else
        {
            Debug.LogError("Impossible de démarrer le Client.");
        }
    }

    // Le serveur charge le Lobby pour la session.
    private IEnumerator OpenLobby(NetworkManager manager)
    {
        yield return null;

        if (manager == null || !manager.IsServer)
            yield break;

        SceneEventProgressStatus result =
            manager.SceneManager.LoadScene(
                "Lobby",
                LoadSceneMode.Single
            );

        if (result != SceneEventProgressStatus.Started)
        {
            Debug.LogError(
                $"Impossible de charger le Lobby : {result}"
            );
        }
    }

    private bool CanConnect(NetworkManager manager)
    {
        if (manager == null)
        {
            Debug.LogError("NetworkManager introuvable !");
            return false;
        }

        if (manager.GetComponent<UnityTransport>() == null)
        {
            Debug.LogError("UnityTransport introuvable !");
            return false;
        }

        if (manager.IsListening)
        {
            Debug.LogWarning("Session déjà démarrée !");
            return false;
        }

        return true;
    }

    private bool TryGetPort(out ushort port)
    {
        port = 0;

        if (portInput == null ||
            !ushort.TryParse(portInput.text.Trim(), out port) ||
            port == 0)
        {
            Debug.LogWarning("Port invalide !");
            return false;
        }

        return true;
    }
}