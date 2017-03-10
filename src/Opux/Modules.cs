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
        [Command("help"), Summary("Reports help text.")]
        public async Task Help()
        {
            var userInfo = Context.Message.Author;
            await ReplyAsync($"{userInfo.Mention}, Here is a list of plugins available, !help");
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
        //[Command("about", RunMode = RunMode.Async), Summary("About this bot")]
        //public async Task About()
        //{
        //    try
        //    {
        //        await Task.CompletedTask;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Command("reauth"), Summary("Reauth all users")]
        public async Task Testsql()
        {
            try
            {
                await Functions.AuthCheck(Context);
            }
            catch (Exception ex)
            {
                //await Logger.logError(ex.Message);
                Console.WriteLine(ex.Message);
                await Task.FromException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //[Command("killmail", RunMode = RunMode.Async), Summary("Killmail Test")]
        //public async Task Killmail()
        //{
        //    try
        //    {
        //        await Task.CompletedTask;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}
    }

}
