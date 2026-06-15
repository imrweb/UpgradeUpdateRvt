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

        private string _Prefix;
        public string Prefix { get { return _Prefix; } set { _Prefix = value; OnPropertyChanged("Prefix"); } }

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

        public ObservableCollection<RvtFiles> ListFiles { get; set; } = new ObservableCollection<RvtFiles>();
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
                var files = System.IO.Directory.GetFiles(DirectoryPath, "*.rvt", System.IO.SearchOption.TopDirectoryOnly);
                ListFiles.Clear();
                foreach (var file in files)
                {
                    RvtFiles rvtFile = new RvtFiles()
                    {
                        FilePath = file,
                        OriginFilename = System.IO.Path.GetFileName(file),
                        NewFileName = "",
                        Workshared = false,
                        Coverted = false
                    };
                    ListFiles.Add(rvtFile);
                }
            }
        }

        #endregion

        // rename fole batch add prefix UPD_ function
        public void AjouterPrefix(string prefix)
        {
            string dossier = this._DirectoryPath;

            foreach (RvtFiles fichier in this.ListFiles)
            {
                string nomActuel = Path.GetFileName(fichier.FilePath);

                // Si le préfixe existe déjà → on skip
                if (nomActuel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string nouveauNom = Path.Combine(dossier, prefix + nomActuel);
                File.Move(fichier.FilePath, nouveauNom);
                fichier.TempFilename = nouveauNom;
            }
        }

        public void EnleverPrefix(string prefix)
        {
            var files = Directory.GetFiles(_DirectoryPath, "*.rvt", SearchOption.TopDirectoryOnly);

            foreach (string filePath in files)
            {
                string nom = Path.GetFileName(filePath);

                if (nom.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string nouveauNom = Path.Combine(_DirectoryPath, nom.Substring(prefix.Length));
                    File.Move(filePath, nouveauNom);
                    
                }
            }
        }


        public void RenameFile()
        {

            foreach (RvtFiles fichier in this.ListFiles)
            {
                // add sufix to name file
                string nomActuel = Path.GetFileNameWithoutExtension(fichier.FilePath);
                string extension = Path.GetExtension(fichier.FilePath);
                string nouveauNom = nomActuel + this._Prefix + extension;
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
                string savePath = Path.Combine(this.PathConvertedDirectory, fichier.NewFileName);
                // if file exist skip
                if (File.Exists(savePath))
                {
                    i++;
                    iP++;
                    prgs.Chanage1(iP, this.ListFiles.Count, string.Format("Upgrad: {0} / Not Upgrad: {1} From ({2})", i, n, this.ListFiles.Count));
                    continue;
                }


                try
                {
                    string filePath = Path.Combine(this.DirectoryPath, fichier.TempFilename);
                    ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
                    OpenOptions openOpts = new OpenOptions { DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets };

                    Document doc = Application.uiAppout.Application.OpenDocumentFile(modelPath, openOpts);
                    fichier.DocRvt = doc;
                    fichier.Workshared = doc.IsWorkshared;
                    SaveAsOptions saveOpts = new SaveAsOptions { OverwriteExistingFile = true };
                    if (doc.IsWorkshared)
                    {
                        saveOpts.SetWorksharingOptions(new WorksharingSaveAsOptions { SaveAsCentral = true });
                    }
                    doc.SaveAs(savePath, saveOpts);
                    fichier.Coverted = true;
                    i++;
                    doc.Close();
                    // 
                }
                catch (Exception ex)
                {
                    faillureelem.Append("\n" + fichier.FilePath + ":" +  ex.Message + "\n");
                    n++;

                }
                iP++;
                prgs.Chanage1(iP, this.ListFiles.Count, string.Format("Upgrad: {0} / Not Upgrad: {1} From ({2})", i, n, this.ListFiles.Count));
                if (prgs.Cancellation()) { break; }
            }
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

    }

    public  class RvtFiles
    {
        public Document DocRvt { get; set; }
        public string OriginFilename { get; set; } = "";
        public string TempFilename { get; set; } = "";
        public string NewFileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public bool Workshared { get; set; } = false;
        public bool Coverted { get; set; } = false;


    }
}
