using System.Collections;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private TMP_InputField nicknameInput;

    [Header("Connection Settings")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;

    public void HostGame()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (!CanConnect(manager) ||
            !TryGetPort(out ushort port))
            return;

        string nickname = GetNickname();

        UnityTransport transport =
            manager.GetComponent<UnityTransport>();

        transport.SetConnectionData(
            "127.0.0.1",
            port,
            "0.0.0.0"
        );

        // Préparer les données du joueur.
        manager.NetworkConfig.ConnectionData =
            Encoding.UTF8.GetBytes(nickname);

        if (manager.StartHost())
        {
            // Conserver le pseudo du Host.
            SessionConnectionApproval approval =
                manager.GetComponent<SessionConnectionApproval>();

            if (approval != null)
                approval.SetHostName(nickname);

            Debug.Log($"Host démarré : {nickname}");

            StartCoroutine(OpenLobby(manager));
        }
        else
        {
            Debug.LogError("Impossible de démarrer le Host.");
        }
    }

    public void JoinGame()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (!CanConnect(manager) ||
            !TryGetPort(out ushort port))
            return;

        if (ipInput == null ||
            string.IsNullOrWhiteSpace(ipInput.text))
        {
            Debug.LogWarning("Adresse IP manquante !");
            return;
        }

        string nickname = GetNickname();
        string ip = ipInput.text.Trim();

        UnityTransport transport =
            manager.GetComponent<UnityTransport>();

        transport.SetConnectionData(ip, port);

        // Envoyer le pseudo au serveur lors de la connexion.
        manager.NetworkConfig.ConnectionData =
            Encoding.UTF8.GetBytes(nickname);

        if (manager.StartClient())
        {
            Debug.Log(
                $"{nickname} tente de rejoindre {ip}:{port}"
            );
        }
        else
        {
            Debug.LogError("Connexion impossible.");
        }
    }

    private string GetNickname()
    {
        string nickname = nicknameInput != null
            ? nicknameInput.text
            : "";

        return SessionConnectionApproval.CleanNickname(
            nickname
        );
    }

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
                $"Chargement Lobby impossible : {result}"
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
            Debug.LogWarning("Session déjà démarrée.");
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

    public void QuitGame()
    {
        Debug.Log("Fermeture du jeu...");

#if UNITY_EDITOR
        // Dans Unity Editor : arrêter le Play Mode.
        UnityEditor.EditorApplication.isPlaying = false;
#else
    // Dans le jeu compilé : fermer l'application.
    Application.Quit();
#endif
    }
}