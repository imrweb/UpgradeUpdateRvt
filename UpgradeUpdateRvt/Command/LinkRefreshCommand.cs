using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UpgradeUpdateRvt.Model;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class LinkRefreshCommand : ICommand
    {
        private LinkRvtVM _linkRvtVM;
        public LinkRefreshCommand(LinkRvtVM linkRvtVM) { _linkRvtVM = linkRvtVM; }
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
            _linkRvtVM.LinkModel.ReloadFiles();
            _linkRvtVM.LinkModel.LinkedRvt(_linkRvtVM._currentDoc);

            foreach (RvtLinks linkfile in _linkRvtVM.LinkModel.ListFiles)
            {
                DocLinks linkSimilarity = _linkRvtVM.LinkModel.AnalyseSimilarity(linkfile);
                if (linkSimilarity != null)
                {
                    linkfile.CorrespondLink = linkSimilarity;
                    if (linkSimilarity.LinkElement.GetLinkedFileStatus() == LinkedFileStatus.Loaded)            
                    {
                        linkfile.Loaded = true;
                    }
                }
            }

        }
    }
}
