using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UpgradeUpdateRvt.Model;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class RefreshCommand : ICommand
    {
        private MainVM _mainVM;
        public RefreshCommand(MainVM mainVM) { _mainVM = mainVM; }
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
            _mainVM.Init.EnleverPrefix("UPD_");
            _mainVM.Init.LoadFiles();
            _mainVM.Init.RenameFile();
            _mainVM.Init.CanConvert = true;


        }
    }
}
