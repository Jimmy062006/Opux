using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Opux2
{
    public class GlobalCommands : ModuleBase
    {
        [Command("help", RunMode = RunMode.Async), Summary("Reports help text.")]
        public async Task Help()
        {
            foreach (var c in Base.Commands.Modules)
            {
                var start = $"{Context.User.Mention}, {Environment.NewLine}";
                if (c.IsSubmodule)
                {
                    foreach (var command in c.Commands)
                    {
                        var com = $"```Name: {command.Name}, Summary:{command.Summary}```{Environment.NewLine}";
                        await ReplyAsync($"{com}");
                    }
                }
                else
                {

                    foreach (var sub in c.Submodules)
                    {

                        foreach (var command in sub.Commands)
                        {
                            var com = $"```Name: {command.Name}, Summary:{command.Summary}```{Environment.NewLine}";
                            await ReplyAsync($"{com}");
                        }
                    }
                }
            }
        }

        [Command("stats", RunMode = RunMode.Async), Summary("Reports help text.")]
        public async Task Stats()
        {
            var start = $"{Context.User.Mention}, Welcome to Opux Help The following plugins are Enabled{Environment.NewLine}";
            var middle = "";
            foreach (var p in Base.Plugins)
            {
                middle += $"```Name: {p.Name}, Version: {p.Version}```{Environment.NewLine}";
            }
            await ReplyAsync($"{start}{middle}");
        }
    }
}
