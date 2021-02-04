
namespace DiscordModel
{
    public class DiscordRole : DiscordNamedObject
    {
        public int Position { get; }
        public bool CanManage { get; }

        public DiscordRole(ulong id, string name, int position, bool canManage)
            : base(id, name)
        {
            Position = position;
            CanManage = canManage;
        }
    }
}
