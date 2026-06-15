# Base de Connaissances - Plugin UpgradeUpdateRvt

Ce document sert de base de connaissances technique et fonctionnelle pour le développement et la maintenance du plugin Revit **UpgradeUpdateRvt**.

---

## 🏛️ 1. Architecture Générale (MVVM + Commandes)

Le plugin utilise le patron de conception **MVVM** (Model-View-ViewModel) couplé au modèle d'extensibilité d'Autodesk Revit.

### Composants structurels
* **External Command (Revit Entrypoints)** : 
  - [Application.cs](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/Application.cs) : Commande Revit principale pour l'upgrade de fichiers.
  - [LinkRvt.cs](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/LinkRvt.cs) : Commande Revit pour la gestion des liaisons.
* **Views (WPF / XAML)** :
  - [ApplicatioUI.xaml](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/ApplicatioUI.xaml) : Définit l'interface d'upgrade en lot, organisée en trois onglets distincts (`Conversion en lot`, `Renommage des fichiers` et `Nettoyage après conversion`) pour une meilleure ergonomie UX.
  - [LinkRvtUI.xaml](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/LinkRvtUI.xaml) : Définit l'interface de gestion des liaisons.
* **ViewModels** :
  - [MainVM.cs](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/ViewModel/MainVM.cs) & [LinkRvtVM.cs](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/ViewModel/LinkRvtVM.cs) exposent les données et relient les commandes aux vues.
* **Models** :
  - [MainM.cs](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/Model/MainM.cs) & [LinkRvtM.cs](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/Model/LinkRvtM.cs) portent la logique métier.
* **Commands (WPF ICommand)** :
  - Déclarées dans le dossier [Command](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/Command) (ex: `ConvertCommand`, `LinkRefreshCommand`, `CleanupCommand`).

---

## ⚙️ 2. Moteur de Mise à Niveau en Lot (Batch Upgrade)

### Fonctionnement
1. **Sélection & Détection** : L'utilisateur sélectionne directement un dossier projet via [BrowseFolderCommand](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/Command/BrowseFolderCommand.cs) à l'aide d'un sélecteur de dossier (`OpenFolderDialog`). Le plugin scanne le dossier et charge les modèles Revit admissibles (les fichiers `.0001.rvt` de sauvegarde automatique sont ignorés).
2. **Renommage temporaire (Préfixe `UPD_`)** : 
   - Pour éviter que Revit ne confonde des fichiers de même nom déjà ouverts en session, le plugin ajoute temporairement un préfixe `UPD_` aux fichiers du disque via `AjouterPrefix`.
   - Ce renommage est encapsulé dans un bloc `try...finally` pour garantir le rétablissement des noms initiaux (`EnleverPrefix`) même en cas de plantage.
3. **Conversion détachée & Structure récursive** :
   - Les fichiers sont ouverts détachés (`DetachFromCentralOption.DetachAndPreserveWorksets`) pour casser les liaisons d'origine avec l'ancien fichier central tout en conservant les sous-projets (Worksets).
   - Les fichiers convertis sont enregistrés via `doc.SaveAs` dans un sous-dossier `Converted` ou directement dans leur **dossier source** si l'option `SaveInSameFolder` est activée. Si le modèle était partagé, il est recréé en tant que nouveau fichier central (`SaveAsCentral = true`).
   - **Option récursive (Sous-dossiers)** : Si `IncludeSubdirectories` est activé, l'arborescence d'origine est lue via `SearchOption.AllDirectories`. Durant la conversion, le plugin calcule le chemin relatif propre à chaque fichier à l'aide de `Path.GetRelativePath` et recrée cette même structure d'arborescence à l'intérieur du dossier `Converted` de sortie pour éviter toute collision de noms de fichiers. Si `SaveInSameFolder` est actif, les fichiers convertis sont directement enregistrés au sein de leurs sous-dossiers sources respectifs.
   - **Gestion de l'écrasement (Évitement de perte de données)** : Si le fichier est enregistré au même emplacement et sous le même nom (écrasement réel), l'ancien fichier temporaire `UPD_...` est supprimé en fin de traitement réussi. En cas d'erreur de conversion, la version d'origine est automatiquement restaurée pour prévenir toute perte de données.
