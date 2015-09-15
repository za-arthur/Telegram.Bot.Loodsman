using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Bot.Loodsman
{
    class LoodsmanFile
    {
        public string Name { get; set; }
        public string LocalName { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
