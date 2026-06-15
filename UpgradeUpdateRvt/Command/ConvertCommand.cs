using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UpgradeUpdateRvt.Model;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class ConvertCommand : ICommand
    {
        private MainVM _mainVM;
        public ConvertCommand(MainVM mainVM) { _mainVM = mainVM; }
        
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
            try
            {
                if (!_mainVM.Init.SaveInSameFolder)
                {
                    var parentDir = Directory.GetParent(_mainVM.Init.DirectoryPath);
                    string pathUpToDirectory = parentDir != null ? parentDir.FullName : _mainVM.Init.DirectoryPath;
                    // create Folder Converted if not exist in pathUpToDirectory
                    string convertedFolderPath = System.IO.Path.Combine(pathUpToDirectory, "Converted");
                    string convertdirectery = convertedFolderPath;
                    if (!Directory.Exists(convertedFolderPath))
                    {
                        convertdirectery = Directory.CreateDirectory(convertedFolderPath).FullName;
                    }
                    _mainVM.Init.PathConvertedDirectory = convertdirectery;
                }
                
                // add prefix to file name
                _mainVM.Init.AjouterPrefix("UPD_");
                _mainVM.Init.UpgradeUpdateRvt();
            }
            finally
            {
                _mainVM.Init.EnleverPrefix("UPD_");
            }
        }
    }
}
