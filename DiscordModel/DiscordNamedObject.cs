using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordModel
{
    public class DiscordNamedObject
    {
        public ulong Id { get; }
        public string Name { get; }

        public DiscordNamedObject(ulong id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return "@" + Name;
        }
    }
}
