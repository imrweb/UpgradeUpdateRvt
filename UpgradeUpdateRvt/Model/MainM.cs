using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.VisualBasic;
using ProrsbArs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml.Linq;

namespace UpgradeUpdateRvt.Model
{
    public class MainM : INotifyPropertyChanged
    {
        private bool _CanConvert;    
        public bool CanConvert { get { return _CanConvert; } set { _CanConvert = value; OnPropertyChanged("CanConvert"); } }

        private string _Prefix = "";
        public string Prefix 
        { 
            get { return _Prefix; } 
            set 
            { 
                _Prefix = value; 
                OnPropertyChanged("Prefix"); 
                RenameFile(); 
            } 
        }

        private string _Suffix = "";
        public string Suffix 
        { 
            get { return _Suffix; } 
            set 
            { 
                _Suffix = value; 
                OnPropertyChanged("Suffix"); 
                RenameFile(); 
            } 
        }

        private string _FindText = "";
        public string FindText 
        { 
            get { return _FindText; } 
            set 
            { 
                _FindText = value; 
                OnPropertyChanged("FindText"); 
                RenameFile(); 
            } 
        }

        private string _ReplaceText = "";
        public string ReplaceText 
        { 
            get { return _ReplaceText; } 
            set 
            { 
                _ReplaceText = value; 
                OnPropertyChanged("ReplaceText"); 
                RenameFile(); 
            } 
        }

        private string _MainPath;
        public string MainPath { get { return _MainPath; } set { _MainPath = value;  OnPropertyChanged("MainPath"); }  }

        private string _DirectoryPath;
        public string DirectoryPath { get { return _DirectoryPath; } set { _DirectoryPath = value; OnPropertyChanged("DirectoryPath"); } }


        private string _FileNameOnly;
        public string FileNameOnly   { get { return _FileNameOnly; } set { _FileNameOnly = value; OnPropertyChanged("FileNameOnly"); } }

        private string _PathUpToDirectory;

        public string PathUpToDirectory { get { return _PathUpToDirectory; } set { _PathUpToDirectory = value; OnPropertyChanged("PathUpToDirectory"); } }

        private string _PathConvertedDirectory;
        public string PathConvertedDirectory { get { return _PathConvertedDirectory; } set { _PathConvertedDirectory = value; OnPropertyChanged("PathConvertedDirectory"); } }

        private bool _IncludeSubdirectories;
        public bool IncludeSubdirectories { get { return _IncludeSubdirectories; } set { _IncludeSubdirectories = value; OnPropertyChanged("IncludeSubdirectories"); } }

        private bool _SaveInSameFolder;
        public bool SaveInSameFolder { get { return _SaveInSameFolder; } set { _SaveInSameFolder = value; OnPropertyChanged("SaveInSameFolder"); } }

        private bool _EnableCleanup;
        public bool EnableCleanup { get { return _EnableCleanup; } set { _EnableCleanup = value; OnPropertyChanged("EnableCleanup"); } }

        private string _CleanupPatterns = "Revit_temp, *R26*";
        public string CleanupPatterns { get { return _CleanupPatterns; } set { _CleanupPatterns = value; OnPropertyChanged("CleanupPatterns"); } }

        public ObservableCollection<RvtFiles> ListFiles { get; set; } = new ObservableCollection<RvtFiles>();
        public ObservableCollection<CleanupItem> CleanupList { get; set; } = new ObservableCollection<CleanupItem>();
        #region Proprite Changed Hendler
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }


