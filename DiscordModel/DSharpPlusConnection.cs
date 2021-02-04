using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordModel
{
    public static class DSharpPlusConnection
    {
        private static DSharpPlus.DiscordClient Client { get; set; }
        public static List<DiscordGuild> Guilds { get; set; }
            = new List<DiscordGuild>();

        public static async Task Connect(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("No token provided.");

            Client = new DSharpPlus.DiscordClient(new DSharpPlus.DiscordConfiguration()
            {
                Token = token,
                Intents = DSharpPlus.DiscordIntents.AllUnprivileged
            });

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
        }

        private static Task GuildDownloadCompleted(
            DSharpPlus.DiscordClient sender,
            DSharpPlus.EventArgs.GuildDownloadCompletedEventArgs e)
        {
            return Task.Run(ConstructDiscordGuildCollection);
        }

        private static async Task ConstructDiscordGuildCollection()
        {
            foreach (var g in Client.Guilds.Values)
            {
                var guild = ConvertGuildFromDSharpPlus(g);
                Guilds.Add(guild);

                foreach (var c in g.Channels.Values)
                {
                    guild.Channels.Add(
                        ConvertChannelFromDSharpPlus(c, g.CurrentMember));

                    foreach (var o in c.PermissionOverwrites)
                        if (o.Type == DSharpPlus.OverwriteType.Member)
                            guild.Members.Add(await ConvertMemberFromDSharpPlus(o.Id));
                }

                var botRolePosition = g.CurrentMember.Hierarchy;

                foreach (var r in g.Roles.Values)
                    guild.Roles.Add(ConvertRoleFromDSharpPlus(r, botRolePosition));
            }
        }

        private static DiscordGuild ConvertGuildFromDSharpPlus(
            DSharpPlus.Entities.DiscordGuild guild)
        {
            return new DiscordGuild(
                guild.Id,
                guild.Name,
                guild.Permissions.Value.HasPermission(
                    DSharpPlus.Permissions.ManageChannels)
                );
        }

        public static DiscordRole ConvertRoleFromDSharpPlus(
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
                channelType
                );

            foreach (var o in channel.PermissionOverwrites)
                discordChannel.Overwrites.Add(
                    ConvertDiscordOverwriteFromDSharpPlus(o, channelType));

            return discordChannel;
        }

        private static async Task<DiscordMember> ConvertMemberFromDSharpPlus(
           ulong id)
        {
            return new DiscordMember(
                id,
                (await Client.GetUserAsync(id)).Username
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
                _ => DiscordChannelType.Unknown,
            };
        }

        private static DiscordOverwrite ConvertDiscordOverwriteFromDSharpPlus(
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
