using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System.Linq;
using System.Windows;

namespace UpgradeUpdateRvt
{
    [Transaction(TransactionMode.Manual)]
    public class Application : IExternalCommand  
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

            // All modifications to the Revit model must be within a Transaction
            /*  using (Transaction t = new Transaction(doc, "My Command Transaction"))
              {
                  t.Start();

                  // Your code to interact with the document goes here (e.g., creating elements, modifying parameters)
                  TaskDialog.Show("Revit Add-in", "The active document name is: " + doc.Title);

                  t.Commit();
              }*/
            uiAppout.Application.FailuresProcessing += OnFailuresProcessing;
            uiAppout.DialogBoxShowing += Ignore_the_diagore;
            ApplicatioUI appUI = new ApplicatioUI();
            appUI.ShowDialog();

            return Result.Succeeded;
        }

        private void OnFailuresProcessing(object sender, FailuresProcessingEventArgs e)
        {
            var fa = e?.GetFailuresAccessor();

            // Ignore all warnings.
            fa.DeleteAllWarnings();

            // Resolve all resolvable errors.
            var failures = fa.GetFailureMessages();
            if (!failures.Any())
            {
                return;
            }

            failures = failures.Where(fail => fail.HasResolutions()).ToList();
            fa.ResolveFailures(failures);
        }
        private void Ignore_the_diagore(object o, DialogBoxShowingEventArgs e)
        {
            // DialogBoxShowingEventArgs has two subclasses - TaskDialogShowingEventArgs & MessageBoxShowingEventArgs
            // In this case we are interested in this event if it is TaskDialog being shown. 
            TaskDialogShowingEventArgs t = e as TaskDialogShowingEventArgs;
            // t is not null  and t.message have text
            if (t != null && t.Message != "")
            {
                // Call OverrideResult to cause the dialog to be dismissed with the specified return value
                // (int) is used to convert the enum TaskDialogResult.No to its integer value which is the data type required by OverrideResult
                try { e.OverrideResult((int)TaskDialogResult.Close);} catch{}
                try { e.OverrideResult((int)TaskDialogResult.Ok); } catch { }
                try { e.OverrideResult((int)TaskDialogResult.Cancel); } catch { }
            }
        }
    }

}
