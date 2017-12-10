using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Architecture;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using CommonMethod;

namespace AutoCreateRoomSlab
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class AutoCreateRoomSlab : IExternalCommand
    {
        public UIDocument UIDoc { get; private set; }
        public Document Document { get; private set; }
        public Application Application { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDoc = commandData.Application.ActiveUIDocument;
            Document = UIDoc.Document;
            Application = commandData.Application.Application;

            if (!LineStyle.IsExistLineStyle(Document, "房间边界线"))
            {
                ////生成线样式
                using (Transaction ts = new Transaction(Document, "LineStyle"))
                {
                    ts.Start();
                    Category lineCategory = Document.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
                    Category newCategory = Document.Settings.Categories.NewSubcategory(lineCategory, "房间边界线");
                    Color newColor = new Color(255, 0, 0);
                    newCategory.LineColor = newColor;
                    newCategory.SetLineWeight(1, GraphicsStyleType.Projection);
                    ts.Commit();
                }
            }
            Operation();

            return Result.Succeeded;
        }

        public void Operation()
        {
            Transaction trans = new Transaction(Document);
            trans.Start("Make Slab");
            // 查找名称为房间的楼板类型
            var floorType = (new FilteredElementCollector(Document).OfClass(typeof(FloorType)).First(x => x.Name == "房间") as FloorType) as FloorType;
            var opt = new SpatialElementBoundaryOptions()
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };
            // 遍历模型中所有的房间
            var allRooms = new FilteredElementCollector(Document).OfClass(typeof(SpatialElement)).OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Room room in allRooms)
            {
                CurveArray array = new CurveArray();
                Floor floor = null;
                var curves = room.GetBoundarySegments(opt).First().Select(y => y.GetCurve());

                // 尝试创建楼板，包括曲线段
                try
                {
                    Curve lastCurve = curves.Cast<Curve>().Last();
                    foreach (Curve curve in curves)
                    {
                        if (curve is Arc)
                        {
                            var arc = curve as Arc;
                            var cp = arc.Evaluate(0.5, true);
                            array.Append(Arc.Create(lastCurve.GetEndPoint(1), curve.GetEndPoint(1), cp));
                        }
                        else
                        {
                            array.Append(Line.CreateBound(lastCurve.GetEndPoint(1), curve.GetEndPoint(1)));
                        }

                        //DetailCurve dc = Document.Create.NewDetailCurve(UIDoc.ActiveView, curve);
                        //if (LineStyle.BackLineStyle(Document) != null)
                        //{
                        //    LineStyle.SetLineStyle(LineStyle.BackLineStyle(Document), dc);
                        //}

                        lastCurve = curve;
                    }
                    floor = Document.Create.NewFloor(array, floorType, room.Level, false);

                }

                // 如果创建楼板出错则使用仅直线段的方式创建楼板
                catch
                {
                    Curve lastCurve = curves.Cast<Curve>().Last();
                    foreach (Curve curve in curves)
                    {
                        var line = Line.CreateBound(lastCurve.GetEndPoint(1), curve.GetEndPoint(1));
                        array.Append(line);
                        lastCurve = curve;

                        DetailCurve dc = Document.Create.NewDetailCurve(UIDoc.ActiveView, line);
                        if (LineStyle.BackLineStyle(Document) != null)
                        {
                            LineStyle.SetLineStyle(LineStyle.BackLineStyle(Document), dc);
                        }
                    }
                    try
                    {
                        floor = Document.Create.NewFloor(array, floorType, room.Level, false);
                    }
                    catch
                    {
                        floor = null;
                    }
                }
                finally
                {
                    if (floor != null)
                    {
                        // 设置创建出的楼板的各种参数
                        var floorName = room.Level.Name.Substring(0, room.Level.Name.IndexOf("层"));
                        floor.LookupParameter("RoomIndex").Set(floorName + (room.Number.Length < 2 ? ("0" + room.Number) : room.Number)); //楼层+编号
                        floor.LookupParameter("RoomGUID")?.Set(room.UniqueId);
                        floor.LookupParameter("自标高的高度偏移").Set(UnitUtils.ConvertToInternalUnits(3000, DisplayUnitType.DUT_MILLIMETERS));
                    }
                }
            }
            trans.Commit();
        }


    }
}
