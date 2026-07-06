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
    public class LinkRvtM : INotifyPropertyChanged
    {
        private string _MainDirectory;
        public string MainDirectory { get { return _MainDirectory; } set { _MainDirectory = value; OnPropertyChanged("MainDirectory"); } }
        // check for ifcs files
        private bool _IsIfcFiles;
        public bool IsIfcFiles { get { return _IsIfcFiles; } set { _IsIfcFiles = value; OnPropertyChanged("IsIfcFiles"); } }

        private bool _IncludeSubdirectories;
        public bool IncludeSubdirectories { get { return _IncludeSubdirectories; } set { _IncludeSubdirectories = value; OnPropertyChanged("IncludeSubdirectories"); } }

        public ObservableCollection<RvtLinks> ListFiles { get; set; } = new ObservableCollection<RvtLinks>();
        public ObservableCollection<DocLinks> DocLinks { get; set; } = new ObservableCollection<DocLinks>();
        public void ReloadFiles()
        {
            if (IsIfcFiles)
            {
                LoadIfcFiles();
            }
            else
            {
                LoadFiles();
            }
        }

        public void LoadIfcFiles()
        {
            if (string.IsNullOrEmpty(MainDirectory)) return;
            var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(MainDirectory, "*.ifc", searchOption);
            ListFiles.Clear();
            foreach (var file in files)
            {
                string filename = Path.GetFileName(file);
                string relativeFolder = GetRelativeFolder(file);
                ListFiles.Add(new RvtLinks
                {
                    FilePath = file,
                    Filename = filename,
                    SubFolder = relativeFolder,
                    Linked = false
                });
            }
        }
        public void LoadFiles()
        {
            if (string.IsNullOrEmpty(MainDirectory)) return;
            var searchOption = IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(MainDirectory, "*.rvt", searchOption);
            ListFiles.Clear();
            foreach (var file in files)
            {
                string filename = Path.GetFileName(file);
                // Exclure les fichiers du type "nom.0001.rvt", "nom.0002.rvt", etc.
                if (System.Text.RegularExpressions.Regex.IsMatch(filename, @"\.\d{4}\.rvt$"))
                    continue;
                // Optionnel : exclure aussi le fichier central actif
                if (filename == LinkRvt.docout?.Title + ".rvt")
                    continue;
                string relativeFolder = GetRelativeFolder(file);
                ListFiles.Add(new RvtLinks
                {
                    FilePath = file,
                    Filename = filename,
                    SubFolder = relativeFolder,
                    Linked = false
                });
            }
        }

        private string GetRelativeFolder(string file)
        {
            if (!IncludeSubdirectories) return "";

            string fileDirectory = Path.GetDirectoryName(file);
            string relativeFolder = Path.GetRelativePath(MainDirectory, fileDirectory);
            return relativeFolder == "." ? "" : relativeFolder;
        }

        public void LinkedRvt(Document doc)
        {
            DocLinks.Clear();
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> linkedInstances = collector.OfClass(typeof(RevitLinkInstance)).ToElements();
            foreach (Element elem in linkedInstances)
            {
                RevitLinkInstance linkInstance = elem as RevitLinkInstance;
                if (linkInstance != null)
                {
                    RevitLinkType linkType = doc.GetElement(linkInstance.GetTypeId()) as RevitLinkType;
                    if (linkType != null)
                    {
                        string linkName = linkType.Name;
                        DocLinks docLink = new DocLinks()
                        {
                            LinkName = linkName,
                            LinkElement = linkType
                        };
                        DocLinks.Add(docLink);
                    }
                }
            }
        }
        public DocLinks AnalyseSimilarity(RvtLinks rvtLink)
        {
            // find similarity between LinkName and Filename troouth ListFiles
            DocLinks result = null;
            double similarity = 0.0;
            foreach (DocLinks docLink in DocLinks)
            {
                double newsimilarity = CalculateSimilarity(docLink.LinkName, rvtLink.Filename);
                // if newsimilarity is up than similarity replace result  and similarity
                if (newsimilarity > similarity && newsimilarity > 0.92)
                {
                    similarity = newsimilarity;
                    result = docLink;
                }
            }
            return result;
        }
        public static double CalculateSimilarity(string s1, string s2)
        {
            if (s1 == s2) return 1.0;
            if (s1.Length == 0 || s2.Length == 0) return 0.0;
            int len1 = s1.Length;
            int len2 = s2.Length;
            int[,] matrix = new int[len1 + 1, len2 + 1];
            for (int i = 0; i <= len1; i++) matrix[i, 0] = i;
            for (int j = 0; j <= len2; j++) matrix[0, j] = j;
            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            int distance = matrix[len1, len2];
            return 1.0 - (double)distance / Math.Max(len1, len2);
        }
        #region Proprite Changed Hendler
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
    public class DocLinks
    {
        public string LinkName { get; set; } = "";
        public RevitLinkType LinkElement { get; set; } = null;
    }
    public class RvtLinks : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _linked = false;
        public bool Linked
        {
            get => _linked;
            set
            {
                _linked = value;
                OnPropertyChanged(nameof(Linked));
            }
        }
        private bool _loaded = false;
        public bool Loaded
        {
            get => _loaded;
            set
            {
                _loaded = value;
                OnPropertyChanged(nameof(Loaded));
            }
        }
        // autres propriétés (simplifiées ici)
        public string FilePath { get; set; } = "";
        public string Filename { get; set; } = "";
        public string SubFolder { get; set; } = "";
        public DocLinks CorrespondLink { get; set; } = null;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