        public void LoadFiles()
        {
            if (!string.IsNullOrEmpty(DirectoryPath))
            {
                var searchOption = IncludeSubdirectories ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;
                var files = System.IO.Directory.GetFiles(DirectoryPath, "*.rvt", searchOption);
                ListFiles.Clear();
                foreach (var file in files)
                {
                    string filename = System.IO.Path.GetFileName(file);
                    // Exclure les fichiers du type "nom.0001.rvt", "nom.0002.rvt", etc.
                    if (System.Text.RegularExpressions.Regex.IsMatch(filename, @"\.\d{4}\.rvt$"))
                        continue;

                    string relativeFolder = "";
                    if (IncludeSubdirectories)
                    {
                        string fileDir = Path.GetDirectoryName(file);
                        relativeFolder = Path.GetRelativePath(DirectoryPath, fileDir);
                        if (relativeFolder == ".")
                        {
                            relativeFolder = "";
                        }
                    }

                    RvtFiles rvtFile = new RvtFiles()
                    {
                        FilePath = file,
                        OriginFilename = filename,
                        SubFolder = relativeFolder,
                        NewFileName = "",
                        Workshared = false,
                        Converted = false,
                        Status = "En attente"
                    };
                    ListFiles.Add(rvtFile);
                }
            }
        }

        #endregion

        // rename fole batch add prefix UPD_ function
        public void AjouterPrefix(string prefix)
        {
            foreach (RvtFiles fichier in this.ListFiles)
            {
                string nomActuel = Path.GetFileName(fichier.FilePath);

                // Si le préfixe existe déjà → on skip mais on renseigne TempFilename
                if (nomActuel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    fichier.TempFilename = fichier.FilePath;
                    fichier.Status = "Renommage ignoré (déjà préfixé)";
                    continue;
                }

                string dossierFichier = Path.GetDirectoryName(fichier.FilePath);
                string nouveauNom = Path.Combine(dossierFichier, prefix + nomActuel);
                File.Move(fichier.FilePath, nouveauNom);
                fichier.TempFilename = nouveauNom;
                fichier.Status = "Préparé (renommé temporairement)";
            }
        }

        public void EnleverPrefix(string prefix)
        {
            // 1. Gérer les fichiers de notre liste de manière ciblée
            foreach (RvtFiles fichier in this.ListFiles)
            {
                if (string.IsNullOrEmpty(fichier.TempFilename) || !File.Exists(fichier.TempFilename))
                    continue;

                string dossierFichier = Path.GetDirectoryName(fichier.FilePath);
                string originalPath = fichier.FilePath;

                // Si on enregistre dans le même dossier
                if (SaveInSameFolder)
                {
                    string targetPath = Path.Combine(dossierFichier, fichier.NewFileName);

                    // Si la conversion a réussi
                    if (fichier.Converted)
                    {
                        // Si le nouveau nom est identique au nom d'origine (écrasement réel)
                        if (string.Equals(fichier.NewFileName, fichier.OriginFilename, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                // On garde le fichier converti et on supprime le backup temporaire (UPD_...)
                                if (File.Exists(fichier.TempFilename))
                                {
                                    File.Delete(fichier.TempFilename);
                                }
                            }
                            catch
                            {
                                // Ignorer les erreurs d'accès fichier
                            }
                            continue;
                        }
                    }
                }

                // Dans tous les autres cas (échec conversion, ou dossier Converted séparé, ou nom différent) :
                // On restaure le fichier d'origine à partir du fichier temporaire si le fichier temporaire existe
                try
                {
                    if (File.Exists(originalPath))
                    {
                        File.Delete(originalPath);
                    }
                    File.Move(fichier.TempFilename, originalPath);
                }
                catch
                {
                    // Ignorer les erreurs de restauration pour continuer
                }
            }

            // 2. Nettoyage général de secours sur le disque (au cas où il reste des fichiers orphelins)
            if (string.IsNullOrEmpty(_DirectoryPath) || !Directory.Exists(_DirectoryPath)) return;
            var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(_DirectoryPath, prefix + "*.rvt", searchOption);

            foreach (string filePath in files)
            {
                string nom = Path.GetFileName(filePath);
                try
                {
                    string dossierFichier = Path.GetDirectoryName(filePath);
                    string nouveauNom = Path.Combine(dossierFichier, nom.Substring(prefix.Length));
                    if (!File.Exists(nouveauNom))
                    {
                        File.Move(filePath, nouveauNom);
                    }
                }
                catch
                {
                    // Ignorer
                }
            }
        }


