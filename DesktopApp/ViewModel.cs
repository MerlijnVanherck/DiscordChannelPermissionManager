using DiscordModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DesktopApp
{
    public class ViewModel
    {
        public ObservableCollection<DiscordGuild> Guilds { get; }
            = new ObservableCollection<DiscordGuild>();

        public ObservableCollection<DiscordNamedObject> OverwriteNames { get; }
            = new ObservableCollection<DiscordNamedObject>();

        public ObservableCollection<DiscordChannel> Channels { get; }
            = new ObservableCollection<DiscordChannel>();

        public async Task Connect(string token)
        {
            Guilds.Clear();
            Console.WriteLine("Start connecting");
            await DSharpPlusConnection.Connect(token);
            Console.WriteLine("Finished connecting");
            UpdateGuildsCollection();
        }

        public async Task Disconnect()
        {
            Guilds.Clear();
            OverwriteNames.Clear();
            Channels.Clear();
            await DSharpPlusConnection.Disconnect();
        }

        public void UpdateGuildsCollection()
        {
            Guilds.Clear();

            foreach (var g in DSharpPlusConnection.Guilds)
                Guilds.Add(g);
        }

        public void UpdateOverwriteNamesCollection(DiscordGuild guild)
        {
            OverwriteNames.Clear();

            foreach (var o in guild.GetOrderedOverwriteNames())
                OverwriteNames.Add(o);
        }

        public void UpdateChannelsCollection(DiscordGuild guild)
        {
            Channels.Clear();

            foreach (var c in guild.GetOrderedChannels())
                Channels.Add(c);
        }

        public async Task CommitChangedOverwrite(
            ulong guildId,
            Dictionary<ulong, DiscordOverwrite> overwriteList)
        {
            await DSharpPlusConnection.CommitChangedOverwrite(overwriteList);

            var guild = Guilds.First(g => g.Id == guildId);

            foreach (var p in overwriteList)
            {
                var channel = guild.Channels.Find(c => c.Id == p.Key);
                var overwrite = channel.Overwrites.FindIndex(o => o.Id == p.Value.Id);
                channel.Overwrites[overwrite] = p.Value;
            }
        }

        public async Task RemoveOverwrite(
            ulong guildId,
            DiscordChannel channel,
            DiscordNamedObject overwriteName)
        {
            await DSharpPlusConnection.DeleteExistingOverwrite(channel.Id, overwriteName.Id);

            Guilds.First(g => g.Id == guildId)
                .Channels.First(c => c.Id == channel.Id)
                .Overwrites.RemoveAll(o => o.Id == overwriteName.Id);
        }

        public async Task AddOverwrite(
            ulong guildId,
            DiscordChannel channel,
            DiscordNamedObject overwriteName,
            bool isRole)
        {
            await DSharpPlusConnection.AddNewOverwrite(guildId, channel.Id, overwriteName.Id, isRole);

            Guilds.First(g => g.Id == guildId)
                .Channels.First(c => c.Id == channel.Id)
                .Overwrites.Add(new DiscordOverwrite(overwriteName.Id, isRole, channel.Type));
        }
    }
}
