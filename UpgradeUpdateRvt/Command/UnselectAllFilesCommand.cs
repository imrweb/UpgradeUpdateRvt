using System;
using System.Windows.Input;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class UnselectAllFilesCommand : ICommand
    {
        private readonly MainVM _mainVM;

        public UnselectAllFilesCommand(MainVM mainVM) { _mainVM = mainVM; }

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
            foreach (var file in _mainVM.Init.ListFiles)
            {
                file.Selected = false;
            }
        }
    }
}
