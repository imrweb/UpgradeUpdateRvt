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
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            // rn browse .rvt openFileDialog
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".rvt"; // Default file extension
            dialog.Filter = "Text documents (.rvt)|*.rvt"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                // get directory path
                string directoryPath = System.IO.Path.GetDirectoryName(filename);
                // het file name 
                string fileNameOnly = System.IO.Path.GetFileName(filename);
                // get path up to directory Directory Parent of directoryPath
             
               


                if (filename != "")
                {
                    _mainVM.Init.MainPath = filename;
                    _mainVM.Init.DirectoryPath = directoryPath;
                    _mainVM.Init.FileNameOnly = fileNameOnly;
                 
                }
            }
        }



    }
}