        public void RenameFile()
        {
            foreach (RvtFiles fichier in this.ListFiles)
            {
                string nomActuel = Path.GetFileNameWithoutExtension(fichier.FilePath);
                string extension = Path.GetExtension(fichier.FilePath);

                // 1. Appliquer Recherche & Remplacer si défini
                if (!string.IsNullOrEmpty(FindText))
                {
                    nomActuel = nomActuel.Replace(FindText, ReplaceText ?? "");
                }

                // 2. Appliquer Préfixe et Suffixe
                string nouveauNom = (Prefix ?? "") + nomActuel + (Suffix ?? "") + extension;
                fichier.NewFileName = nouveauNom;
            }
        }
        public void UpgradeUpdateRvt()
        {
            ProgressBar prgs = new ProgressBar();
            string FileName = "Title";
            StringBuilder faillureelem = new StringBuilder();
            StringBuilder render = new StringBuilder();
            int i = 0;
            int iP = 0;
            int n = 0;
            prgs.Run(false, FileName);
            prgs.MessageText(false, "Please wait ..");
            foreach (RvtFiles fichier in this.ListFiles)
            {
                fichier.Status = "En cours...";
                string targetDir = Path.GetDirectoryName(fichier.FilePath);
                if (!SaveInSameFolder)
                {
                    targetDir = this.PathConvertedDirectory;
                    if (IncludeSubdirectories)
                    {
                        string relativeDir = Path.GetRelativePath(this.DirectoryPath, Path.GetDirectoryName(fichier.FilePath));
                        if (relativeDir != ".")
                        {
                            targetDir = Path.Combine(this.PathConvertedDirectory, relativeDir);
                        }
                    }
                }
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string savePath = Path.Combine(targetDir, fichier.NewFileName);
                // if file exist skip
                if (File.Exists(savePath))
                {
                    fichier.Status = "Déjà converti (passé)";
                    i++;
                    iP++;
                    prgs.Chanage1(iP, this.ListFiles.Count, string.Format("Upgrad: {0} / Not Upgrad: {1} From ({2})", i, n, this.ListFiles.Count));
                    continue;
                }


                Document doc = null;
                try
                {
                    string filePath = string.IsNullOrEmpty(fichier.TempFilename) ? fichier.FilePath : fichier.TempFilename;
                    ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
                    OpenOptions openOpts = new OpenOptions { DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets };

                    doc = Application.uiAppout.Application.OpenDocumentFile(modelPath, openOpts);
                    fichier.DocRvt = doc;
                    fichier.Workshared = doc.IsWorkshared;
                    SaveAsOptions saveOpts = new SaveAsOptions { OverwriteExistingFile = true };
                    if (doc.IsWorkshared)
                    {
                        saveOpts.SetWorksharingOptions(new WorksharingSaveAsOptions { SaveAsCentral = true });
                    }
                    doc.SaveAs(savePath, saveOpts);
                    fichier.Converted = true;
                    fichier.Status = "Succès";
                    i++;
                }
                catch (Exception ex)
                {
                    faillureelem.Append("\n" + fichier.FilePath + ":" +  ex.Message + "\n");
                    fichier.Status = "Erreur : " + ex.Message;
                    n++;
                }
                finally
                {
                    if (doc != null)
                    {
                        try
                        {
                            doc.Close(false);
                        }
                        catch
                        {
                            // Ignorer en cas d'erreur de fermeture
                        }
                    }
                }
                iP++;
                prgs.Chanage1(iP, this.ListFiles.Count, string.Format("Upgrad: {0} / Not Upgrad: {1} From ({2})", i, n, this.ListFiles.Count));
                if (prgs.Cancellation()) 
                {
                    fichier.Status = "Annulé";
                    break; 
                }
            }

            // Appel de la méthode de nettoyage
            NettoyerDossiersEtFichiers();

            prgs.MessageText(true, "\n Revit Regenerating ...");
            render.AppendLine("Number of File Upgraded: " + i.ToString());
            render.AppendLine("Number of File Not Upgraded: " + n.ToString());
            if (faillureelem.Length > 0)
            {
                render.AppendLine("Elements Not Upgraded: " + faillureelem.ToString());
            }
            // dialog 
            prgs.MessageText(false, render.ToString());
            prgs.Finish();

        }

