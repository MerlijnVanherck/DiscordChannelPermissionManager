using System.Collections.Generic;

namespace DiscordModel
{
    public class DiscordOverwrite
    {
        public ulong Id { get; }
        public bool IsRole { get; }
        public Dictionary<DiscordPermission, bool?> Permission { get; }
            = new Dictionary<DiscordPermission, bool?>();

        public DiscordOverwrite(ulong id, bool isRole, DiscordChannelType type)
        {
            Id = id;
            IsRole = isRole;
            Permission.Add(DiscordPermission.SendMessages, null);
            Permission.Add(DiscordPermission.SendTTS, null);
            Permission.Add(DiscordPermission.ManageMessages, null);
            Permission.Add(DiscordPermission.EmbedLinks, null);
            Permission.Add(DiscordPermission.AttachFiles, null);

            if (type is not DiscordChannelType.VoiceChannel)
            {
                Permission.Add(DiscordPermission.SendMessages, null);
                Permission.Add(DiscordPermission.SendTTS, null);
                Permission.Add(DiscordPermission.ManageMessages, null);
                Permission.Add(DiscordPermission.EmbedLinks, null);
                Permission.Add(DiscordPermission.AttachFiles, null);
                Permission.Add(DiscordPermission.ReadMessageHistory, null);
                Permission.Add(DiscordPermission.MentionEveryone, null);
                Permission.Add(DiscordPermission.UseExternalEmoji, null);
                Permission.Add(DiscordPermission.AddReactions, null);
            }

            if (type is not DiscordChannelType.TextChannel)
            {
                Permission.Add(DiscordPermission.Connect, null);
                Permission.Add(DiscordPermission.Speak, null);
                Permission.Add(DiscordPermission.Video, null);
                Permission.Add(DiscordPermission.MuteMembers, null);
                Permission.Add(DiscordPermission.DeafenMembers, null);
                Permission.Add(DiscordPermission.MoveMembers, null);
                Permission.Add(DiscordPermission.UseVoiceActivity, null);
                Permission.Add(DiscordPermission.PrioritySpeaker, null);
            }
        }
    }
}