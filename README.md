# Deskflow Game Lock

**Garde la souris dans le jeu quand le jeu la confine. Retrouve le passage vers le Mac quand il la libère.**

[Télécharger la version Windows](https://github.com/frankbrg/deskflow-game-lock/releases/latest) · [English](README.en.md) · [Signaler un problème](https://github.com/frankbrg/deskflow-game-lock/issues)

Petit compagnon Windows pour [Deskflow](https://github.com/deskflow/deskflow), le logiciel de partage de souris et de clavier entre ordinateurs. Il évite de basculer accidentellement vers un autre ordinateur pendant une partie, sans bloquer systématiquement les vidéos en plein écran.

> **Version initiale expérimentale.** Testée avec Windows 11, Deskflow **1.27.0 en mode serveur/service** et Cities: Skylines II. La détection suit le confinement Windows de la souris ; elle ne reconnaît pas tous les jeux. La limite des configurations avec un seul écran physique est expliquée ci-dessous.

## Ce que ça change

| Situation | Comportement |
| --- | --- |
| Vidéo ou navigateur en plein écran, pointeur libre | Le passage vers le Mac reste autorisé. |
| Jeu qui confine la souris, même en mode fenêtré ou sans bordures | Deskflow est verrouillé sur le PC. |
| Menu de pause qui libère le pointeur | Le passage vers le Mac est rétabli automatiquement. |
| Alt+Tab et libération du pointeur par le jeu | Le verrouillage automatique est retiré. |
| Verrouillage manuel déjà activé | Il est conservé. |

Ce n'est **pas** un détecteur de plein écran, ni une liste de jeux. Le signal utilisé est la zone dans laquelle Windows autorise réellement le déplacement du pointeur.

## Prérequis

- **Windows 10 ou 11, x64**, sur le PC qui partage sa souris et son clavier.
- **.NET Framework 4.8 ou ultérieur** — aucun SDK .NET n'est nécessaire pour utiliser la version compilée.
- Deskflow **1.27.0**, configuré comme **serveur**, avec son service Windows et le journal au niveau **Info** ou plus détaillé.
- Journal Deskflow à son emplacement standard : `%ProgramData%\Deskflow\deskflow-daemon.log`.
- Raccourci de verrouillage Deskflow par défaut : **Arrêt défil / Scroll Lock**. Ne pas désactiver cette fonction ni remplacer l'action de verrouillage par un autre raccourci.

Rien à installer sur le Mac ou les autres clients Deskflow. Les autres versions de Deskflow et les journaux personnalisés ne sont pas validés.

### Important : un seul écran physique côté Windows

Si un jeu confine le pointeur exactement à tout le bureau Windows, `GetClipCursor` renvoie le même rectangle qu'en mode libre. Cette version ne peut pas distinguer ces deux états.

Cela concerne notamment un jeu en plein écran sur un PC avec **un seul écran physique Windows**. Un Mac connecté par Deskflow ne compte pas comme écran Windows supplémentaire. Le confinement à une fenêtre plus petite ou à un point peut tout de même être détecté.

Sur un PC avec plusieurs écrans Windows, le confinement à l'un d'eux est détectable. Ce comportement conservateur évite de rebloquer les vidéos simplement parce qu'elles sont en plein écran.

## Installation en quelques clics

1. Installer et faire fonctionner Deskflow entre les deux ordinateurs.
2. Ouvrir la page [Releases](https://github.com/frankbrg/deskflow-game-lock/releases/latest).
3. Télécharger **`deskflow-game-lock-v0.1.0-windows-x64.zip`** dans **Assets**. Les archives « Source code » sont destinées au développement.
4. **Extraire tout le ZIP** dans un dossier, puis double-cliquer sur **`Install.cmd`**.
5. Une icône apparaît près de l'horloge Windows ; elle peut se trouver dans les icônes masquées.

L'installation se fait pour l'utilisateur courant dans **`%LOCALAPPDATA%\DeskflowGameLock`**, ajoute deux raccourcis au menu Démarrer et active le lancement à l'ouverture de session. Aucun droit administrateur n'est demandé. Le démarrage automatique peut être décoché dans le menu de l'icône.

Les fichiers `.cmd` lancent les scripts PowerShell locaux fournis dans l'archive. Leur option `ExecutionPolicy Bypass` ne concerne que ce processus et ne modifie pas la stratégie Windows enregistrée. Le binaire n'est pas signé numériquement ; une signature éditeur n'est pas fournie dans cette première version.

### Utilisation portable

Extraire le ZIP dans un dossier où l'utilisateur peut écrire, puis lancer **`DeskflowAutoLock.exe`**. Aucun script d'installation n'est requis. Garder ce dossier à son emplacement si le démarrage Windows est activé depuis l'icône.

## Commandes utiles

| Action | Commande |
| --- | --- |
| Mettre la protection en pause / la reprendre | **Ctrl+Alt+F12** ou double-clic sur l'icône |
| Accéder aux options | Clic droit sur l'icône |
| Déverrouiller manuellement Deskflow | **Arrêt défil / Scroll Lock** |
| Activer ou désactiver le lancement automatique | **Démarrer avec Windows**, dans le menu de l'icône |
| Quitter sans retirer le démarrage automatique | **Quitter** |
| Quitter et retirer le démarrage automatique | **Quitter et désactiver le démarrage** |

Un déverrouillage manuel est respecté jusqu'à la libération de la capture ou au changement de fenêtre. Le programme retire seulement les verrouillages qu'il a lui-même activés.

## Vérifier que ça marche

Dans **Cities: Skylines II**, reprendre une partie puis ouvrir le menu de pause : le verrouillage doit suivre la capture et la libération de la souris. La pause de la **simulation** n'est pas nécessairement la même chose que le menu de pause ; si le jeu reste confiné, la protection reste active.

Une fenêtre de test est aussi incluse dans le programme. Depuis PowerShell, dans le dossier contenant l'exécutable :

```powershell
.\DeskflowAutoLock.exe --test-window
```

- **Espace** : simuler la capture du jeu / sa libération dans un menu.
- **F11** : passer du mode fenêtre au plein écran et inversement.
- **Échap** : fermer la fenêtre de test.

Le plein écran seul ne doit pas verrouiller Deskflow. La capture doit le verrouiller, même dans la petite fenêtre.

## Désinstallation et mises à jour

**Installation classique :** ouvrir **Uninstall Deskflow Game Lock** dans le menu Démarrer, ou lancer `Uninstall.cmd` depuis l'archive. Le programme, ses journaux locaux, ses raccourcis et son entrée de démarrage sont retirés. Les fichiers supplémentaires éventuellement ajoutés au dossier sont conservés. **Deskflow reste installé.**

**Mode portable :** choisir **Quitter et désactiver le démarrage**, puis supprimer le dossier extrait.

**Mise à jour :** extraire la nouvelle archive et relancer `Install.cmd`. L'ancienne instance reçoit une demande d'arrêt propre avant le remplacement du programme. L'installateur réactive le démarrage Windows par défaut.

## Dépannage

| Problème | Vérification |
| --- | --- |
| Rien ne se verrouille | Vérifier le service Deskflow, son journal Info, le raccourci Scroll Lock et la limite « un seul écran ». |
| Protection en pause après une erreur | L'action n'a pas été confirmée par Deskflow en deux secondes. Vérifier le journal et reprendre avec Ctrl+Alt+F12. |
| Un menu reste bloqué | Le jeu peut conserver le confinement dans ce menu. Arrêt défil permet de forcer le déverrouillage. |
| Le raccourci Ctrl+Alt+F12 est utilisé ailleurs | Utiliser le menu de l'icône ; un échec d'enregistrement est indiqué dans `activity.log`. |
| Jeu exécuté en administrateur ou filtrant les entrées synthétiques | Le verrouillage peut échouer ; ce cas n'a pas été validé. |
| Programme arrêté de force, souris toujours verrouillée | Appuyer sur Arrêt défil pour retirer le verrouillage de Deskflow. |

`status.txt` et `activity.log` sont enregistrés à côté du programme. Ils contiennent l'état, les rectangles d'écran et le nom du processus concerné. Ils restent locaux. Avant de joindre un journal à une issue publique, relire son contenu et retirer les informations personnelles éventuelles.

## Fonctionnement et limites

Toutes les 50 ms, le programme lit [`GetClipCursor`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipcursor), compare le rectangle au bureau virtuel Windows et vérifie sa correspondance avec la fenêtre au premier plan. Il utilise ensuite le verrouillage natif de Deskflow via Scroll Lock, et attend sa confirmation dans le journal Deskflow. La libération est temporisée de 150 ms pour éviter les transitions brèves.

- Aucune modification de la configuration Deskflow ou des paramètres du jeu.
- Aucun code injecté dans le jeu, aucune lecture de sa mémoire, aucune capture d'écran et aucun envoi réseau par le programme.
- Un confinement persistant dans un menu reste un confinement : il n'existe pas de détection universelle de la « pause ».
- Les jeux qui recentrent le pointeur sans utiliser `ClipCursor` peuvent ne pas être détectés. La compatibilité avec les systèmes anti-triche n'est pas certifiée.
- **Cities: Skylines II a été vérifié en jeu et dans son menu de pause. Battlefield 6 n'a pas été testé.**

## Compiler et contribuer

Le code est en C#, avec Windows Forms et les API Windows. La compilation utilise le compilateur fourni avec .NET Framework ; pas de package NuGet ni de dépendance à télécharger.

```powershell
git clone https://github.com/frankbrg/deskflow-game-lock.git
cd deskflow-game-lock
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build.ps1 -RunTests -Package
```

Résultats : `bin/DeskflowAutoLock.exe`, archive et empreinte SHA-256 dans `dist/`. Dix contrôles de régression couvrent le confinement fenêtré, les coordonnées négatives, l'absence de confinement, le cas mono-écran et les rectangles invalides. Ils ne remplacent pas des essais avec chaque moteur de jeu.

Pour signaler un problème, indiquer les versions Windows et Deskflow, le nombre d'écrans physiques Windows, le jeu, le mode d'affichage et la différence entre le jeu et son menu de pause.

## Licence

[MIT](LICENSE). Projet indépendant, non affilié à Deskflow ou aux éditeurs des jeux cités. Deskflow n'est pas inclus dans l'archive.
