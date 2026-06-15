using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpgradeUpdateRvt
{
    [Transaction(TransactionMode.Manual)]
    public class LinkRvt : IExternalCommand
    {
        public static Document docout;
        public static UIDocument uiDocout;
        public static UIApplication uiAppout;
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // Get the active UI document and application
            uiAppout = commandData.Application;
            uiDocout = uiAppout.ActiveUIDocument;

            // Get the active database document
            docout = uiDocout.Document;
 
            LinkRvtUI LinkUI = new LinkRvtUI();
            LinkUI.ShowDialog();

            return Result.Succeeded;
        }



    }
}
