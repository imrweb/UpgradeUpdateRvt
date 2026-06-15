using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using ProrsbArs;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UpgradeUpdateRvt.Model;
using UpgradeUpdateRvt.ViewModel;
namespace UpgradeUpdateRvt.Command
{
    internal class LoadCurrentLinksCommand : ICommand
    {
        private LinkRvtVM _linkRvtVM;
        public LoadCurrentLinksCommand(LinkRvtVM linkRvtVM) { _linkRvtVM = linkRvtVM; }
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
            ProgressBar prgs = new ProgressBar();
            string FileName = "Title";
            StringBuilder faillureelem = new StringBuilder();
            StringBuilder render = new StringBuilder();
            int i = 0;
            int iP = 0;
            int n = 0;
            prgs.Run(false, FileName);
            prgs.MessageText(false, "Please wait ..");

            if (_linkRvtVM.LinkModel.IsIfcFiles == false)
            {
                foreach (RvtLinks link in _linkRvtVM.LinkModel.ListFiles)
                {
                    try
                    {
                        WorksetConfiguration wsconf = new WorksetConfiguration();
                        ModelPath path = ModelPathUtils.ConvertUserVisiblePathToModelPath(
                             link.FilePath
                            );
                        if (link.Linked && !link.Loaded)
                        {
                            if (link.CorrespondLink != null)
                            {
                                link.CorrespondLink.LinkElement.LoadFrom(path, wsconf);
                                link.Loaded = true;
                            }
                            else
                            {
                                using (Transaction t = new Transaction(_linkRvtVM._currentDoc, "Linkdata Transaction"))
                                {
                                    t.Start();
                                    FilePath newpath = new FilePath(link.FilePath);
                                    RevitLinkOptions options = new RevitLinkOptions(true);
                                    
                                    // Create new revit link storing absolute path to file
                                    LinkLoadResult result = RevitLinkType.Create(_linkRvtVM._currentDoc, path, options);
                                    RevitLinkInstance instance = RevitLinkInstance.Create(_linkRvtVM._currentDoc, result.ElementId);
                                    instance.MoveBasePointToHostBasePoint(true);
                                    // Commit the transaction
                                    t.Commit();
                                    i++;
                                }
                                link.Loaded = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        faillureelem.Append("\n" + link.FilePath + ":" + ex.Message + "\n");
                        n++;

                    }
                    iP++;
                    prgs.Chanage1(iP, this._linkRvtVM.LinkModel.ListFiles.Count, string.Format("Upgrad: {0} / Not Upgrad: {1} From ({2})", i, n, this._linkRvtVM.LinkModel.ListFiles.Count));
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
            if (_linkRvtVM.LinkModel.IsIfcFiles == true)
            {
                foreach (RvtLinks link in _linkRvtVM.LinkModel.ListFiles)
                {
                    try
                    {
                        WorksetConfiguration wsconf = new WorksetConfiguration();
                        ModelPath path = ModelPathUtils.ConvertUserVisiblePathToModelPath(
                             link.FilePath
                            );
                        if (link.Linked && !link.Loaded)
                        {
                            if (link.CorrespondLink != null)
                            {
                                link.CorrespondLink.LinkElement.LoadFrom(path, wsconf);
                                link.Loaded = true;
                            }
                            else
                            {
                                string ifcFilePath = link.FilePath;
                                using (Transaction t = new Transaction(_linkRvtVM._currentDoc, "Linkdata IFC Transaction"))
                                {
                                    t.Start();
                                    bool check = File.Exists(ifcFilePath + ".RVT");
                                    if (!check)
                                    {
                                        Document ifcdoc = null;
                                        try
                                        {
                                            ifcdoc = _linkRvtVM._currentDoc.Application.OpenIFCDocument(ifcFilePath);
                                            ifcdoc.SaveAs(ifcFilePath + ".RVT");
                                        }
                                        finally
                                        {
                                            if (ifcdoc != null)
                                            {
                                                try
                                                {
                                                    ifcdoc.Close(false);
                                                }
                                                catch { }
                                            }
                                        }
                                    }

                                    FilePath newpath = new FilePath(ifcFilePath + ".RVT");
                                    RevitLinkOptions options = new RevitLinkOptions(false);
                                    LinkLoadResult result = RevitLinkType.Create(_linkRvtVM._currentDoc, newpath, options);
                                    RevitLinkInstance.Create(_linkRvtVM._currentDoc, result.ElementId);
                                    t.Commit();
                                }
                                link.Loaded = true;
                            }
                        }
                        i++;
                    }
                    catch (Exception ex)
                    {
                        faillureelem.Append("\n" + link.FilePath + ":" + ex.Message + "\n");
                        n++;

                    }
                    iP++;
                    prgs.Chanage1(iP, this._linkRvtVM.LinkModel.ListFiles.Count, string.Format("Upgrad: {0} / Not Upgrad: {1} From ({2})", i, n, this._linkRvtVM.LinkModel.ListFiles.Count));
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
    }
}
