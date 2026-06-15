using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Input;
using UpgradeUpdateRvt.Model;
using static Autodesk.Revit.DB.SpecTypeId;

namespace UpgradeUpdateRvt.ViewModel
{
    public class LinkRvtVM
    {

        public ICommand LinkRefreshCommand { get; set; }
        public ICommand LoadCurrentLinksCommand { get; set; }
        public ICommand LinkBrowseFolderCommand { get; set; }
        public LinkRvtM LinkModel { get; }

        public Document _currentDoc;

        public LinkRvtVM()
        {

            _currentDoc = LinkRvt.docout;
            LinkModel = new LinkRvtM();
            LinkModel.IsIfcFiles = false;
            LinkBrowseFolderCommand = new Command.LinkBrowseFolderCommand(this);
            LinkRefreshCommand = new Command.LinkRefreshCommand(this);
            LoadCurrentLinksCommand = new Command.LoadCurrentLinksCommand(this);
 
        }
    }
}