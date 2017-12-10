using Autodesk.Revit.DB;


namespace CommonMethod
{
    public class LineStyle
    {
        //设置线样式
        public static void SetLineStyle(Category cate, DetailCurve line)
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
        public static bool IsExistLineStyle(Document doc, string Name)
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
        public static Category BackLineStyle(Document doc)
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
    }
}
