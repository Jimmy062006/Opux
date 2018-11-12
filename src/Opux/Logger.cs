using Discord;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Opux
{
    class Logger
    {
        private static ILog Log { get; set; }
        public static LogSeverity LogLevel { get; private set; }

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
        override protected void Convert(TextWriter writer, object state)
        {
            string specialFolder = AppContext.BaseDirectory;
            writer.Write(specialFolder);
        }
    }
}
