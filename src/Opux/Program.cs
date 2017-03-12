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
        public static DiscordSocketClient Client { get; private set; }
        public static CommandService Commands { get; private set; }
        public static DependencyMap Map { get; private set; }
        public static EveLib EveLib { get; private set; }
        public static string ApplicationBase { get; private set; }
        public static IConfigurationRoot Settings { get; private set; }

        static AutoResetEvent autoEvent = new AutoResetEvent(true);

        static Timer stateTimer = new Timer(Functions.RunTick, autoEvent, 1 * 100, 1 * 100);

        public static void Main(string[] args)
        {
            try
            {
                Client = new DiscordSocketClient();
                Commands = new CommandService();
                Map = new DependencyMap();
                EveLib = new EveLib();
                //ApplicationBase = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
                //Settings = new ConfigurationBuilder()
                //.SetBasePath(Program.ApplicationBase)
                //.AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();
                UpdateSettings();

                MainAsync(args).GetAwaiter().GetResult();

                Console.ReadKey();
                Client.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        internal static async Task MainAsync(string[] args)
        {
            Client.Log += Functions.Client_Log;
            Client.LoggedOut += Functions.Event_LoggedOut;
            Client.LoggedIn += Functions.Event_LoggedIn;
            Client.Connected += Functions.Event_Connected;
            Client.Disconnected += Functions.Event_Disconnected;
            Client.GuildAvailable += Functions.Event_GuildAvaliable;

            await Functions.InstallCommands();
            await Client.LoginAsync(TokenType.Bot, Settings.GetSection("config")["token"]);
            await Client.StartAsync();
        }

        public static Task UpdateSettings()
        {
            ApplicationBase = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
            Settings = new ConfigurationBuilder()
            .SetBasePath(Program.ApplicationBase)
            .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();
            return Task.CompletedTask;
        }
    }
}