        private bool NameMatchesPattern(string name, string pattern)
        {
            pattern = pattern.Trim();
            if (string.IsNullOrEmpty(pattern)) return false;

            if (pattern.StartsWith("*") && pattern.EndsWith("*") && pattern.Length > 2)
            {
                string sub = pattern.Substring(1, pattern.Length - 2);
                return name.Contains(sub, StringComparison.OrdinalIgnoreCase);
            }
            else if (pattern.StartsWith("*") && pattern.Length > 1)
            {
                string sub = pattern.Substring(1);
                return name.EndsWith(sub, StringComparison.OrdinalIgnoreCase);
            }
            else if (pattern.EndsWith("*") && pattern.Length > 1)
            {
                string sub = pattern.Substring(0, pattern.Length - 1);
                return name.StartsWith(sub, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase) || 
                       name.Contains(pattern, StringComparison.OrdinalIgnoreCase);
            }
        }

        public void NettoyerDossiersEtFichiers()
        {
            if (!EnableCleanup || string.IsNullOrEmpty(CleanupPatterns)) return;

            string[] patterns = CleanupPatterns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (patterns.Length == 0) return;

            var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 1. Scanner et supprimer les dossiers correspondants
            if (Directory.Exists(DirectoryPath))
            {
                string[] subdirs = Directory.GetDirectories(DirectoryPath, "*", searchOption);
                
                // Trier par longueur décroissante pour supprimer d'abord les sous-dossiers les plus profonds
                var sortedDirs = subdirs.OrderByDescending(d => d.Length).ToList();

                foreach (string dirPath in sortedDirs)
                {
                    string dirName = Path.GetFileName(dirPath);
                    
                    // Éviter de supprimer le dossier Converted lui-même !
                    if (string.Equals(dirName, "Converted", StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool shouldDelete = false;
                    foreach (string pat in patterns)
                    {
                        if (NameMatchesPattern(dirName, pat))
                        {
                            shouldDelete = true;
                            break;
                        }
                    }

                    if (shouldDelete && Directory.Exists(dirPath))
                    {
                        try
                        {
                            Directory.Delete(dirPath, true);
                        }
                        catch
                        {
                            // Ignorer en cas de verrou
                        }
                    }
                }
            }

            // 2. Scanner et supprimer les fichiers correspondants
            if (Directory.Exists(DirectoryPath))
            {
                string[] files = Directory.GetFiles(DirectoryPath, "*.*", searchOption);

                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);

                    // Ne surtout pas supprimer nos nouveaux fichiers convertis !
                    bool isNewConvertedFile = false;
                    foreach (RvtFiles fichier in this.ListFiles)
                    {
                        if (fichier.Converted)
                        {
                            string targetPath = Path.Combine(Path.GetDirectoryName(fichier.FilePath), fichier.NewFileName);
                            if (!SaveInSameFolder)
                            {
                                string targetDir = this.PathConvertedDirectory;
                                if (IncludeSubdirectories)
                                {
                                    string relativeDir = Path.GetRelativePath(this.DirectoryPath, Path.GetDirectoryName(fichier.FilePath));
                                    if (relativeDir != ".")
                                    {
                                        targetDir = Path.Combine(this.PathConvertedDirectory, relativeDir);
                                    }
                                }
                                targetPath = Path.Combine(targetDir, fichier.NewFileName);
                            }

                            if (string.Equals(filePath, targetPath, StringComparison.OrdinalIgnoreCase))
                            {
                                isNewConvertedFile = true;
                                break;
                            }
                        }
                    }

                    if (isNewConvertedFile)
                        continue;

                    bool shouldDelete = false;
                    foreach (string pat in patterns)
                    {
                        if (NameMatchesPattern(fileName, pat))
                        {
                            shouldDelete = true;
                            break;
                        }
                    }

                    if (shouldDelete && File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch
                        {
                            // Ignorer en cas de verrou
                        }
                    }
                }
            }
        }

