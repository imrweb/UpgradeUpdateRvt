
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Input;
using UpgradeUpdateRvt.Model;
namespace UpgradeUpdateRvt.ViewModel
{
    internal class MainVM
    {

    Document _doc ;
        public ICommand RefreshCommand { get; set; }
        public ICommand ConvertCommand { get; set; }
        public ICommand BrowseFolderCommand { get; set; }
        public ICommand CleanupCommand { get; set; }
        public ICommand ScanCleanupCommand { get; set; }
        public ICommand SelectAllFilesCommand { get; set; }
        public ICommand UnselectAllFilesCommand { get; set; }
        public  MainM Init { get; set; }
        public MainVM()
        {

           _doc = Application.docout;
            Init = new MainM();
            RefreshCommand = new Command.RefreshCommand(this);
            ConvertCommand = new Command.ConvertCommand(this);
            BrowseFolderCommand = new Command.BrowseFolderCommand(this);
            CleanupCommand = new Command.CleanupCommand(this);
            ScanCleanupCommand = new Command.ScanCleanupCommand(this);
            SelectAllFilesCommand = new Command.SelectAllFilesCommand(this);
            UnselectAllFilesCommand = new Command.UnselectAllFilesCommand(this);


        }
    }
}
