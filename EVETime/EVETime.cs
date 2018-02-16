using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.Threading.Tasks;

namespace eveTime
{
    public class EVETime : ModuleBase, IPlugin
    {
        [Command("time", RunMode = RunMode.Async), Summary("Returns the time in EVE")]
        public async Task Time()
        {
            await ReplyAsync($"Time In EVE is Currently {DateTime.UtcNow}");
        }

        public string Name => "EveTime";

        public string Description => "Gets and displays the EVE time";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            await Base.Commands.AddModuleAsync(GetType());
            await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
        }

        public async Task Pulse()
        {
            await Task.CompletedTask;
        }
    }
}
