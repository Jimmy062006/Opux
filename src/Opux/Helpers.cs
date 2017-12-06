using Discord.Commands;

namespace Opux
{
    class Helpers
    {
        internal static bool IsUserMention(ICommandContext context)
        {
            if(context.Message.MentionedUserIds.Count != 0)
            {
                return true;
            }
            return false;
        }
    }
}
