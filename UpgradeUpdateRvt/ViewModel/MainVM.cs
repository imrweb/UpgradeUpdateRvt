
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
        public  MainM Init { get; set; }
        public MainVM()
        {

           _doc = Application.docout;
            Init = new MainM();
            RefreshCommand = new Command.RefreshCommand(this);
            ConvertCommand = new Command.ConvertCommand(this);
            BrowseFolderCommand = new Command.BrowseFolderCommand(this);


        }
    }
}
