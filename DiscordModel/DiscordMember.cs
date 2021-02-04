using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordModel
{
    public class DiscordMember : DiscordNamedObject
    {

        public DiscordMember(ulong id, string name) : base(id, name)
        {
        }
    }
}
