using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Opux2
{
    public static class Base
    {
        public static DiscordSocketClient DiscordClient { get; private set; }
        public static CommandService Commands { get; private set; }
        public static string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static IConfiguration Configuration { get; set; }
        public static ICollection<IPlugin> Plugins { get; set; }
        public static IServiceProvider ServiceCollection { get; private set; }
        public static readonly HttpClient _httpClient = new HttpClient();

        internal static bool Quit { get; private set; }
        internal static bool MySqlAvaliable { get; set; }
        internal static LogSeverity LogLevel = default(LogSeverity);
        internal static Dictionary<string, IPlugin> _plugins;

        static Timer _timer = new Timer(timerCallback, null, 500, 500);

        static object ExitLock = new object();
        static ManualResetEventSlim ended = new ManualResetEventSlim();

        static void Main(string[] args)
        {
            try
            {
                DiscordClient = new DiscordSocketClient();

                var result = MainAsync(args).GetAwaiter().GetResult();

                foreach (var Plugin in Plugins)
                {
                    Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Discord", $"{Plugin.Name}")).Wait();
                }

                if (result)
                {
                    System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += context =>
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Discord", "Received termination signal")).Wait();
                        lock (ExitLock)
                        {
                            Monitor.Pulse(ExitLock);
                        }
                        ended.Wait();
                    };

                    lock (ExitLock)
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Discord", "Waiting for termination")).Wait();

                        Monitor.Wait(ExitLock);
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Discord", "Exiting")).Wait();
                    }
                    DiscordClient.StopAsync();
                    ended.Set();
                }
            }
            catch (Exception ex)
            {
                Logger.DiscordClient_Log(new LogMessage(LogSeverity.Critical, "MainAsync", ex.Message, ex));
            }
        }

        internal static async Task<bool> MainAsync(string[] args)
        {
            try
            {
                #region eventhooking

                DiscordClient.Ready += Events.DiscordClient_Ready;
                DiscordClient.Disconnected += Events.DiscordClient_Disconnected;
                DiscordClient.MessageReceived += Events.DiscordClient_HandleCommand;

                DiscordClient.Log += Logger.DiscordClient_Log;
                #endregion

                try
                {
                    IConfigurationBuilder builder;
                    var path = Path.Combine(currentDirectory, "settings.custom.json");
                    if (File.Exists(path))
                    {
                        builder = new ConfigurationBuilder()
                            .SetBasePath(currentDirectory)
                            .AddJsonFile("settings.custom.json", false, true);

                    }
                    else
                    {
                        builder = new ConfigurationBuilder()
                            .SetBasePath(currentDirectory)
                            .AddJsonFile("settings.json", false, true);
                    }

                    Configuration = builder.Build();
                    await mySql.MySqlCheck();
                }
                catch (Exception ex)
                {
                    await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Critical, "MainAsync", ex.Message, ex));
                }

                if (Convert.ToBoolean(Configuration.GetValue<string>("debugger")))
                {
                    while(!Debugger.IsAttached)
                    {
                        await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "MainAsync", "Waiting for Debugger to attach"));
                        await Task.Delay(1000);
                    }
                }

                _plugins = new Dictionary<string, IPlugin>();
                Plugins = PluginLoader<IPlugin>.LoadPlugins("Plugins");

                Commands = new CommandService();
                await Commands.AddModulesAsync(Assembly.GetEntryAssembly());

                await DiscordClient.LoginAsync(TokenType.Bot, Configuration.GetValue<string>("token"), true);
                await DiscordClient.StartAsync();

                while (DiscordClient.ConnectionState != ConnectionState.Connected)
                {
                    await Task.Delay(500);
                }

                foreach (var plugin in Plugins)
                {
                    await plugin.OnLoad().ConfigureAwait(false);
                }

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Critical, "MainAsync", ex.Message, ex));
                return false;
            }
        }

        static void timerCallback(object state)
        {
            Pulse().GetAwaiter();
        }

        static async Task Pulse()
        {
            if (DiscordClient.ConnectionState == ConnectionState.Connected)
            {
                try
                {
                    foreach (var p in Plugins)
                    {
                        await p.Pulse().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Critical, "PulseEvent", ex.Message, ex));
                }
            }
        }
    }
}
