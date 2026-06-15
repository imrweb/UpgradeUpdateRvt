# Plan d'implémentation - Option de conversion récursive (sous-dossiers)

Ce plan décrit les modifications à apporter pour permettre la mise à niveau (upgrade) des maquettes Revit situées dans des sous-dossiers, tout en conservant la structure d'arborescence d'origine dans le dossier de sortie `Converted`.

---

## 🛠️ 1. Modifications de l'Interface Graphique (Vue)

### Fichier : [ApplicatioUI.xaml](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/ApplicatioUI.xaml)
* Ajouter une case à cocher (CheckBox) à côté du champ préfixe/chemin pour permettre d'activer ou de désactiver la recherche dans les sous-dossiers.
* **Code XAML proposé** :
```xml
<CheckBox Content="Inclure les sous-dossiers" 
          IsChecked="{Binding Init.IncludeSubdirectories, Mode=TwoWay}"
          VerticalAlignment="Center" 
          Margin="12,0,0,0"/>
```

---

## 💾 2. Modifications du Modèle de Données (Model)

### Fichier : [MainM.cs](file:///E:/dev/update%20_link_revits/UpgradeUpdateRvt/UpgradeUpdateRvt/Model/MainM.cs)

#### A. Ajouter la propriété de configuration
* Ajouter une propriété booléenne `IncludeSubdirectories` pour stocker le choix de l'utilisateur :
```csharp
private bool _IncludeSubdirectories;
public bool IncludeSubdirectories 
{ 
    get => _IncludeSubdirectories; 
    set { _IncludeSubdirectories = value; OnPropertyChanged("IncludeSubdirectories"); } 
}
```

#### B. Adapter la recherche de fichiers (`LoadFiles`)
* Modifier `LoadFiles` pour utiliser `SearchOption.AllDirectories` lorsque l'option est cochée :
```csharp
var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
var files = System.IO.Directory.GetFiles(DirectoryPath, "*.rvt", searchOption);
```

#### C. Adapter le renommage temporaire (`AjouterPrefix` & `EnleverPrefix`)
* Actuellement, `AjouterPrefix` déplace les fichiers dans `this._DirectoryPath` (le dossier racine). Si le fichier provient d'un sous-dossier, cela va le déplacer hors de son sous-dossier.
* **Correction** : Identifier le sous-dossier propre à chaque fichier pour le renommer sur place :
```csharp
// Dans AjouterPrefix
string dossierFichier = Path.GetDirectoryName(fichier.FilePath);
string nouveauNom = Path.Combine(dossierFichier, prefix + nomActuel);
```
* **Dans `EnleverPrefix`** : Adapter la recherche des fichiers à renommer pour qu'elle soit également récursive si l'option est cochée :
```csharp
var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
var files = Directory.GetFiles(_DirectoryPath, "*.rvt", searchOption);
```

#### D. Préserver la structure des sous-dossiers dans `Converted` (`UpgradeUpdateRvt`)
* Pour éviter que des fichiers de même nom situés dans des sous-dossiers différents ne s'écrasent les uns les autres dans le dossier `Converted`, nous devons recréer l'arborescence relative.
* **Code proposé dans `UpgradeUpdateRvt`** :
```csharp
// Calculer le chemin relatif du sous-dossier par rapport au dossier racine scanné
string relativeDir = Path.GetRelativePath(this.DirectoryPath, Path.GetDirectoryName(fichier.FilePath));
// Déterminer le dossier cible dans Converted
string targetDir = Path.Combine(this.PathConvertedDirectory, relativeDir);

// Créer le sous-dossier s'il n'existe pas
if (!Directory.Exists(targetDir))
{
    Directory.CreateDirectory(targetDir);
}

string savePath = Path.Combine(targetDir, fichier.NewFileName);
```

---

## 📈 3. Validation & Tests

1. **Compilation** : Lancer un `dotnet build` pour s'assurer de l'absence d'erreurs de syntaxe.
2. **Test unitaire fonctionnel** :
   - Créer une structure de dossiers de test :
     - `BIM\` (Dossier racine)
       - `modelle_racine.rvt`
       - `006_ARC_RESIDENCE_CIRTA_BLOC-A\` (Sous-dossier)
         - `modelle_blocA.rvt`
   - Lancer l'outil avec l'option cochée.
   - Vérifier que la structure de sortie est :
     - `Converted\modelle_racine_UPD.rvt`
     - `Converted\006_ARC_RESIDENCE_CIRTA_BLOC-A\modelle_blocA_UPD.rvt`
