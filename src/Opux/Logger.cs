using Discord;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Opux
{
	class Logger
	{
		private static ILog Log { get; set; }
		public static LogSeverity LogLevel { get; private set; }

		private Logger()
		{
			XmlConfigurator.Configure();
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				Hierarchy repository = LogManager.GetRepository() as Hierarchy;
				if (repository != null)
				{
					var appenders = repository.GetAppenders();
					if (appenders != null)
					{
						foreach (var appender in appenders)
						{
							if (appender is FileAppender)
							{
								var fileLogAppender = appender as FileAppender;
								fileLogAppender.File = fileLogAppender.File.Replace(@"\", Path.DirectorySeparatorChar.ToString());
								fileLogAppender.ActivateOptions();
							}
						}
					}
				}
			}
		}

		public static Task DiscordClient_Log(LogMessage arg)
		{
			Log = LogManager.GetLogger(typeof(Logger));

			var cc = Console.ForegroundColor;

			switch (arg.Severity)
			{
				case LogSeverity.Critical:
				case LogSeverity.Error:
					if (arg.Exception != null)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Log.Error(arg.Exception);
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Log.Error(arg.Message);
					}
					break;
				case LogSeverity.Warning:
					if (arg.Exception != null)
					{
						Log.Warn(arg.Exception);
						Console.ForegroundColor = ConsoleColor.Yellow;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Log.Warn(arg.Message);
					}
					break;
				case LogSeverity.Info:
					if (arg.Exception != null)
					{
						Log.Info(arg.Exception);
						Console.ForegroundColor = ConsoleColor.White;
					}
					else
					{
						Log.Info(arg.Message);
						Console.ForegroundColor = ConsoleColor.White;
					}
					break;
				case LogSeverity.Verbose:
				case LogSeverity.Debug:
					if (arg.Exception != null)
					{
						Log.Debug(arg.Exception);
						Console.ForegroundColor = ConsoleColor.DarkGray;
					}
					else
					{
						Log.Info(arg.Message);
						Console.ForegroundColor = ConsoleColor.DarkGray;
					}
					break;
			}

			if (LogLevel <= arg.Severity)
			{
				Console.WriteLine($"{DateTime.Now,-19} [{arg.Severity,8}] [{arg.Source}]: {arg.Message}");
				Console.ForegroundColor = cc;
			}

			return Task.CompletedTask;
		}
	}

	public class SpecialFolderPatternConverter : log4net.Util.PatternConverter
	{
		override public void Convert(TextWriter writer, object state)
		{
			string specialFolder = AppContext.BaseDirectory;
			writer.Write(specialFolder);
		}
	}
}
