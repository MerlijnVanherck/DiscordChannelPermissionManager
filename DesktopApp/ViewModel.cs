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

        public ObservableCollection<DiscordOverwrite> Overwrites { get; }
            = new ObservableCollection<DiscordOverwrite>();

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
            Overwrites.Clear();
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

        public void UpdateOverwritesCollection(DiscordNamedObject overwriteName)
        {
            Overwrites.Clear();

            foreach (var c in Channels)
                Overwrites.Add(
                    c.Overwrites.SingleOrDefault(o => o.Id == overwriteName.Id)
                    ?? new DiscordOverwrite(overwriteName.Id, overwriteName is DiscordRole, c.Type));
        }

        public async Task CommitChangedOverwrite(
            ulong guildId,
            Dictionary<ulong, DiscordOverwrite> overwriteList)
        {
            await DSharpPlusConnection.CommitChangedOverwrite(guildId, overwriteList);
            Console.WriteLine(guildId);

            var guild = Guilds.First(g => g.Id == guildId);

            foreach (var c in guild.Channels)
            {
                var index = c.Overwrites.FindIndex(o => o.Id == overwriteList[c.Id].Id);

                if (index == -1)
                    c.Overwrites.Add(overwriteList[c.Id]);
                else
                    c.Overwrites[index] = overwriteList[c.Id];
            }
        }
    }
}