4. **Options de renommage avancées** :
   - Le plugin fournit un outil de renommage flexible pour configurer les noms des fichiers convertis. L'utilisateur peut spécifier :
     * **Préfixe** : Texte ajouté au début du nom de fichier.
     * **Suffixe** : Texte ajouté à la fin du nom de fichier.
     * **Rechercher / Remplacer** : Remplace une chaîne spécifique par une autre dans le nom du fichier.
   - Grâce à l'implémentation de `INotifyPropertyChanged` sur `RvtFiles`, les nouveaux noms sont recalculés et affichés en temps réel dans la colonne `Nouveau nom` du tableau dès que l'utilisateur modifie ses critères.
5. **Gestion silencieuse des événements Revit** :
   - Événement `FailuresProcessing` : Tous les avertissements Revit sont effacés automatiquement (`DeleteAllWarnings()`) et les erreurs résolubles sont validées sans interaction.
   - Événement `DialogBoxShowing` : Ferme automatiquement les boîtes de dialogue et popups Revit via `OverrideResult((int)TaskDialogResult.Close)`.

---

## 🔗 3. Moteur de Gestion des Liaisons (Revit Links & IFC)

### Algorithme d'appariement flou (Fuzzy Matching)
Le plugin compare les liens existants dans le modèle actif avec les fichiers physiques présents dans le répertoire sélectionné en utilisant la **distance de Levenshtein** (`CalculateSimilarity`) :
- Formule de similarité : `1.0 - (distance / Max(Longueur1, Longueur2))`
- Seuil d'appariement automatique : **92%** de similarité.
- Si le seuil est validé, le fichier physique est associé au lien Revit existant sous le nom `CorrespondLink`.

### Processus d'importation
* **Rechargement (Reload)** : Si le lien est déjà présent dans le modèle (`CorrespondLink != null`), le plugin met à jour son chemin source sur le disque en appelant `LinkElement.LoadFrom(path, wsconf)`.
* **Nouvelle Liaison RVT** : Si aucune correspondance n'est trouvée, le plugin crée un nouveau type de lien (`RevitLinkType.Create`) et l'insère dans le modèle (`RevitLinkInstance.Create`), puis aligne son origine sur le point de base du projet hôte (`MoveBasePointToHostBasePoint(true)`).
* **Nouvelle Liaison IFC** : 
  - Si `IsIfcFiles` est activé, le plugin ouvre d'abord le fichier `.ifc` de manière transparente (`OpenIFCDocument`).
  - Il l'enregistre en tant que fichier `.rvt` intermédiaire dans le même répertoire.
  - Il lie enfin ce fichier `.rvt` généré dans le projet.

---

## 📚 4. APIs Revit Clés utilisées

| Classe / Méthode Revit | Rôle dans le plugin |
| :--- | :--- |
| `ModelPathUtils.ConvertUserVisiblePathToModelPath` | Convertit un chemin Windows (string) en `ModelPath` requis par l'API Revit. |
| `OpenOptions.DetachFromCentralOption` | Définit si le fichier partagé doit être détaché de son fichier central d'origine. |
| `SaveAsOptions.SetWorksharingOptions` | Configure le document sauvegardé pour être un nouveau fichier central. |
| `RevitLinkType.Create` | Crée la définition du lien dans la base de données du document. |
| `RevitLinkInstance.Create` | Place une instance géométrique du lien dans le modèle. |
| `RevitLinkInstance.MoveBasePointToHostBasePoint` | Aligne l'origine du lien sur l'origine du projet hôte. |
| `Application.OpenIFCDocument` | Importe et convertit à la volée un fichier IFC en Document Revit. |

---

## ⚠️ 5. Directives de Maintenance du Code

### Gestion des ressources mémoire (Règle d'or Revit)
> [!IMPORTANT]
> Tout document Revit ouvert via l'API (`OpenDocumentFile`, `OpenIFCDocument`) **DOIT** être fermé explicitement via `doc.Close(false)` dans un bloc `finally`. L'absence de bloc `finally` provoque des fuites mémoire et verrouille les fichiers.

### Événement CanExecuteChanged des Commandes
Pour éviter l'avertissement de compilation `CS0067` sur les classes implémentant `ICommand`, utiliser la structure suivante pour `CanExecuteChanged` :
```csharp
public event EventHandler CanExecuteChanged
{
    add { }
    remove { }
}
```
