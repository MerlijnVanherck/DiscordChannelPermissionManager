using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DiscordModel
{
    public static class DSharpPlusConnection
    {
        private static DSharpPlus.DiscordClient Client;

        public static List<DiscordGuild> Guilds { get; } = new List<DiscordGuild>();

        private static TaskCompletionSource OperationTracker;

        public static Task ConnectFinished => OperationTracker.Task;

        public static async Task Connect(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("No token provided.");

            Client = new DSharpPlus.DiscordClient(new DSharpPlus.DiscordConfiguration()
            {
                Token = token,
                Intents = DSharpPlus.DiscordIntents.Guilds,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning
            });

            OperationTracker = new TaskCompletionSource();
            Guilds.Clear();

            try
            {
                Client.GuildDownloadCompleted += GuildDownloadCompleted;
                await Client.ConnectAsync();
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                Client = null;
                throw new ArgumentException("Invalid token provided.");
            }

            await ConnectFinished;
        }

        public static async Task Disconnect()
        {
            await Client.DisconnectAsync();
        }

        public static async Task CommitChangedOverwrite(ulong guildId, Dictionary<ulong, DiscordOverwrite> overwriteList)
        {
            var dspGuild = await Client.GetGuildAsync(guildId);
            
            foreach (var (channelId, overwrite) in overwriteList)
            {
                var (allow, deny) = ConvertOverwriteToDSharpPlus(overwrite);

                var dspChannel = await Client.GetChannelAsync(channelId);
                var dspOverwrite = dspChannel.PermissionOverwrites.FirstOrDefault(o => o.Id == overwrite.Id);

                var oldOverwriteExists = dspOverwrite is not null;
                var newOverwriteExists = allow != Permissions.None || deny != Permissions.None;

                if (oldOverwriteExists && newOverwriteExists)
                    await UpdateExistingOverwrite(dspOverwrite, allow, deny);
                else if (!oldOverwriteExists && newOverwriteExists)
                    await AddNewOverwrite(dspGuild, dspChannel, allow, deny, overwrite);
                else if (oldOverwriteExists && !newOverwriteExists)
                    await DeleteExistingOverwrite(dspOverwrite);
            }
        }

        private static async Task AddNewOverwrite(
            DSharpPlus.Entities.DiscordGuild guild,
            DSharpPlus.Entities.DiscordChannel channel,
            DSharpPlus.Permissions allow,
            DSharpPlus.Permissions deny,
            DiscordOverwrite overwrite)
        {
            if (overwrite.IsRole)
                await AddNewRoleOverwrite(guild, channel, allow, deny, overwrite);
            else
                await AddNewMemberOverwrite(guild, channel, allow, deny, overwrite);
        }

        private static async Task AddNewMemberOverwrite(
            DSharpPlus.Entities.DiscordGuild guild,
            DSharpPlus.Entities.DiscordChannel channel,
            DSharpPlus.Permissions allow,
            DSharpPlus.Permissions deny,
            DiscordOverwrite overwrite)
        {

            var member = await guild.GetMemberAsync(overwrite.Id);
            await channel.AddOverwriteAsync(member, allow, deny);
        }

        private static async Task AddNewRoleOverwrite(
            DSharpPlus.Entities.DiscordGuild guild,
            DSharpPlus.Entities.DiscordChannel channel,
            DSharpPlus.Permissions allow,
            DSharpPlus.Permissions deny,
            DiscordOverwrite overwrite)
        {
            var role = guild.GetRole(overwrite.Id);
            await channel.AddOverwriteAsync(role, allow, deny);
        }

        private static async Task UpdateExistingOverwrite(
            DSharpPlus.Entities.DiscordOverwrite overwrite,
            DSharpPlus.Permissions allow,
            DSharpPlus.Permissions deny)
        {
            await overwrite.UpdateAsync(allow, deny);
        }

        private static async Task DeleteExistingOverwrite(
            DSharpPlus.Entities.DiscordOverwrite overwrite)
        {
            await overwrite.DeleteAsync();
        }

        private static Task GuildDownloadCompleted(
            DSharpPlus.DiscordClient sender,
            DSharpPlus.EventArgs.GuildDownloadCompletedEventArgs e)
        {
            Task.Run(() => ConstructDiscordGuildCollection())
                .ContinueWith((a) => OperationFinished(a));

            return Task.CompletedTask;
        }

        private static void OperationFinished(Task t)
        {
            if (t.IsFaulted)
                OperationTracker.SetException(t.Exception);

            OperationTracker.SetResult();
        }

        private static async Task ConstructDiscordGuildCollection()
        {
            foreach (var g in Client.Guilds.Values)
            {
                var guild = ConvertGuildFromDSharpPlus(g, g.CurrentMember);
                Guilds.Add(guild);

                foreach (var c in g.Channels.Values)
                {
                    guild.Channels.Add(
                        ConvertChannelFromDSharpPlus(c, g.CurrentMember));

                    foreach (var o in c.PermissionOverwrites)
                        if (o.Type == DSharpPlus.OverwriteType.Member
                            && !guild.Members.Exists(m => m.Id == o.Id))
                            guild.Members.Add(await ConvertMemberFromDSharpPlus(o.Id));
                }

                var botRolePosition = g.CurrentMember.Hierarchy;

                foreach (var r in g.Roles.Values)
                    guild.Roles.Add(ConvertRoleFromDSharpPlus(r, botRolePosition));
            }
        }

        private static DiscordGuild ConvertGuildFromDSharpPlus(
            DSharpPlus.Entities.DiscordGuild guild,
            DSharpPlus.Entities.DiscordMember member)
        {
            var perms = member.Roles
                   .Select(r => r.Permissions)
                   .Aggregate((p1, p2) => p1 | p2);

            return new DiscordGuild(
                guild.Id,
                guild.Name,
                perms.HasPermission(DSharpPlus.Permissions.Administrator)
                );
        }

        private static DiscordRole ConvertRoleFromDSharpPlus(
            DSharpPlus.Entities.DiscordRole role,
            int botRolePosition)
        {
            return new DiscordRole(
                role.Id,
                role.Name,
                role.Position,
                role.Position < botRolePosition);
        }

        private static DiscordChannel ConvertChannelFromDSharpPlus(
            DSharpPlus.Entities.DiscordChannel channel,
            DSharpPlus.Entities.DiscordMember member)
        {
            var channelType = ConvertChannelTypeFromDSharpPlus(channel.Type);

            var discordChannel = new DiscordChannel(
                channel.Id,
                channel.Name,
                channel.Position,
                channel.PermissionsFor(member).HasPermission(
                    DSharpPlus.Permissions.ManageRoles),
                channelType,
                channel.ParentId
                );

            foreach (var o in channel.PermissionOverwrites)
                discordChannel.Overwrites.Add(
                    ConvertOverwriteFromDSharpPlus(o, channelType));

            return discordChannel;
        }

        private static async Task<DiscordMember> ConvertMemberFromDSharpPlus(
           ulong id)
        {
            var member = await Client.GetUserAsync(id);
            return new DiscordMember(
                id,
                member.Username + "#" + member.Discriminator
                );
        }

        private static DiscordChannelType ConvertChannelTypeFromDSharpPlus(
            DSharpPlus.ChannelType type)
        {
            return type switch
            {
                DSharpPlus.ChannelType.Category => DiscordChannelType.Category,
                DSharpPlus.ChannelType.Text => DiscordChannelType.TextChannel,
                DSharpPlus.ChannelType.Voice => DiscordChannelType.VoiceChannel,
                DSharpPlus.ChannelType.News => DiscordChannelType.TextChannel,
                _ => DiscordChannelType.Unknown,
            };
        }

        private static DiscordOverwrite ConvertOverwriteFromDSharpPlus(
            DSharpPlus.Entities.DiscordOverwrite overwrite,
            DiscordChannelType type)
        {
            var discordOverwrite = new DiscordOverwrite(
                overwrite.Id,
                overwrite.Type == DSharpPlus.OverwriteType.Role,
                type);

            var keys = new List<DiscordPermission>(discordOverwrite.Permission.Keys);

            foreach (DiscordPermission p in keys)
                discordOverwrite.Permission[p] = ConvertBoolFromDSharpPlusPermissionLevel(
                    overwrite.CheckPermission(ConvertPermissionToDSharpPlus(p)));

            return discordOverwrite;
        }

        private static (DSharpPlus.Permissions, DSharpPlus.Permissions) ConvertOverwriteToDSharpPlus(
           DiscordOverwrite overwrite)
        {
            var dspOverwrite = new DSharpPlus.Entities.DiscordOverwriteBuilder();

            foreach (var (perm, b) in overwrite.Permission)
            {
                if (b == true)
                    dspOverwrite.Allow(ConvertPermissionToDSharpPlus(perm));
                else if (b == false)
                    dspOverwrite.Deny(ConvertPermissionToDSharpPlus(perm));
            }

            return (dspOverwrite.Allowed, dspOverwrite.Denied);
        }

        private static bool? ConvertBoolFromDSharpPlusPermissionLevel(
            DSharpPlus.PermissionLevel level)
        {
            return level switch
            {
                DSharpPlus.PermissionLevel.Allowed => true,
                DSharpPlus.PermissionLevel.Denied => false,
                _ => null,
            };
        }

        private static DSharpPlus.Permissions ConvertPermissionToDSharpPlus(
            DiscordPermission permissions)
        {
            return permissions switch
            {
                DiscordPermission.ViewChannel => DSharpPlus.Permissions.AccessChannels,
                DiscordPermission.ManageChannel => DSharpPlus.Permissions.ManageChannels,
                DiscordPermission.ManagePermissions => DSharpPlus.Permissions.ManageRoles,
                DiscordPermission.ManageWebhooks => DSharpPlus.Permissions.ManageWebhooks,
                DiscordPermission.CreateInvite => DSharpPlus.Permissions.CreateInstantInvite,

                DiscordPermission.SendMessages => DSharpPlus.Permissions.SendMessages,
                DiscordPermission.SendTTS => DSharpPlus.Permissions.SendTtsMessages,
                DiscordPermission.ManageMessages => DSharpPlus.Permissions.ManageMessages,
                DiscordPermission.EmbedLinks => DSharpPlus.Permissions.EmbedLinks,
                DiscordPermission.AttachFiles => DSharpPlus.Permissions.AttachFiles,
                DiscordPermission.ReadMessageHistory => DSharpPlus.Permissions.ReadMessageHistory,
                DiscordPermission.MentionEveryone => DSharpPlus.Permissions.MentionEveryone,
                DiscordPermission.UseExternalEmoji => DSharpPlus.Permissions.UseExternalEmojis,
                DiscordPermission.AddReactions => DSharpPlus.Permissions.AddReactions,

                DiscordPermission.Connect => DSharpPlus.Permissions.UseVoice,
                DiscordPermission.Speak => DSharpPlus.Permissions.Speak,
                DiscordPermission.Video => DSharpPlus.Permissions.Stream,
                DiscordPermission.MuteMembers => DSharpPlus.Permissions.MuteMembers,
                DiscordPermission.DeafenMembers => DSharpPlus.Permissions.DeafenMembers,
                DiscordPermission.MoveMembers => DSharpPlus.Permissions.MoveMembers,
                DiscordPermission.UseVoiceActivity => DSharpPlus.Permissions.UseVoiceDetection,
                DiscordPermission.PrioritySpeaker => DSharpPlus.Permissions.PrioritySpeaker,
                _ => throw new ArgumentException("Unknown permission overwrite."),
            };
        }
    }
}
