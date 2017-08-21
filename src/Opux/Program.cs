using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using EveLibCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WS4NetCore;

namespace Opux
{
    public class Program
    {
        public static DiscordSocketClient Client { get; private set; }
        public static CommandService Commands { get; private set; }
        public static EveLib EveLib { get; private set; }
        public static String ApplicationBase { get; private set; }
        public static IServiceProvider ServiceCollection { get; private set; }
        public static IConfigurationRoot Settings { get; private set; }

        static AutoResetEvent autoEvent = new AutoResetEvent(true);

        static Timer stateTimer = new Timer(Functions.RunTick, autoEvent, 100, 100);

        public static void Main(string[] args)
        {
            try
            {
                ApplicationBase = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
                if (!File.Exists(ApplicationBase + "/Opux.db"))
                    File.Copy(ApplicationBase + "/Opux.def.db", ApplicationBase + "/Opux.db");
                UpdateSettings();
                if (Convert.ToBoolean(Settings.GetSection("config")["WS4NetProvider"]))
                {
                    Client = new DiscordSocketClient(new DiscordSocketConfig() { });
                }
                else
                {
                    Client = new DiscordSocketClient(new DiscordSocketConfig() { WebSocketProvider = WS4NetProvider.Instance });
                }

                
                Commands = new CommandService();
                EveLib = new EveLib();
                MainAsync(args).GetAwaiter().GetResult();

                Console.ReadKey();
                Client.StopAsync();
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                LoggerAsync(ex).GetAwaiter().GetResult();
            }
        }

        internal static async Task LoggerAsync(Exception args)
        {
            await Functions.Client_Log(new LogMessage(LogSeverity.Error, "Main", args.Message, args));
        }

        internal static async Task MainAsync(string[] args)
        {
            Client.Log += Functions.Client_Log;
            Client.UserJoined += Functions.Event_UserJoined;
            Client.Ready += Functions.Ready;

            try
            {
                await Functions.InstallCommands();
                await Client.LoginAsync(TokenType.Bot, Settings.GetSection("config")["token"]);
                await Client.StartAsync();
            }
            catch (HttpException ex)
            {
                if (ex.Reason.Contains("401"))
                {
                    await Functions.Client_Log(new LogMessage(LogSeverity.Error, "Discord", $"Check your Token: {ex.Reason}"));
                }
            }
            catch (Exception ex)
            {
                await Functions.Client_Log(new LogMessage(LogSeverity.Error, "Main", ex.Message, ex));
            }
        }

        public static Task UpdateSettings()
        {
            try
            {
                Settings = new ConfigurationBuilder()
                .SetBasePath(ApplicationBase)
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["notificationFeed"]))
                    Functions._nextNotificationCheck = DateTime.Parse(Functions.SQLiteDataQuery("cacheData", "data", "nextNotificationCheck").GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                var debug = ex.Message;
            }
            return Task.CompletedTask;
        }
    }
}
