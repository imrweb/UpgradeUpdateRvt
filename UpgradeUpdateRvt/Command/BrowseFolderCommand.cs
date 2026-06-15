using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class BrowseFolderCommand : ICommand
    {
        private MainVM _mainVM;
        public BrowseFolderCommand(MainVM mainVM) { _mainVM = mainVM; }
        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Sélectionner le dossier contenant les maquettes Revit"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string folderPath = dialog.FolderName;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    _mainVM.Init.MainPath = folderPath;
                    _mainVM.Init.DirectoryPath = folderPath;
                    _mainVM.Init.FileNameOnly = "";
                    
                    _mainVM.Init.LoadFiles();
                    _mainVM.Init.RenameFile();
                    _mainVM.Init.CanConvert = true;
                }
            }
        }



    }
}
