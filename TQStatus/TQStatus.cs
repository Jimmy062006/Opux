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
        static DateTime lastRun = new DateTime();
        StatusApi status = new StatusApi();

        internal static bool _Running = false;
        internal static bool VIP = false;
        internal static string version = "";
        internal static DateTime starttime = new DateTime();

        [Command("status", RunMode = RunMode.Async), Summary("Gets and displays the status of the EVE server")]
        public async Task Status()
        {
            var result = await status.GetStatusAsyncWithHttpInfo();

            if (result.StatusCode == 200)
            {
                var Players = result.Data.Players;
                var ServerVersion = result.Data.ServerVersion;

                var builder = new EmbedBuilder()
                    .WithColor(new Color(0x00D000))
                    .WithAuthor(author =>
                    {
                        author
                            .WithName($"EVE Online Server Status");
                    })
                    .AddInlineField("Players Online:", $"{Players}")
                    .AddInlineField("Version", $"{ServerVersion}")
                    .AddInlineField("StartTime", $"{starttime}");

                builder.WithTimestamp(DateTime.UtcNow);

                var embed = builder.Build();

                await ReplyAsync($"", false, embed).ConfigureAwait(false);
            }
        }

        public string Name => "TQStatus";

        public string Description => "Gets and displays the status of the EVE server";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            await Base.Commands.AddModuleAsync(GetType());
            await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
        }

        public async Task Pulse()
        {
            try
            {
                if ( !_Running && DateTime.UtcNow > lastRun.AddSeconds(30))
                {
                    _Running = true;
                    await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Running EVE Server Status Check"));
                    //Needs settings file for these
                    var channelRaw = Base.Configuration.GetSection("channelid");
                    var guildidRaw = Base.Configuration.GetSection("guildid");

                    if (channelRaw.Value != null & guildidRaw.Value != null)
                    {
                        UInt64.TryParse(channelRaw.Value, out ulong channelid);
                        UInt64.TryParse(guildidRaw.Value, out ulong guildid);

                        var textchannel = Base.DiscordClient.GetGuild(guildid).GetTextChannel(channelid);

                        var result = await status.GetStatusAsyncWithHttpInfo();
                        if (result.StatusCode == 200)
                        {


                            if (VIP != (result.Data.Vip??false) || version != result.Data.ServerVersion || result.Data.StartTime > starttime.AddMinutes(1))
                            {
                                VIP = result.Data.Vip??false;
                                version = result.Data.ServerVersion;
                                starttime = result.Data.StartTime ?? DateTime.MinValue;

                                var builder = new EmbedBuilder()
                                    .WithColor(new Color(0x00D000))
                                    .WithAuthor(author =>
                                    {
                                        author
                                            .WithName($"EVE Sever status changed");
                                    })
                                    .AddInlineField("Status", "Online")
                                    .AddInlineField("Players", $"{result.Data.Players}");
                                if (VIP)
                                    builder.AddInlineField("VIP", "VIP Mode Only!!");

                                builder.WithTimestamp(DateTime.UtcNow);

                                var embed = builder.Build();

                                await textchannel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            var builder = new EmbedBuilder()
                                .WithColor(new Color(0x00D000))
                                .WithAuthor(author =>
                                {
                                    author
                                        .WithName($"EVE Sever status changed");
                                })
                                .AddInlineField("Status", "Offline")
                                .AddInlineField("Players", $"Offline");

                            var embed = builder.Build();
                        }
                        lastRun = DateTime.UtcNow;
                        _Running = false;
                    }
                    else
                    {
                        await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, Name, $"ChannelID or GuildID is not set in main settings. EVEStatus Disabled."));
                    }
                }
            }
            catch (Exception ex)
            {
                lastRun = DateTime.UtcNow;
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"{ex.Message}"));
                _Running = false;
            }
            await Task.CompletedTask;
        }
    }
}
