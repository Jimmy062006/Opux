using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EveLibCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Opux
{
    public class Program
    {
        public static DiscordSocketClient client = new DiscordSocketClient();
        public static CommandService commands = new CommandService();
        public static DependencyMap map = new DependencyMap();
        public static EveLib eveLib = new EveLib();
        public static string applicationBase;
        public static IConfigurationRoot Settings;

        static AutoResetEvent autoEvent = new AutoResetEvent(true);

        static Timer stateTimer = new Timer(Functions.RunTick, autoEvent, 1 * 100, 1 * 100);

        public static void Main(string[] args)
        {
            try
            {
                applicationBase = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
                Settings = new ConfigurationBuilder()
                .SetBasePath(Program.applicationBase)
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

                MainAsync(args).GetAwaiter().GetResult();

                Console.ReadKey();
                client.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        internal static async Task MainAsync(string[] args)
        {
            client.Log += Functions.Client_Log;
            client.LoggedOut += Functions.Event_LoggedOut;
            client.LoggedIn += Functions.Event_LoggedIn;
            client.Connected += Functions.Event_Connected;
            client.Disconnected += Functions.Event_Disconnected;
            client.GuildAvailable += Functions.Event_GuildAvaliable;

            await Functions.InstallCommands();
            await client.LoginAsync(TokenType.Bot, Settings.GetSection("config")["token"]);
            await client.StartAsync();
        }
    }
}
