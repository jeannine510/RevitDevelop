using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using System.Xml;
using Autodesk.Revit.UI.Events;
using System.Windows.Forms;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB.Architecture;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace ManualSelectRoom
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ManualCreateRoomSlab : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (IsExistLineStyle(doc, "房间边界线"))
            {

            }
            else
            {
                ////生成线样式
                using (Transaction ts = new Transaction(doc, "LineStyle"))
                {
                    ts.Start();
                    Category lineCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
                    Category newCategory = doc.Settings.Categories.NewSubcategory(lineCategory, "房间边界线");
                    Color newColor = new Color(255, 0, 0);
                    newCategory.LineColor = newColor;
                    newCategory.SetLineWeight(1, GraphicsStyleType.Projection);
                    ts.Commit();
                }
            }

            Transaction ts2 = new Transaction(doc, "BIM");
            ts2.Start();
            Reference refer = uidoc.Selection.PickObject(ObjectType.Element, "");
            Room room = doc.GetElement(refer) as Room;

            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;
            CurveArray array = new CurveArray();
            IList< IList<BoundarySegment> > loops = room.GetBoundarySegments(opt);
            foreach (var loop in loops)
            {
                foreach (BoundarySegment seg in loop)
                {
                    Curve curve = seg.GetCurve();
                    DetailCurve dc = doc.Create.NewDetailCurve(uidoc.ActiveView, curve);
                    if (BackLineStyle(doc) != null)
                    {
                        SetLineStyle(BackLineStyle(doc), dc);
                    }

                    array.Append(dc.GeometryCurve);
                }
                break;
            }

            //foreach(Curve a in array)
            //{
            //    Debug.Write($"{a.GetEndPoint(0).X.ToString()}\n");
            //    Debug.Write($"{a.GetEndPoint(1).X.ToString()}\n");
            //}
            //foreach (Curve a in array)
            //{
            //    Debug.Write($"{a.GetEndPoint(0).Y.ToString()}\n");
            //    Debug.Write($"{a.GetEndPoint(1).Y.ToString()}\n");
            //}

            CreateFloor(doc, array);
            ts2.Commit();
            return Result.Succeeded;
        }
        //设置线样式
        private void SetLineStyle(Category cate, DetailCurve line)
        {
            ElementId Id = new ElementId(cate.Id.IntegerValue + 1);

            foreach (Parameter p in line.Parameters)
            {
                if (p.Definition.Name == "线样式")
                {
                    p.Set(Id);
                    break;
                }
            }
        }
        //判断线样式是否存在
        private bool IsExistLineStyle(Document doc, string Name)
        {

            Category IsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            CategoryNameMap map = IsCategory.SubCategories;
            foreach (Category g in map)
            {
                if (g.Name == Name)
                {
                    return true;
                }
            }
            return false;
        }
        //搜索目标线样式
        private Category BackLineStyle(Document doc)
        {
            Category lineCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            CategoryNameMap map = lineCategory.SubCategories;
            foreach (Category g in map)
            {
                if (g.Name == "房间边界线")
                {
                    return g;
                }
            }
            return null;
        }
        //创建楼板，类型默认 标高默认
        private void CreateFloor(Document doc, CurveArray array)
        {
            FloorType floorType = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).FirstOrDefault() as FloorType;
            Level level = new FilteredElementCollector(doc).OfClass(typeof(Level)).OrderBy(o => (o as Level).ProjectElevation).First() as Level;

            try
            {
                Floor floor = doc.Create.NewFloor(array, floorType, level, false, XYZ.BasisZ);
            }
            catch(Exception ex)
            {
                Debug.Write(ex.ToString());
                CurveArray newArray = new CurveArray();

                foreach (var a in array)
                {
                    if(a is Arc)
                    {
                        var arc = a as Arc;
                        //arc = Arc.Create(arc.GetEndPoint(1), arc.GetEndPoint(0), arc.Evaluate(0.5, true));
                        var line = Line.CreateBound(arc.GetEndPoint(0), arc.GetEndPoint(1));
                        newArray.Append(line);
                        continue;
                    }
                    newArray.Append(a as Line);
                }
                Floor floor = doc.Create.NewFloor(newArray, floorType, level, false, XYZ.BasisZ);
            }
            
        }
    }
}