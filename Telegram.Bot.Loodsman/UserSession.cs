using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Bot.Loodsman
{
    class UserSession
    {
        public int UserId { get; set; }
        public Loodsman Connection { get; set; }        
        public string LastCommand { get; set; }
        public Task CurrentTask { get; set; }

        public LoodsmanObject CurrentObject { get; set; }
    }
}
