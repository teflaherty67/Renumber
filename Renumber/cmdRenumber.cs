#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace Renumber
{
    [Transaction(TransactionMode.Manual)]
    public class cmdRenumber : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // put any code needed for the form here

            // open form

            View curView = doc.ActiveView;

            if (curView is Autodesk.Revit.DB.ViewSection)
            {
                frmViewSection curForm = new frmViewSection()
                {
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    Topmost = true,
                };

                curForm.ShowDialog();
            }
            else if (curView is Autodesk.Revit.DB.ViewSheet)
            {
                frmViewSheet curForm = new frmViewSheet()
                {
                    Width = 400,
                    Height = 380,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    Topmost = true,
                };

                curForm.ShowDialog();
            }

            else if (curView is Autodesk.Revit.DB.ViewPlan)
            {
                frmViewPlan curForm = new frmViewPlan()
                {
                    Width = 400,
                    Height = 350,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    Topmost = true,
                };

                curForm.ShowDialog();
            }

            // get form data and do something

            return Result.Succeeded;
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
