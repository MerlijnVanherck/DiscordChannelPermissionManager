using System.Collections.Generic;
using System.Linq;

namespace DiscordModel
{
    public class DiscordGuild : DiscordNamedObject
    {
        public bool CanManagePermissions { get; }
        public List<DiscordRole> Roles { get; }
            = new List<DiscordRole>();
        public List<DiscordMember> Members { get; }
            = new List<DiscordMember>();
        public List<DiscordChannel> Channels { get; }
            = new List<DiscordChannel>();

        public DiscordGuild(ulong id, string name, bool canManagePermissions)
            : base (id, name)
        {
            CanManagePermissions = canManagePermissions;
        }

        public List<DiscordRole> GetOrderedRoles()
        {
            return Roles.OrderByDescending(r => r.Position).ToList();
        }

        public List<DiscordMember> GetOrderedMembers()
        {
            return Members.OrderBy(m => m.Name).ToList();
        }

        public List<DiscordChannel> GetOrderedChannels()
        {
            var channelList = new List<DiscordChannel>();

            foreach (DiscordChannel c1 in Channels
                .FindAll(c => c.Type == DiscordChannelType.Category)
                .OrderBy(c => c.Position))
            {
                channelList.Add(c1);

                foreach (DiscordChannel c2 in Channels
                    .FindAll(c => c.ParentId == c1.Id)
                    .OrderBy(c => c.Position)
                    .ThenBy(c => c.Type))
                    channelList.Add(c2);
            }

            return channelList;
        }

        public List<DiscordNamedObject> GetOrderedOverwrites()
        {
            var list = new List<DiscordNamedObject>();

            foreach (var r in GetOrderedRoles())
                list.Add(r);

            foreach (var m in Members.OrderBy(m => m.Name))
                list.Add(m);

            return list;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}