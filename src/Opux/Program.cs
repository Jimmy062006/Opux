using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using EveLibCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Opux
{
	public class Program
	{
		public static DiscordSocketClient Client { get; private set; }
		public static CommandService Commands { get; private set; }
		public static EveLib EveLib { get; private set; }
		public static string ApplicationBase { get; private set; }
		public static IServiceProvider ServiceCollection { get; private set; }
		public static IConfigurationRoot Settings { get; private set; }
		internal static readonly HttpClient _httpClient = new HttpClient();
		internal static readonly HttpClient _zKillhttpClient = new HttpClient();
		internal static bool quit = false;
		internal static bool debug = false;
		static object ExitLock = new object();
		static ManualResetEventSlim ended = new ManualResetEventSlim();

		static AutoResetEvent autoEvent = new AutoResetEvent(true);

		static Timer stateTimer = new Timer(Functions.RunTick, autoEvent, 100, 100);

		public static void Main(string[] args)
		{

			ApplicationBase = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath).Replace('\\', Path.DirectorySeparatorChar);

			if (!File.Exists(Path.Combine(ApplicationBase, "Opux.db")))
				File.Copy(ApplicationBase + "/Opux.def.db", Path.Combine(ApplicationBase, "Opux.db"));

			UpdateSettings();

			Client = new DiscordSocketClient(new DiscordSocketConfig() { });
			Commands = new CommandService();
			EveLib = new EveLib();
			MainAsync().GetAwaiter().GetResult();

			var headless = Convert.ToBoolean(Settings.GetSection("config")["Systemd_Support"]);

			if (!headless)
			{
				var dockerMode = Environment.GetEnvironmentVariable("DOCKER_MODE");

				if (dockerMode != null)
				{
					Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Docker", "Docker mode enabled")).Wait();
					if (dockerMode == "debug")
					{
						Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Docker", "Debug mode enabled")).Wait();
						debug = true;
					}

					System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx =>
					{
						Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Docker", "Received termination signal")).Wait();
						lock (ExitLock)
						{
							Monitor.Pulse(ExitLock);
						}
						ended.Wait();
					};

					lock (ExitLock)
					{
						Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Docker", "Waiting for termination")).Wait();
						Monitor.Wait(ExitLock);
						Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Docker", "Exiting")).Wait();
						quit = true;
					}
				}

				while (!quit)
				{
					var command = Console.ReadLine();
					switch (command.Split(" ")[0])
					{
						case "quit":
							Console.WriteLine($"Quitting Opux");
							quit = true;
							break;
						case "debug":
							if (!debug)
							{
								Console.WriteLine($"Debug mode Active");
								debug = true;
							}
							else
							{
								Console.WriteLine($"Debug mode Disabled");
								debug = false;
							}
							break;
						case "admin":
							var guild = Client.GetGuild(Convert.ToUInt64(Settings.GetSection("config")["guildId"]));
							var rolesToAdd = new List<SocketRole>();
							var GuildRoles = guild.Roles;
							guild.GetUser(Convert.ToUInt64(command.Split(" ")[2])).AddRoleAsync(GuildRoles.FirstOrDefault(x => x.Name == command.Split(" ")[1]));
							break;
					}
				}
			}
			else
			{
				Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Headless", "Headless mode enabled")).Wait();

				System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx =>
				{
					Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Headless", "Received termination signal")).Wait();
					lock (ExitLock)
					{
						Monitor.Pulse(ExitLock);
					}
					ended.Wait();
				};

				lock (ExitLock)
				{
					Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Headless", "Waiting for termination")).Wait();
					Monitor.Wait(ExitLock);
					Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Headless", "Exiting")).Wait();
					quit = true;
				}
			}
			Client.StopAsync();
			ended.Set();
		}

		internal static async Task LoggerAsync(Exception args)
		{
			await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "Main", args.Message, args));
		}

		internal static async Task MainAsync()
		{
			Client.Log += Logger.DiscordClient_Log;
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
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "Discord", $"Check your Token: {ex.Reason}"));
				}
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "Main", ex.Message, ex));
			}
		}

		public static Task UpdateSettings()
		{
			try
			{
				_httpClient.DefaultRequestHeaders.Add("User-Agent", "OpuxBotWebsocket");
				Settings = new ConfigurationBuilder()
				.SetBasePath(ApplicationBase)
				.AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();
				if (Convert.ToBoolean(Settings.GetSection("config")["notificationFeed"]))
					Functions._nextNotificationCheck = DateTime.Parse(Functions.SQLiteDataQuery("cacheData", "data", "nextNotificationCheck").GetAwaiter().GetResult());
			}
			catch (Exception)
			{
				//var debug = ex.Message;
			}
			return Task.CompletedTask;
		}
	}
}
