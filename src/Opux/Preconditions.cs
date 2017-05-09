using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Opux
{

    public class CheckForRoleAttribute : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider map)
        {
            var roles = new List<IRole>(context.Guild.Roles);
            var userRoleIDs = context.Guild.GetUserAsync(context.User.Id).Result.RoleIds;
            var roleMatch = Program.Settings.GetSection("config").GetSection("adminRoles").GetChildren();
            foreach (var role in roleMatch)
            {
                var tmp = roles.FirstOrDefault(x => x.Name == role.Key);
                if (tmp != null)
                {
                    var check = userRoleIDs.FirstOrDefault(x => x == tmp.Id);
                    if (check != 0)
                    {
                        await Task.CompletedTask;
                        return PreconditionResult.FromSuccess();
                    }
                }
            }
            return PreconditionResult.FromError("You must be the owner of the bot to run this command.");
        }
    }
}
