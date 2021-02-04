using System.Collections.Generic;

namespace DiscordModel
{
    public class DiscordChannel : DiscordNamedObject
    {
        public int Position { get; }
        public bool CanManage { get; }
        public DiscordChannelType Type { get; }
        public ulong? ParentId { get; }
        public List<DiscordOverwrite> Overwrites { get; }
            = new List<DiscordOverwrite>();

        public DiscordChannel(
            ulong id,
            string name,
            int position,
            bool canManage,
            DiscordChannelType type,
            ulong? parentId = null)
            : base(id, name)
        {
            Position = position;
            CanManage = canManage;
            Type = type;
            ParentId = parentId;
        }

        public override string ToString()
        {
            return Type switch
            {
                DiscordChannelType.Category => "> " + Name,
                DiscordChannelType.TextChannel => "# " + Name,
                DiscordChannelType.VoiceChannel => "🔊 " + Name,
                _ => Name,
            };
        }
    }
}