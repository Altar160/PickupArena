# PickupArena



1\. Lancer le jeu



Host :



Lancer le jeu et entrer un pseudo.



Cliquer sur HOST GAME.



Attendre au moins 2 joueurs dans le Lobby.



Cliquer sur START GAME.



Client :



Lancer le jeu et entrer un pseudo.



Indiquer l'adresse IP du Host et le port (7777 par défaut).



Cliquer sur JOIN GAME.



Sur un même ordinateur, utiliser 127.0.0.1. Sur un réseau local, utiliser l'adresse IPv4 du Host.



Le jeu accepte 2 à 4 joueurs.



2\. Architecture



GameManager : gère les phases de jeu (Countdown, Playing, GameOver) et le chronomètre.



LobbyManager : gère les joueurs connectés et le lancement de la partie.



GameSceneSpawnManager : fait apparaître les joueurs à des positions aléatoires.



PlayerMovement / PlayerScore : gèrent les déplacements, les effets temporaires et les scores.



Pickup / PickupSpawner : classe abstraite, effets polymorphiques et apparition des pickups.



IPlayerEffectReceiver : interface permettant d'appliquer les effets aux joueurs.



ScoreboardUI / GameHUD : interfaces mises à jour grâce aux événements C# et aux NetworkVariables.



PickupArenaSettings : ScriptableObject regroupant les paramètres du gameplay.



3\. Choix réseau



Le jeu utilise Netcode for GameObjects et Unity Transport, avec une connexion directe par IP et port.



L'architecture est server-authoritative : le Host fait également office de serveur.



Le serveur contrôle :



Les déplacements et les positions des joueurs.



L'apparition et la collecte des pickups.



Les scores et les effets temporaires.



Le chronomètre et les changements de phase.



Les Clients transmettent leurs commandes de déplacement via des RPC. Les NetworkVariables synchronisent les informations entre joueurs.



Ce choix permet de conserver un état de jeu cohérent et d'éviter que chaque Client décide lui-même des scores ou des effets.



4\. Limitations et problèmes connus



Connexion directe IP/port uniquement, sans Relay ni matchmaking.



Si le Host quitte, la session se ferme pour tous les joueurs.



Impossible de rejoindre une partie déjà commencée.



Tests à 2 joueurs effectués ; tests complets à 3 et 4 joueurs encore à réaliser.



Aucun bug bloquant identifié lors des derniers tests à 2 joueurs.





Utilisation de l'IA



J'ai utilisé ChatGPT principalement pour m'aider à rechercher et à comprendre la documentation de Netcode for GameObjects, une technologie que je n'avais encore jamais utilisée. La mise en place du multijoueur a été la partie qui m'a demandé le plus de temps et d'apprentissage.



ChatGPT m'a également aidé à identifier et corriger des erreurs, à comprendre certaines parties du code et à ajouter des commentaires pour améliorer sa lisibilité.

