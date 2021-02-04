using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordModel
{
    public enum DiscordPermission
    {
        ViewChannel = 0,
        ManageChannel = 1,
        ManagePermissions = 2,
        ManageWebhooks = 3,
        CreateInvite = 4,

        SendMessages = 5,
        SendTTS = 6,
        ManageMessages = 7,
        EmbedLinks = 8,
        AttachFiles = 9,
        ReadMessageHistory = 10,
        MentionEveryone = 11,
        UseExternalEmoji = 12,
        AddReactions = 13,

        Connect = 14,
        Speak = 15,
        Video = 16,
        MuteMembers = 17,
        DeafenMembers = 18,
        MoveMembers = 19,
        UseVoiceActivity = 20,
        PrioritySpeaker = 21
    }
}
