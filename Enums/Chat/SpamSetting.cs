using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChatBot.Enums.Chat
{
    enum SpamSetting
    {
        None = 0,
        enabled,
        permission,
        timeouts,
        Wall,
        ASCII,
        Links,
        Caps,
    }
}
