using Discord.Commands;
using System;
using System.Globalization;
using System.Linq;
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
        [Command("help"), Summary("Reports help text.")]
        public async Task Help()
        {
            var userInfo = Context.Message.Author;
            await ReplyAsync($"{userInfo.Mention}, Here is a list of plugins available, **!help | !jita | !amarr | !dodixe | !rens | !pc**");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("pc", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Pc([Remainder] string x)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("jita", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !jita Tritanium")]
        public async Task Jita([Remainder] string x)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("amarr", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Amarr([Remainder] string x)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("rens", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Rens([Remainder] string x)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("dodixe", RunMode = RunMode.Async), Summary("Performs Prices Checks Example: !pc Tritanium")]
        public async Task Dodixe([Remainder] string x)
        {
            var userInfo = Context.Message.Author;
            if (x == null)
            {
                await ReplyAsync($"{Context.Message.Author.Mention} please provide an item name");
            }
            else
            {
                await Functions.PriceCheck(Context, x, "dodixe");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("rehash", RunMode = RunMode.Async), Summary("Rehash settings file")]
        [CheckForRole]
        public async Task About()
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
        [Command("reauth"), Summary("Reauth all users")]
        [CheckForRole]
        public async Task Reauth()
        {
            try
            {
                await Functions.AuthCheck(Context);
                await ReplyAsync($"{Context.Message.Author.Mention} REAUTH COMPLETED");
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
        [Command("auth"), Summary("Auth User")]
        public async Task Auth()
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["authWeb"]))
            {
                try
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} To Auth please vist {(string)Program.Settings.GetSection("auth")["authurl"]} and Login with your main");
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
        [Command("auth"), Summary("Auth User")]
        public async Task Auth([Remainder] string x)
        {
            if (Convert.ToBoolean(Program.Settings.GetSection("config")["authWeb"]))
            {
                try
                {
                    await Functions.AuthUser(Context, x);
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
        [Command("evetime"), Summary("EVE TQ Time")]
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
        [Command("MOTD"), Summary("Shows MOTD")]
        public async Task MOTD()
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
    }

}
