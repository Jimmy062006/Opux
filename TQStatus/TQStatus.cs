using Discord;
using Discord.Commands;
using ESIClient.Api;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.Threading.Tasks;

namespace tqStatus
{
    public class TQStatus : ModuleBase, IPlugin
    {
        StatusApi status = new StatusApi();

        static DateTime lastRun { get; set; }
        static bool _Running { get; set; }
        static bool _FirstRunDone { get; set; }
        static bool _VIP { get; set; }
        static string _Version { get; set; }
        static DateTime _Starttime { get; set; }

        [Command("status", RunMode = RunMode.Async), Summary("Gets and displays the status of the EVE server")]
        public async Task Status()
        {
            var result = await status.GetStatusAsyncWithHttpInfo();

            if (result.StatusCode == 200)
            {
                var Players = result.Data.Players;
                var ServerVersion = result.Data.ServerVersion;
                var StartTime = result.Data.StartTime;

                var builder = new EmbedBuilder()
                    .WithColor(new Color(0x00D000))
                    .WithAuthor(author =>
                    {
                        author
                            .WithName($"EVE Online Server Status");
                    })
                    .AddInlineField("Players Online:", $"{Players}")
                    .AddInlineField("Version", $"{ServerVersion}")
                    .AddInlineField("StartTime", $"{_Starttime}");

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

                    var channelRaw = Base.Configuration.GetValue<string>("channelid");
                    var guildidRaw = Base.Configuration.GetValue<string>("guildid");

                    if (channelRaw != null & guildidRaw != null)
                    {
                        UInt64.TryParse(channelRaw, out ulong channelid);
                        UInt64.TryParse(guildidRaw, out ulong guildid);

                        var textchannel = Base.DiscordClient.GetGuild(guildid).GetTextChannel(channelid);

                        var status = await this.status.GetStatusAsyncWithHttpInfo();
                        if (status.StatusCode == 200 && Convert.ToInt16(status.Headers["X-Esi-Error-Limit-Remain"]) > 10)
                        {
                            if (_FirstRunDone)
                            {
                                if (_VIP != (status.Data.Vip ?? false) || _Version != status.Data.ServerVersion || status.Data.StartTime > _Starttime.AddMinutes(1))
                                {
                                    _VIP = status.Data.Vip ?? false;
                                    _Version = status.Data.ServerVersion;
                                    _Starttime = status.Data.StartTime ?? DateTime.MinValue;

                                    var builder = new EmbedBuilder()
                                        .WithColor(new Color(0x00D000))
                                        .WithAuthor(author =>
                                        {
                                            author
                                                .WithName($"EVE Sever status changed");
                                        })
                                        .AddInlineField("Status", "Online")
                                        .AddInlineField("Players", $"{status.Data.Players}");
                                    if (_VIP)
                                        builder.AddInlineField("VIP", "VIP Mode Only!!");

                                    builder.WithTimestamp(DateTime.UtcNow);

                                    var embed = builder.Build();

                                    await textchannel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                                }
                            }
                            else if (!_FirstRunDone)
                            {
                                _VIP = status.Data.Vip ?? false;
                                _Version = status.Data.ServerVersion;
                                _Starttime = status.Data.StartTime ?? DateTime.MinValue;

                                _FirstRunDone = true;
                                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"EVE Server Status Check Active"));
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
