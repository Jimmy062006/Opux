using Discord;
using Discord.Commands;
using ESIClient.Model;
using Newtonsoft.Json;
using Opux2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace priceChecks
{
    public class PriceChecks : ModuleBase, IPlugin
    {
        [Command("pc", RunMode = RunMode.Async), Summary("Use !pc itemname to get the Global Price for the item")]
        public async Task Pc([Remainder] string x)
        {
            await PriceCheck(null, x, Context);
        }

        [Command("jita", RunMode = RunMode.Async), Summary("Use !pc itemname to get the Global Price for the item")]
        public async Task Jita([Remainder] string x)
        {
            await PriceCheck("Jita", x, Context);
        }

        [Command("dodixie", RunMode = RunMode.Async), Summary("Use !pc itemname to get the Global Price for the item")]
        public async Task Dodixie([Remainder] string x)
        {
            await PriceCheck("Dodixie", x, Context);
        }

        [Command("rens", RunMode = RunMode.Async), Summary("Use !pc itemname to get the Global Price for the item")]
        public async Task Rens([Remainder] string x)
        {
            await PriceCheck("Rens", x, Context);
        }

        public string Name => "PriceChecks";

        public string Description => "Plugin will get prices for items from evemarketer's API";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            await Base.Commands.AddModuleAsync(GetType());
            await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
        }

        public Task Pulse()
        {
            return Task.CompletedTask;
        }

        internal async static Task PriceCheck(string System, string String, ICommandContext Context)
        {
            try
            {
                var channel = Context.Channel;

                var UniverseApi = new ESIClient.Api.UniverseApi();
                var IDs = new List<string> { String };

                if (!String.IsNullOrWhiteSpace(System))
                    IDs.Add(System);

                var IDstoNames = UniverseApi.PostUniverseIds(IDs);

                if (IDstoNames.InventoryTypes == null)
                {
                    await channel.SendMessageAsync($"{Context.User.Mention}, {String} was not found", false, null).ConfigureAwait(false);
                    return;
                }

                var ItemType = IDstoNames.InventoryTypes.FirstOrDefault(x => x.Name.ToLower() == String.ToLower());

                PostUniverseIdsSystem SystemName = new PostUniverseIdsSystem { Name = "Global", Id = 0 };

                if (IDstoNames.Systems != null && IDstoNames.Systems.Count > 0)
                    SystemName = IDstoNames.Systems.FirstOrDefault(x => x.Name == System);

                var url = "https://api.evemarketer.com/ec";

                var eveCentralReply = "";

                if (System == null)
                     eveCentralReply = await Base._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemType.Id}");
                else
                    eveCentralReply = await Base._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemType.Id}&usesystem={SystemName.Id}");

                var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {Context.Message.Author}'s Price check to {channel.Name}"));
                var builder = new EmbedBuilder()
                    .WithColor(new Color(0x00D000))
                    .WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemType.Id}_64.png")
                    .WithAuthor(author =>
                    {
                        author
                            .WithName($"Item: {ItemType.Name}")
                            .WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemType.Id}/")
                            .WithIconUrl("https://just4dns2.co.uk/shipexplosion.png");
                    })
                    .WithDescription($"{SystemName.Name} Prices")
                    .AddInlineField("Buy", $"Low: {centralreply.buy.min.ToString("N2")}{Environment.NewLine}" +
                    $"Avg: {centralreply.buy.avg.ToString("N2")}{Environment.NewLine}" +
                    $"High: {centralreply.buy.max.ToString("N2")}")
                    .AddInlineField("Sell", $"Low: {centralreply.sell.min.ToString("N2")}{Environment.NewLine}" +
                    $"Avg: {centralreply.sell.avg.ToString("N2")}{Environment.NewLine}" +
                    $"High: {centralreply.sell.max.ToString("N2")}")
                    .AddField($"Extra Data", $"\u200b")
                    .AddInlineField("Buy", $"5%: {centralreply.buy.fivePercent.ToString("N2")}{Environment.NewLine}" +
                    $"Volume: {centralreply.buy.volume}")
                    .AddInlineField("Sell", $"5%: {centralreply.sell.fivePercent.ToString("N2")}{Environment.NewLine}" +
                    $"Volume: {centralreply.sell.volume.ToString("N0")}");
                var embed = builder.Build();

                await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Critical, "PriceCheck", ex.Message, ex));
            }
        }

    }

    #region JsonClasses
    public class Items
    {
        public Buy buy { get; set; }
        public All all { get; set; }
        public Sell sell { get; set; }
    }

    public class Buy
    {
        public Forquery forQuery { get; set; }
        public Int64 volume { get; set; }
        public float wavg { get; set; }
        public float avg { get; set; }
        public float variance { get; set; }
        public float stdDev { get; set; }
        public float median { get; set; }
        public float fivePercent { get; set; }
        public float max { get; set; }
        public float min { get; set; }
        public bool highToLow { get; set; }
        public long generated { get; set; }
    }

    public class All
    {
        public Forquery1 forQuery { get; set; }
        public int volume { get; set; }
        public float wavg { get; set; }
        public float avg { get; set; }
        public float variance { get; set; }
        public float stdDev { get; set; }
        public float median { get; set; }
        public float fivePercent { get; set; }
        public float max { get; set; }
        public float min { get; set; }
        public bool highToLow { get; set; }
        public long generated { get; set; }
    }

    public class Forquery
    {
        public bool Bid { get; set; }
        public int[] Types { get; set; }
        public object[] Regions { get; set; }
        public object[] Systems { get; set; }
        public int Hours { get; set; }
        public int Minq { get; set; }
    }

    public class Forquery1
    {
        public object bid { get; set; }
        public int[] types { get; set; }
        public object[] regions { get; set; }
        public object[] systems { get; set; }
        public int hours { get; set; }
        public int minq { get; set; }
    }

    public class Sell
    {
        public Forquery2 forQuery { get; set; }
        public Int64 volume { get; set; }
        public float wavg { get; set; }
        public float avg { get; set; }
        public float variance { get; set; }
        public float stdDev { get; set; }
        public float median { get; set; }
        public float fivePercent { get; set; }
        public float max { get; set; }
        public float min { get; set; }
        public bool highToLow { get; set; }
        public long generated { get; set; }
    }

    public class Forquery2
    {
        public bool bid { get; set; }
        public int[] types { get; set; }
        public object[] regions { get; set; }
        public object[] systems { get; set; }
        public int hours { get; set; }
        public int minq { get; set; }
    }
    #endregion
}