        public void AnalyserDossierPourNettoyage()
        {
            CleanupList.Clear();
            if (string.IsNullOrEmpty(DirectoryPath) || !Directory.Exists(DirectoryPath) || string.IsNullOrEmpty(CleanupPatterns)) return;

            string[] patterns = CleanupPatterns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (patterns.Length == 0) return;

            var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 1. Scanner les dossiers correspondants
            string[] subdirs = Directory.GetDirectories(DirectoryPath, "*", searchOption);
            var sortedDirs = subdirs.OrderBy(d => d.Length).ToList();

            foreach (string dirPath in sortedDirs)
            {
                string dirName = Path.GetFileName(dirPath);
                
                if (string.Equals(dirName, "Converted", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (string pat in patterns)
                {
                    if (NameMatchesPattern(dirName, pat))
                    {
                        string relative = Path.GetRelativePath(DirectoryPath, dirPath);
                        if (relative == ".") relative = "";

                        CleanupList.Add(new CleanupItem
                        {
                            IsSelected = true,
                            Name = dirName,
                            Type = "Dossier",
                            Path = dirPath,
                            RelativePath = relative,
                            Pattern = pat.Trim()
                        });
                        break;
                    }
                }
            }

            // 2. Scanner les fichiers correspondants
            string[] files = Directory.GetFiles(DirectoryPath, "*.*", searchOption);

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);

                // Sécurité : Ne pas lister nos nouveaux fichiers convertis ou temporaires
                bool isNewConvertedFile = false;
                foreach (RvtFiles fichier in this.ListFiles)
                {
                    if (fichier.Converted)
                    {
                        string targetPath = Path.Combine(Path.GetDirectoryName(fichier.FilePath), fichier.NewFileName);
                        if (!SaveInSameFolder)
                        {
                            string targetDir = this.PathConvertedDirectory;
                            if (IncludeSubdirectories)
                            {
                                string relativeDir = Path.GetRelativePath(this.DirectoryPath, Path.GetDirectoryName(fichier.FilePath));
                                if (relativeDir != ".")
                                {
                                    targetDir = Path.Combine(this.PathConvertedDirectory, relativeDir);
                                }
                            }
                            targetPath = Path.Combine(targetDir, fichier.NewFileName);
                        }

                        if (string.Equals(filePath, targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            isNewConvertedFile = true;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(fichier.TempFilename) && string.Equals(filePath, fichier.TempFilename, StringComparison.OrdinalIgnoreCase))
                    {
                        isNewConvertedFile = true;
                        break;
                    }
                }

                if (isNewConvertedFile)
                    continue;

                foreach (string pat in patterns)
                {
                    if (NameMatchesPattern(fileName, pat))
                    {
                        string relative = Path.GetRelativePath(DirectoryPath, filePath);

                        CleanupList.Add(new CleanupItem
                        {
                            IsSelected = true,
                            Name = fileName,
                            Type = "Fichier",
                            Path = filePath,
                            RelativePath = relative,
                            Pattern = pat.Trim()
                        });
                        break;
                    }
                }
            }
        }

        public void ExecuterNettoyageSelectionne()
        {
            var itemsToDelete = CleanupList.Where(item => item.IsSelected).ToList();
            if (itemsToDelete.Count == 0) return;

            ProgressBar prgs = new ProgressBar();
            prgs.Run(false, "Nettoyage");
            prgs.MessageText(false, "Suppression en cours...");

            int total = itemsToDelete.Count;
            int current = 0;
            int deletedFiles = 0;
            int deletedDirs = 0;
            int failed = 0;

            // 1. Supprimer d'abord les fichiers
            var filesToDelete = itemsToDelete.Where(item => item.Type == "Fichier").ToList();
            foreach (var file in filesToDelete)
            {
                if (prgs.Cancellation()) break;

                if (File.Exists(file.Path))
                {
                    try
                    {
                        File.Delete(file.Path);
                        deletedFiles++;
                    }
                    catch
                    {
                        failed++;
                    }
                }
                else
                {
                    deletedFiles++;
                }

                current++;
                prgs.Chanage1(current, total, $"Fichiers : {deletedFiles} | Dossiers : {deletedDirs} | Échecs : {failed} ({current}/{total})");
            }

            // 2. Supprimer les dossiers (les plus profonds d'abord)
            var dirsToDelete = itemsToDelete.Where(item => item.Type == "Dossier")
                                            .OrderByDescending(d => d.Path.Length)
                                            .ToList();
            foreach (var dir in dirsToDelete)
            {
                if (prgs.Cancellation()) break;

                if (Directory.Exists(dir.Path))
                {
                    try
                    {
                        Directory.Delete(dir.Path, true);
                        deletedDirs++;
                    }
                    catch
                    {
                        failed++;
                    }
                }
                else
                {
                    deletedDirs++;
                }

                current++;
                prgs.Chanage1(current, total, $"Fichiers : {deletedFiles} | Dossiers : {deletedDirs} | Échecs : {failed} ({current}/{total})");
            }

            prgs.Finish();

            // Rafraîchir l'analyse après suppression
            AnalyserDossierPourNettoyage();
        }

    }

    public class RvtFiles : INotifyPropertyChanged
    {
        private Document _docRvt;
        public Document DocRvt { get { return _docRvt; } set { _docRvt = value; OnPropertyChanged("DocRvt"); } }

        private string _originFilename = "";
        public string OriginFilename { get { return _originFilename; } set { _originFilename = value; OnPropertyChanged("OriginFilename"); } }

        private string _subFolder = "";
        public string SubFolder { get { return _subFolder; } set { _subFolder = value; OnPropertyChanged("SubFolder"); } }

        private string _tempFilename = "";
        public string TempFilename { get { return _tempFilename; } set { _tempFilename = value; OnPropertyChanged("TempFilename"); } }

        private string _newFileName = "";
        public string NewFileName { get { return _newFileName; } set { _newFileName = value; OnPropertyChanged("NewFileName"); } }

        private string _filePath = "";
        public string FilePath { get { return _filePath; } set { _filePath = value; OnPropertyChanged("FilePath"); } }

        private bool _workshared = false;
        public bool Workshared { get { return _workshared; } set { _workshared = value; OnPropertyChanged("Workshared"); } }

        private bool _converted = false;
        public bool Converted { get { return _converted; } set { _converted = value; OnPropertyChanged("Converted"); } }

        private string _status = "En attente";
        public string Status { get { return _status; } set { _status = value; OnPropertyChanged("Status"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CleanupItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged("IsSelected"); } }

        private string _name = "";
        public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }

        private string _type = "";
        public string Type { get { return _type; } set { _type = value; OnPropertyChanged("Type"); } }

        private string _path = "";
        public string Path { get { return _path; } set { _path = value; OnPropertyChanged("Path"); } }

        private string _relativePath = "";
        public string RelativePath { get { return _relativePath; } set { _relativePath = value; OnPropertyChanged("RelativePath"); } }

        private string _pattern = "";
        public string Pattern { get { return _pattern; } set { _pattern = value; OnPropertyChanged("Pattern"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
