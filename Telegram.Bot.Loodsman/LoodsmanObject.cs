using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Bot.Loodsman
{
    class LoodsmanObject
    {
        private List<LoodsmanObject> childs = new List<LoodsmanObject>();
        private List<LoodsmanFile> files = new List<LoodsmanFile>();

        public int Id { get; set; }
        public string Product { get; set; }
        public List<LoodsmanObject> Childs { get { return childs; } }
        public List<LoodsmanFile> Files { get { return files; } }

        public override string ToString()
        {
            return Product;
        }
    }

    static class LoodsmanObjectChildsExtension
    {
        public static string ToStringEx(this List<LoodsmanObject> obj)
        {
            return string.Join(Environment.NewLine, obj);
        }

        public static string[][] ToKeyboard(this List<LoodsmanObject> obj)
        {
            return obj.Select(o => new string[] {o.ToString()}).ToArray();
        }
    }

    static class LoodsmanObjectFilesExtension
    {
        public static string ToStringEx(this List<LoodsmanFile> files)
        {
            return string.Join(Environment.NewLine, files);
        }

        public static string[][] ToKeyboard(this List<LoodsmanFile> files)
        {
            return files.Select(f => new string[] { f.ToString() }).ToArray();
        }
    }
}
