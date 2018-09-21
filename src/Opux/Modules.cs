using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Opux
{
    // Create a module with no prefix
    public partial class Info : ModuleBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("help", RunMode = RunMode.Async), Summary("Reports help text.")]
        public async Task Help()
        {
            var userInfo = Context.Message.Author;
            await ReplyAsync($"{userInfo.Mention}, Here is a list of plugins available, **!help | !about | !char | !corp | !jita | !amarr | !dodixie | !rens | !pc**");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("pc", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Pc([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["pricecheck"]))
            {
                var userInfo = Context.Message.Author;
                if (x == null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} please provide an item name");
                }
                else
                {
                    await Functions.PriceCheck(Context, x, "");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("jita", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !jita Tritanium")]
        public async Task Jita([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["pricecheck"]))
            { 
                var userInfo = Context.Message.Author;
                if (x == null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} please provide an item name");
                }
                else
                {
                    await Functions.PriceCheck(Context, x, "jita");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("amarr", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Amarr([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["pricecheck"]))
            {
                var userInfo = Context.Message.Author;
                if (x == null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} please provide an item name");
                }
                else
                {
                    await Functions.PriceCheck(Context, x, "amarr");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("rens", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Rens([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["pricecheck"]))
            {
                var userInfo = Context.Message.Author;
                if (x == null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} please provide an item name");
                }
                else
                {
                    await Functions.PriceCheck(Context, x, "rens");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("dodixie", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Dodixe([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["pricecheck"]))
            {
                var userInfo = Context.Message.Author;
                if (x == null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} please provide an item name");
                }
                else
                {
                    await Functions.PriceCheck(Context, x, "dodixie");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("rehash", RunMode = RunMode.Async), Summary("Rehash settings file")]
        [CheckForRole]
        public async Task Reshash()
        {
            try
            {
                await Program.UpdateSettings();
                await ReplyAsync($"{Context.Message.Author.Mention} REHASH COMPLETED");
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("reauth", RunMode = RunMode.Async), Summary("Reauth all users")]
        [CheckForRole]
        public async Task Reauth()
        {
            try
            {
                await Functions.AuthCheck(Context);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("auth", RunMode = RunMode.Async), Summary("Auth User")]
        public async Task Auth()
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["authWeb"]))
            {
                try
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} To Auth please visit {(string)Program.Settings.GetSection("auth")["authurl"]} and Login with your main");
                }
                catch (Exception ex)
                {
                    await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                    await Task.FromException(ex);
                }
            }
            else
            {
                await ReplyAsync($"{Context.Message.Author.Mention}, Auth is disabled on this server");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("auth", RunMode = RunMode.Async), Summary("Auth User")]
        public async Task Auth([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["authWeb"]))
            {
                try
                {
                    if (Helpers.IsUserMention(Context))
                    {
                        await Functions.SendAuthMessage(Context);
                    }
                    else
                    {
                        await Functions.AuthUser(Context, x);
                    }
                }
                catch (Exception ex)
                {
                    await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                    await Task.FromException(ex);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("evetime", RunMode = RunMode.Async), Summary("EVE TQ Time")]
        public async Task EveTime()
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["time"]))
            {
                try
                {
                    await Functions.EveTime(Context);
                }
                catch (Exception ex)
                {
                    await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                    await Task.FromException(ex);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("motd", RunMode = RunMode.Async), Summary("Shows MOTD")]
        public async Task Motd()
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["MOTD"]))
            {
                try
                {
                    await Functions.MOTD(Context);
                }
                catch (Exception ex)
                {
                    await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                    await Task.FromException(ex);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("ops", RunMode = RunMode.Async), Summary("Shows current Fleetup Operations")]
        public async Task Ops()
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["fleetup"]))
            {
                try
                {
                    await Functions.Ops(Context, null);
                }
                catch (Exception ex)
                {
                    await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                    await Task.FromException(ex);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("ops", RunMode = RunMode.Async), Summary("Shows current Fleetup Operations")]
        public async Task Ops([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["fleetup"]))
            {
                try
                {
                        await Functions.Ops(Context, x);
                }
                catch (Exception ex)
                {
                    await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                    await Task.FromException(ex);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("about", RunMode = RunMode.Async), Summary("About Opux")]
        public async Task About()
        {
            try
            {
                await Functions.About(Context);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("char", RunMode = RunMode.Async), Summary("Character Details")]
        public async Task Char([Remainder] string x)
        {
            try
            {
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["charcorp"]))
                    await Functions.Char(Context, x);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("corp", RunMode = RunMode.Async), Summary("Corporation Details")]
        public async Task Corp([Remainder] string x)
        {
            try
            {
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["charcorp"]))
                    await Functions.Corp(Context, x);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("dupes", RunMode = RunMode.Async), Summary("Deletes Duplicate Discord ID's from the MYSQL database")]
        [CheckForRole]
        public async Task Dupes()
        {
            try
            {
                await Functions.Dupes(Context, null);
                await Context.Channel.SendMessageAsync("Duplicates Removed. Check logs for errors", false, null, null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "Modules", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("ts", RunMode = RunMode.Async), Summary("Activates Teamspeak check")]
        public async Task Ts()
        {
            try
            {
                await Functions.TS_Check(Context);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "TeamSpeak", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("ts reset", RunMode = RunMode.Async), Summary("Activates Teamspeak check")]
        public async Task TsReset()
        {
            try
            {
                await Functions.TS_Reset(Context);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "TeamSpeak", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("ts reset", RunMode = RunMode.Async), Summary("Activates Teamspeak check")]
        [CheckForRole]
        public async Task TsReset([Remainder] string x)
        {
            try
            {
                await Functions.TS_Reset(Context, x);
            }
            catch (Exception ex)
            {
                await Context.Message.Channel.SendMessageAsync("ERROR");
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "TeamSpeak", ex.Message, ex));
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("authmaint", RunMode = RunMode.Async), Summary("Activates Teamspeak check")]
        [CheckForRole]
        public async Task AuthMaint()
        {
            try
            {
                await Functions.AuthMaint(Context);
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "AuthMaint", ex.Message, ex));
                await Task.FromException(ex);
            }
        }
    }
}
