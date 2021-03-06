﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Architecture;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;

namespace RoomOperation
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class GetRoomCurve : IExternalCommand
    {
        public Document Document { get; private set; }
        public Application Application { get; private set; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document = commandData.Application.ActiveUIDocument.Document;
            Application = commandData.Application.Application;

            //输出所有房间
            List<Room> allRooms = GetAllRooms();
            foreach(var room in allRooms)
            {
                Debug.Print(room.Name);
            }

            //创建房间边界线
            Operation();

            return Result.Succeeded;
        }

        private List<Room> GetAllRooms()
        {
            FilteredElementCollector collector = new FilteredElementCollector(Document);
            var rooms = collector.OfCategory(BuiltInCategory.OST_Rooms).OfType<Room>();
            return rooms.ToList();
        }

        /// <summary>
        /// 获取房间边界
        /// </summary>
        /// <returns></returns>
        /// 
        public void GetRoomBoundary()
        {
            //var opt = new SpatialElementBoundaryOptions()
            //{
            //    SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            //};

            // 定义边界设置  
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            opt.StoreFreeBoundaryFaces = true;
            opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center; // 闭合线  
                                                                                        //opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish; // 非闭合线  

            var allRooms = new FilteredElementCollector(Document).OfClass(typeof(SpatialElement)).OfCategory(BuiltInCategory.OST_Rooms);
            // 遍历模型中所有的房间
            foreach (Room room in allRooms)
            {
                CurveArray array = new CurveArray();
                Floor floor = null;
                var curves = room.GetBoundarySegments(opt).First().Select(y => y.GetCurve());

                // 尝试创建楼板，包括曲线段
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
                    lastCurve = curve;
                }
                //floor = Document.Create.NewFloor(array, floorType, room.Level, false);
            }

            //using (Transaction tx = new Transaction(Document))
            //{
            //    tx.Start("Convert Area Loops To Model Curves");

            //    foreach (SpatialElement e in allRooms)
            //    {
            //        Area area = e as Area;
            //        Debug.Print(area.Name);

            //        double z = area.Level.Elevation;
            //        Plane levelPlane = Application.Create.NewPlane(XYZ.BasisZ, new XYZ(0, 0, z));
            //        SketchPlane sketchPlane = SketchPlane.Create(Document, levelPlane);
            //        //SketchPlane sketchPlane = doc.Create.NewSketchPlane(levelPlane);
            //        IList<IList<BoundarySegment>>
            //        loops = area.GetBoundarySegments(opt);
            //        foreach (IList<BoundarySegment> loop in loops)
            //        {
            //            foreach (BoundarySegment seg in loop)
            //            {
            //                Document.Create.NewModelCurve(seg.GetCurve(), sketchPlane);
            //            }
            //        }
            //    }

            //    tx.Commit();
            //}
        }

        public void Operation()
        {
            Transaction trans = new Transaction(Document);
            trans.Start("Make Slab");
            // 查找名称为房间的楼板类型
            var floorType = (new FilteredElementCollector(Document).OfClass(typeof(FloorType)).First(x => x.Name == "房间") as FloorType) as FloorType;
            var opt = new SpatialElementBoundaryOptions()
            {
                StoreFreeBoundaryFaces = true,
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };
            // 遍历模型中所有的房间
            foreach (Room room in new FilteredElementCollector(Document).OfClass(typeof(SpatialElement)).OfCategory(BuiltInCategory.OST_Rooms))
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
                        array.Append(Line.CreateBound(lastCurve.GetEndPoint(1), curve.GetEndPoint(1)));
                        lastCurve = curve;
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
