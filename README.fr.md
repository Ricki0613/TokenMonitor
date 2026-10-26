<p align="center"><img src="assets/TokenMonitor.png" width="72" alt="Token Monitor"></p>
<h1 align="center">Token Monitor</h1>
<p align="center"><strong>Quotas, soldes et utilisation de l’API en un coup d’œil.</strong><br>Un outil de suivi pour Windows compatible avec Codex / ChatGPT Work, DeepSeek et OpenAI API.</p>
<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4" alt="Windows 10 / 11 x64">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/latest"><img src="https://img.shields.io/badge/version-3.2.1-10A37F" alt="Version 3.2.1"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"></a>
</p>
<p align="center"><a href="https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip"><strong>Télécharger pour Windows</strong></a> · <a href="#démarrage-rapide">Démarrage rapide</a> · <a href="docs/USER_GUIDE.zh-CN.md">Guide d’utilisation (chinois)</a></p>

<p align="center">
  <a href="README.md">简体中文</a> · <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.de.md">Deutsch</a> · <a href="README.ru.md">Русский</a>
</p>

[![Token Monitor](assets/readme-hero.png)](assets/readme-hero.png)

<p align="center"><sub>Tableau de bord et widget flottant · Données de démonstration · Cliquez pour afficher l’image en haute résolution</sub></p>

## Vos données à portée de vue

Suivez trois services dans un tableau de bord pendant que vous programmez ou utilisez des modèles, et gardez les données utiles visibles sur le bureau.

- **Widget flottant compact** : deux pourcentages indiquent les quotas restants sur 5 heures et sur une semaine. Déplacez-le, double-cliquez pour ouvrir les détails et choisissez parmi cinq tailles.
- **Fenêtres flottantes indépendantes** : affichez, masquez, déplacez et redimensionnez chaque service. Les services connectés continuent à s’actualiser lorsque leurs fenêtres sont masquées.
- **Historique local** : graphiques sur 7 ou 30 jours, conservés pendant 90 jours. Les observations manquantes restent absentes.
- **Affichage personnalisable** : thème clair, sombre ou système, trois tailles de texte et navigation latérale permanente.
- **Mises à jour et retour arrière** : téléchargez et vérifiez le paquet dans l’application, confirmez le redémarrage et conservez la version précédente.

## Données prises en charge

| Service | Données affichées | Connexion |
| --- | --- | --- |
| **ChatGPT / Codex** | Quota restant commun à Codex / ChatGPT Work, heures de réinitialisation et crédits de réinitialisation disponibles | Codex déjà connecté sur cet ordinateur |
| **DeepSeek** | Solde disponible, solde rechargé et solde offert, par devise | API Key ou session locale DeepSeek Harness |
| **OpenAI API** | Tokens du jour et du mois, entrées, sorties, entrées en cache et nombre de requêtes pour un API Key ID | API Key ordinaire ; l’historique exige également une Admin API Key et l’identifiant correspondant |

Les données ChatGPT concernent le quota Codex / ChatGPT Work, pas toutes les limites de messages de ChatGPT. Les statistiques OpenAI proviennent de l’endpoint officiel `usage/completions` ; les entrées en cache font déjà partie des tokens d’entrée.

## Démarrage rapide

**Configuration requise : Windows 10 / 11 x64 et .NET Framework 4.8. L’interface de l’application est actuellement en chinois.** Python, Node et le SDK .NET ne sont pas nécessaires pour l’utiliser.

1. [Téléchargez l’archive Windows](https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip) et extrayez-la entièrement. Choisissez l’archive exécutable plutôt que le fichier `Source code` généré par GitHub.
2. Lancez **Install.cmd** et choisissez les dossiers du programme et des données. L’installateur crée des raccourcis sur le bureau et dans le menu Démarrer. Vous pouvez aussi lancer directement `TokenMonitor.exe` après extraction.
3. Dans chaque carte, ouvrez **连接设置** (paramètres de connexion), connectez les services souhaités, puis activez leurs fenêtres ou le widget.

L’actualisation automatique est réglée sur **30 secondes** par défaut. Choisissez 15 / 30 / 60 / 120 secondes ou actualisez manuellement depuis le tableau de bord. Fermer la fenêtre principale réduit l’application dans la zone de notification ; double-cliquez sur son icône pour la retrouver ou utilisez son menu pour quitter complètement.

[Installation et dépannage](docs/START_HERE.zh-CN.md) · [Guide complet](docs/USER_GUIDE.zh-CN.md) (en chinois)

### DeepSeek : API Key ou Harness

Saisissez une API Key dans **DeepSeek 密钥**, ou connectez-vous à Harness avec le même utilisateur Windows et sélectionnez **DeepSeek 密钥 → DeepSeek 余额来源 → DeepSeek Harness 登录账户**. Enregistrez votre choix ; Harness n’a pas besoin de rester ouvert.

