using Discord;
using Discord.Commands;
using ESIClient.Api;
using Opux2;
using System;
using System.Threading.Tasks;

namespace tqStatus
{
    public class TQStatus : ModuleBase, IPlugin
    {
        static bool online = false;
        static DateTime lastRun = new DateTime();
        StatusApi status = new StatusApi();

        [Command("status", RunMode = RunMode.Async), Summary("Gets and displays the status of the eve server")]
        public async Task Status()
        {
            var result = await status.GetStatusAsync();

            if (result != null)
            {
                var builder = new EmbedBuilder()
                    .WithColor(new Color(0x00D000))
                    .WithAuthor(author =>
                    {
                        author
                            .WithName($"EVE Online Server Status");
                    })
                    .AddInlineField("Players Online:", $"{result.Players}")
                    .AddInlineField("Version", $"{result.ServerVersion}")
                    .AddInlineField("StartTime", $"{result.StartTime}");

                var embed = builder.Build();

                await ReplyAsync($"", false, embed).ConfigureAwait(false);
            }
        }

        public string Name => "TQStatus";

        public string Description => "Gets and displays the status of the eve server";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            await Base.Commands.AddModuleAsync(GetType());
            await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
        }

        public async Task Pulse()
        {
            if (DateTime.UtcNow > lastRun.AddSeconds(30))
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Running EVE Server Status Check"));
                //Needs settings file for these
                var channel = 396445945501319188U;
                var guildid = 161913760544456704U;

                var textchannel = Base.DiscordClient.GetGuild(guildid).GetTextChannel(channel);

                var result = await status.GetStatusAsync();
                var VIP = result.Vip != null ? "Active" : "Inactive";

                if (result == null && online)
                {
                    online = false;

                    var builder = new EmbedBuilder()
                        .WithColor(new Color(0x00D000))
                        .WithAuthor(author =>
                        {
                            author
                                .WithName($"EVE Sever status changed");
                        })
                        .AddField("Status", "Offline");

                    var embed = builder.Build();

                    await textchannel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                }
                if (result != null && !online)
                {
                    online = true;

                    var builder = new EmbedBuilder()
                        .WithColor(new Color(0x00D000))
                        .WithAuthor(author =>
                        {
                            author
                                .WithName($"EVE Sever status changed");
                        })
                        .AddInlineField("Status", "Online")
                        .AddInlineField("Players", $"{result.Players}")
                        .AddInlineField("VIP", VIP);

                    var embed = builder.Build();

                    await textchannel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                }
                lastRun = DateTime.UtcNow;
            }

            await Task.CompletedTask;
        }
    }
}
