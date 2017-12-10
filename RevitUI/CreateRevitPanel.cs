using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Windows.Media.Imaging;
using System.IO;

namespace RevitUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class CreateRevitPanel : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                CreateSamplePanel(application);
                //CreateRibbonSamplePanel(application);
                return Result.Succeeded;
            }
            catch(Exception ex)
            {
                TaskDialog.Show(ex.ToString(), "创建面板失败");
                return Result.Failed;
            }

            
        }

        /// <summary>
        /// 添加标签页，面板和按钮
        /// </summary>
        /// <param name="application"></param>
        private void CreateSamplePanel(UIControlledApplication application)
        {
            string tabName = "AMAZINGLIN";
            //创建标签页
            application.CreateRibbonTab(tabName);
            //在标签页上创建面板
            RibbonPanel selPanal = application.CreateRibbonPanel(tabName, "选择面板");
            RibbonPanel showRoomBoundary = application.CreateRibbonPanel(tabName, "显示房间边界");
            RibbonPanel manualSelectRoom = application.CreateRibbonPanel(tabName, "手动选择房间");
            //在已有标签页上添加面板
            //在附加模块添加
            application.CreateRibbonPanel(Tab.AddIns, "选择楼层");
            //在分析模块添加
            application.CreateRibbonPanel(Tab.Analyze, "自动分析");

            //创建按钮
            PushButton pushBatton = selPanal.AddItem(
                new PushButtonData("选择楼层", "选择楼层",
                "D:/XuJL/Revit/Room/GetRoomList/RoomList/HelloWorld/bin/Debug/HelloWorld.dll", "HelloWorld.HelloWorld"))
                as PushButton;

            ////创建显示房间边界按钮
            //PushButton pushBatton = selPanal.AddItem(
            //    new PushButtonData("显示房间边界", "显示房间边界",
            //    "D:/XuJL/Revit/Room/GetRoomList/RoomList/RoomOperation/bin/Debug/RoomOperation.dll", "RoomOperation.DispalyRoomCurve"))
            //    as PushButton;

            //为按钮设置图片
            Uri uri = new Uri("C:/Program Files/Autodesk/Revit 2016/SDA/data/resources/languages/italy.png");
            BitmapImage image = new BitmapImage(uri);
            pushBatton.LargeImage = image;
            pushBatton.ToolTip = "选择一个楼层";
            pushBatton.LongDescription = "选择一个楼层，自动提取楼层边界";

            //创建显示房间边界按钮
            PushButton displayRoomCurveBtn = showRoomBoundary.AddItem(
                new PushButtonData("显示房间边界", "显示房间边界",
                "D:/XuJL/Revit/Room/GetRoomList/RoomList/AutoCreateRoomSlab/bin/Debug/AutoCreateRoomSlab.dll", "AutoCreateRoomSlab.AutoCreateRoomSlab"))
                as PushButton;

            //为按钮设置图片
            Uri roomBoundaryUri = new Uri("C:/Program Files/Autodesk/Revit 2016/SDA/data/resources/languages/usa.png");
            BitmapImage roomBoundaryImage = new BitmapImage(roomBoundaryUri);
            displayRoomCurveBtn.LargeImage = roomBoundaryImage;

            //
            //创建手动选择房间按钮
            PushButton manualSelectRoomBtn = manualSelectRoom.AddItem(
                new PushButtonData("选择房间", "选择一个房间",
                "D:/XuJL/Revit/Room/GetRoomList/RoomList/ManualSelectRoom/bin/Debug/ManualSelectRoom.dll", "ManualSelectRoom.ManualCreateRoomSlab"))
                as PushButton;

            //为按钮设置图片
            Uri selectRoomURI = new Uri("C:/Program Files/Autodesk/Revit 2016/SDA/data/resources/languages/brazil.png");
            BitmapImage selectRoomImage = new BitmapImage(selectRoomURI);
            manualSelectRoomBtn.LargeImage = selectRoomImage;
        }

        /// <summary>
        /// 网上的样例，创建多级图标
        /// </summary>
        /// <param name="application"></param>
        /// 

        // 程序集路径
        static string AddInPath = typeof(CreateRevitPanel).Assembly.Location;
        // 按钮图标目录
        static string ButtonIconsFolder = Path.GetDirectoryName(AddInPath);

        private void CreateRibbonSamplePanel(UIControlledApplication application)
        {
            //RibbonPanel显示一个大按钮，图标为第一个按钮的图标
            string firstPanelName = "Ribbon 实例";//面板底部文字提示
            RibbonPanel ribbonSamplePanel = application.CreateRibbonPanel(firstPanelName);

            #region 创建墙和结构墙

            //RibbonPanel(面板)->SplitButton(按钮组)->PushButton(按钮)
            SplitButtonData splitButtonData = new SplitButtonData("NewWallSplit", "创建墙");//按钮数据，按钮组显示的文字为第一个按钮的文字
            SplitButton splitButton = ribbonSamplePanel.AddItem(splitButtonData) as SplitButton;//添加到面板

            //PushButton pushButton = splitButton.AddPushButton(new PushButtonData("WallPush", "普通墙", AddInPath, "Revit.SDK.Samples.Ribbon.CS.CreateWall"));
            //最后一个参数是执行按钮事件的响应类名
            PushButtonData pushButtonDataWall = new PushButtonData("WallPush", "普通墙", AddInPath, "HY.CreateWall");
            PushButton pushButton = splitButton.AddPushButton(pushButtonDataWall);
            //大图标，小图标
            pushButton.LargeImage = new BitmapImage(new Uri(Path.Combine(ButtonIconsFolder, "CreateWall.png"), UriKind.Absolute));
            pushButton.Image = new BitmapImage(new Uri(Path.Combine(ButtonIconsFolder, "CreateWall-S.png"), UriKind.Absolute));
            //提示文字，提示图片
            pushButton.ToolTip = "Creates a partition wall in the building model.";
            pushButton.ToolTipImage = new BitmapImage(new Uri(Path.Combine(ButtonIconsFolder, "CreateWallTooltip.bmp"), UriKind.Absolute));

            //按钮组里第二个按钮
            pushButton = splitButton.AddPushButton(new PushButtonData("StrWallPush", "结构墙", AddInPath, "HY.CreateStructureWall"));
            pushButton.LargeImage = new BitmapImage(new Uri(Path.Combine(ButtonIconsFolder, "StrcturalWall.png"), UriKind.Absolute));
            pushButton.Image = new BitmapImage(new Uri(Path.Combine(ButtonIconsFolder, "StrcturalWall-S.png"), UriKind.Absolute));

            #endregion

            ribbonSamplePanel.AddSeparator();
        }
    }
}