Le mode Harness regroupe les portefeuilles rechargés et offerts par devise. Il lit les identifiants à leur emplacement local par défaut lorsque nécessaire, sans les copier dans Token Monitor. Vous pouvez revenir au mode API Key. Les dossiers Harness personnalisés et les serveurs non officiels ne sont pas pris en charge. Consultez les [règles de gestion des identifiants](SECURITY.md).

### OpenAI : vérifier la clé et accéder à l’historique

Vérifiez d’abord votre API Key ordinaire, puis renseignez l’**Admin API Key** de l’organisation et l’**API Key ID** correspondant (`key_…`). La validation de la clé ordinaire ne donne pas à elle seule accès à l’historique. Si la configuration est incomplète, l’application indique l’étape suivante au lieu d’afficher une utilisation nulle.

## Historique, mises à jour et données locales

### Tendances sur 7 et 30 jours

Les graphiques utilisent l’heure de Pékin (UTC+8). ChatGPT et DeepSeek conservent la dernière mesure réussie de chaque période de 15 minutes. L’historique dépend de la source locale : changer de compte modifie la courbe.

Les totaux quotidiens OpenAI sont conservés séparément selon une empreinte du Key ID. Chaque relevé corrige les valeurs récupérées pour le mois en cours ; pour les mois précédents, seules les valeurs déjà collectées sont conservées. Le total du jour est provisoire.

**Une variation du solde n’est pas une dépense.** Les rechargements, crédits offerts et remboursements modifient aussi le solde DeepSeek.

### Mises à jour dans l’application

Dans **关于软件** (à propos), vérifiez les mises à jour, lisez les nouveautés et téléchargez le paquet. L’application vérifie SHA-256 et la version de l’exécutable avant de demander confirmation pour redémarrer et installer. Paramètres, identifiants et historique sont conservés ; la version précédente peut être restaurée depuis la même page.

Les versions stables sont vérifiées au démarrage, puis toutes les six heures après une vérification réussie. Les problèmes réseau et les limitations de GitHub n’interrompent pas le suivi ; un lien de téléchargement manuel reste disponible. Depuis la version v3.1.0 ou antérieure, la première mise à niveau doit être installée manuellement.

[Versions publiées](https://github.com/Ricki0613/TokenMonitor/releases) · [Journal des modifications](CHANGELOG.md)

### Données conservées sur l’ordinateur

L’utilisation courante et l’enregistrement des clés ne nécessitent pas de droits administrateur. Le dossier par défaut est `%LOCALAPPDATA%\TokenMonitor`. Dans **数据目录**, choisissez un dossier vide accessible en écriture pour copier et vérifier paramètres, identifiants et historique ; le dossier d’origine reste une sauvegarde.

- Les clés DeepSeek et OpenAI sont chiffrées avec Windows DPAPI pour l’utilisateur actuel.
- Les identifiants Harness sont lus à la demande et transmis uniquement au service officiel DeepSeek.
- Les relevés d’utilisation ne lancent pas de requêtes d’inférence. La réinitialisation officielle d’un quota demande une confirmation distincte.
- La recherche de mises à jour n’envoie ni paramètres locaux, ni identifiants, ni données d’utilisation du compte à GitHub.

Consultez [SECURITY.md](SECURITY.md) pour plus de détails. Les statistiques peuvent être retardées par les fournisseurs. Le démarrage automatique n’est pas configuré et les exécutables publiés ne sont pas encore signés.

## Développement et contributions

Les [signalements et suggestions](https://github.com/Ricki0613/TokenMonitor/issues/new/choose) ainsi que les pull requests sont les bienvenus. Évitez toute donnée de compte ou clé réelle dans les captures, journaux et commits. Voir [CONTRIBUTING.md](CONTRIBUTING.md).

<details>
<summary>Compiler et vérifier depuis les sources</summary>

```powershell
git clone https://github.com/Ricki0613/TokenMonitor.git
cd TokenMonitor
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

Les fichiers produits se trouvent dans `build/`. La compilation utilise le compilateur C# de .NET Framework. Les tests hors ligne emploient des données de démonstration, sans compte ni clé réelle. Créez l’archive avec `scripts/package.ps1` et vérifiez l’installation avec `scripts/test-package.ps1 -Archive <ZIP>`.

L’image du README est produite à résolution double à partir des contrôles WPF natifs et de données de démonstration avec `scripts/render-readme.ps1`. Pour déclencher les notifications, publiez une version stable contenant l’archive exécutable et marquez-la Latest ; un tag Git seul ne suffit pas.

</details>

## Licence

[MIT License](LICENSE) · Copyright (c) 2026 Ricki0613

Projet indépendant, sans affiliation officielle avec OpenAI ou DeepSeek.
