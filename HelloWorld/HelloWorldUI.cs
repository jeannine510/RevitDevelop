using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace RoomList
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class HelloWorldUI : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            throw new NotImplementedException();
        }

        public Result OnStartup(UIControlledApplication application)
        {
            TaskDialog.Show("Amazinglin", "Hello World");
            return Result.Succeeded;
        }
    }
}
