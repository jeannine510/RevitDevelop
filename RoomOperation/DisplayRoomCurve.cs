using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using System.Diagnostics;

namespace RoomOperation
{
    [Autodesk.Revit.Attributes.Transaction(TransactionMode.Manual)]
    class DisplayRoomCurve : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            FilteredElementCollector col = null;

            //ElementSet set = uidoc.Selection.Elements;
            IList<Element> set = uidoc.Selection.PickElementsByRectangle();

            int n = set.Count;

            if (0 < n)
            {
                List<ElementId> ids = new List<ElementId>(set.OfType<Area>().Select<Area, ElementId>(e => e.Id));

                if (0 == ids.Count)
                {
                    message = "Please select some area alements "
                      + " before running his command, or nothing "
                      + "at all to process all of them.";

                    return Result.Failed;
                }

                // 注意从逻辑上讲是不需要调用 WhereElementIsNotElementType() 方法的。  
                // 但是 Revit API 有个小缺陷，如果不调用则在对 col 进行过滤操作时会抛出一个异常。  
                col = new FilteredElementCollector(doc, ids).WhereElementIsNotElementType();
            }
            else
            {
                // 注意我们不能使用 Area 作为 OfClass() 方法的参数，因为 Area 不是 Revit 的本地对象。  
                // 应该先使用 Area 的基类 SpatialElement 作为 OfClass() 方法的参数，然后在结果中将类型转换成 Area。  
                col = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement));
            }

            // 定义边界设置  
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            opt.StoreFreeBoundaryFaces = true;
            opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center; // 闭合线  
                                                                                        //opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish; // 非闭合线  

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Convert Area Loops To Model Curves");

                foreach (SpatialElement e in col)
                {
                    Area area = e as Area;
                    Debug.Print(area.Name);

                    double z = area.Level.Elevation;
                    Plane levelPlane = app.Create.NewPlane(XYZ.BasisZ, new XYZ(0, 0, z));
                    SketchPlane sketchPlane = SketchPlane.Create(doc, levelPlane);
                    //SketchPlane sketchPlane = doc.Create.NewSketchPlane(levelPlane);
                    IList<IList<BoundarySegment>>
                    loops = area.GetBoundarySegments(opt);
                    foreach (IList<BoundarySegment> loop in loops)
                    {
                        foreach (BoundarySegment seg in loop)
                        {
                            doc.Create.NewModelCurve(seg.GetCurve(), sketchPlane);
                        }
                    }
                }

                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
}
