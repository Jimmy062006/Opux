using Discord;
using Discord.Commands;
using ESIClient.Client;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using HttpListener = System.Net.Http.HttpListener;

namespace authWeb
{
    public class AuthWeb : ModuleBase, IPlugin
    {
        public static IConfiguration Configuration { get; set; }

        internal static HttpListener listener;

        public string Name => "AuthWeb";

        public string Description => "Interface for AuthWeb and the User";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            //Load Settings
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Base.currentDirectory)
                    .AddJsonFile("Plugins/AuthWeb/config.json");

                Configuration = builder.Build();

                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));

            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Critical, Name, ex.Message, ex));
            }

            if (listener == null || !listener.IsListening)
            {
                var port = (int)Configuration.GetValue(typeof(int), "port");
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Starting AuthWeb Server"));
                listener = new HttpListener(IPAddress.Any, port);

                listener.Request += async (sender, context) =>
                {
                    if (context.Request.Url.LocalPath == "/")
                    {
                        var status = await new ESIClient.Api.StatusApi().GetStatusAsync();
                        context.Response.Headers.Add("Content-Type", "text/html");

                        await context.Response.WriteContentAsync(status.ToJson().ToString());
                    }
                    await Task.CompletedTask;

                    context.Response.Close();
                };

                listener.Start();

                await Task.CompletedTask;
            }
        }

        public async Task Pulse()
        {
            await Task.CompletedTask;
        }
    }
}
