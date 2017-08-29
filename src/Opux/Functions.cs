using ByteSizeLib;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EveLibCore;
using Matrix.Xmpp.Chatstates;
using Matrix.Xmpp.Client;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Opux.JsonClasses;

namespace Opux
{
    internal class Functions
    {
        static DateTime _lastAuthCheck = DateTime.Now;
        static DateTime _lastFeedCheck = DateTime.Now;
        internal static DateTime _nextNotificationCheck = DateTime.FromFileTime(0);
        static int _lastNotification;
        static bool _avaliable = false;
        static bool _running = false;
        static bool _jabberRunning = false;
        static string _motdtopic;
        static DateTime _lastTopicCheck = DateTime.Now;

        //Timer is setup here
        #region Timer stuff
        public async static void RunTick(Object stateInfo)
        {
            try
            {
                if (!_running && _avaliable)
                {
                    _running = true;
                    await Async_Tick(stateInfo);
                }
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
            }
        }

        private async static Task Async_Tick(object args)
        {
            try
            {
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["authWeb"]))
                {
                    await AuthWeb();
                }
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["authCheck"]))
                {
                    await AuthCheck(null);
                }
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["killFeed"]))
                {
                    await KillFeed(null);
                }
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["notificationFeed"]))
                {
                    await NotificationFeed(null);
                }
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["fleetup"]))
                {
                    await FleetUp();
                }
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["updatetopic"]))
                {
                    await TopicMOTD(null);
                }
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["jabber"]))
                {
                    await Jabber();
                }

                //await Client_Log(new LogMessage(LogSeverity.Info, "DETZ",$"{Process.GetCurrentProcess().Id}"));

                _running = false;
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
                _running = false;
            }
        }
        #endregion

        //Needs logging to a file added
        #region Logger
        internal async static Task Client_Log(LogMessage arg)
        {
            try
            {

                var path = Path.Combine(AppContext.BaseDirectory, "logs");
                var file = Path.Combine(path, $"{arg.Source}.log");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (!File.Exists(file))
                {
                    File.Create(file);
                }

                var cc = Console.ForegroundColor;

                switch (arg.Severity)
                {
                    case LogSeverity.Critical:
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;

                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogSeverity.Verbose:
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                }

                using (StreamWriter logFile = new StreamWriter(File.Open(file, FileMode.Append, FileAccess.Write, FileShare.Write), Encoding.UTF8))
                {
                    if (arg.Exception != null)
                    {
                        await logFile.WriteLineAsync($"{DateTime.Now,-19} [{arg.Severity,8}]: {arg.Message} {Environment.NewLine}{arg.Exception}");
                    }
                    else
                    {
                        await logFile.WriteLineAsync($"{DateTime.Now,-19} [{arg.Severity,8}]: {arg.Message}");
                    }
                }

                Console.WriteLine($"{DateTime.Now,-19} [{arg.Severity,8}] [{arg.Source}]: {arg.Message}");
                Console.ForegroundColor = cc;

            }
            catch { }
        }
        #endregion

        //Events are attached here
        #region EVENTS
        internal async static Task Event_UserJoined(SocketGuildUser arg)
        {
            var channel = arg.Guild.DefaultChannel;
            var authurl = Program.Settings.GetSection("auth")["authurl"];
            if (!String.IsNullOrWhiteSpace(authurl))
            {
                await channel.SendMessageAsync($"Welcome {arg.Mention} to the server, To gain access please auth at {authurl} ");
            }
            else
            {
                await channel.SendMessageAsync($"Welcome {arg.Mention} to the server");
            }
        }

        internal static Task Ready()
        {
            _avaliable = true;
            return Task.CompletedTask;
        }
        #endregion

        //Auth
        #region AuthWeb
        internal static System.Net.Http.HttpListener listener;

        internal async static Task AuthWeb()
        {
            var callbackurl = Program.Settings.GetSection("auth")["callbackurl"];
            var client_id = Program.Settings.GetSection("auth")["client_id"];
            var secret = Program.Settings.GetSection("auth")["secret"];
            var url = Program.Settings.GetSection("auth")["url"];
            var port = Convert.ToInt32(Program.Settings.GetSection("auth")["port"]);

            if (listener == null || !listener.IsListening)
            {
                await Client_Log(new LogMessage(LogSeverity.Info, "AuthWeb", "Starting AuthWeb Server"));
                listener = new System.Net.Http.HttpListener(IPAddress.Any, port);

                listener.Request += async (sender, context) =>
                {
                    var request = context.Request;
                    var response = context.Response;
                    if (request.HttpMethod == HttpMethod.Get.ToString())
                    {
                        if (request.Url.LocalPath == "/")
                        {
                            await response.WriteContentAsync("<!doctype html>" +
                                "<html>" +
                                "<head>" +
                                "    <title>Discord Authenticator</title>" +
                                "    <meta name=\"viewport\" content=\"width=device-width\">" +
                                "    <link rel=\"stylesheet\" href=\"https://djyhxgczejc94.cloudfront.net/frameworks/bootstrap/3.0.0/themes/cirrus/bootstrap.min.css\">" +
                                "    <script type=\"text/javascript\" src=\"https://ajax.googleapis.com/ajax/libs/jquery/2.0.3/jquery.min.js\"></script>" +
                                "    <script type=\"text/javascript\" src=\"https://netdna.bootstrapcdn.com/bootstrap/3.1.1/js/bootstrap.min.js\"></script>" +
                                "    <style type=\"text/css\">" +
                                "        /* Space out content a bit */" +
                                "        body {" +
                                "            padding-top: 20px;" +
                                "            padding-bottom: 20px;" +
                                "        }" +
                                "        /* Everything but the jumbotron gets side spacing for mobile first views */" +
                                "        .header, .marketing, .footer {" +
                                "            padding-left: 15px;" +
                                "            padding-right: 15px;" +
                                "        }" +
                                "       /* Custom page header */" +
                                "        .header {" +
                                "            border-bottom: 1px solid #e5e5e5;" +
                                "        }" +
                                "        /* Make the masthead heading the same height as the navigation */" +
                                "        .header h3 {" +
                                "            margin-top: 0;" +
                                "            margin-bottom: 0;" +
                                "            line-height: 40px;" +
                                "            padding-bottom: 19px;" +
                                "        }" +
                                "        /* Custom page footer */" +
                                "        .footer {" +
                                "            padding-top: 19px;" +
                                "            color: #777;" +
                                "            border-top: 1px solid #e5e5e5;" +
                                "        }" +
                                "        /* Customize container */" +
                                "        @media(min-width: 768px) {" +
                                "            .container {" +
                                "                max-width: 730px;" +
                                "            }" +
                                "        }" +
                                "        .container-narrow > hr {" +
                                "            margin: 30px 0;" +
                                "        }" +
                                "        /* Main marketing message and sign up button */" +
                                "        .jumbotron {" +
                                "            text-align: center;" +
                                "            border-bottom: 1px solid #e5e5e5;" +
                                "        }" +
                                "        .jumbotron .btn {" +
                                "            font-size: 21px;" +
                                "            padding: 14px 24px;" +
                                "            color: #0D191D;" +
                                "        }" +
                                "        /* Supporting marketing content */" +
                                "        .marketing {" +
                                "            margin: 40px 0;" +
                                "        }" +
                                "        .marketing p + h4 {" +
                                "            margin-top: 28px;" +
                                "        }" +
                                "        /* Responsive: Portrait tablets and up */" +
                                "        @media screen and(min-width: 768px) {" +
                                "            /* Remove the padding we set earlier */" +
                                "            .header, .marketing, .footer {" +
                                "                padding-left: 0;" +
                                "                padding-right: 0;" +
                                "            }" +
                                "            /* Space out the masthead */" +
                                "            .header {" +
                                "                margin-bottom: 30px;" +
                                "            }" +
                                "            /* Remove the bottom border on the jumbotron for visual effect */" +
                                "            .jumbotron {" +
                                "                border-bottom: 0;" +
                                "            }" +
                                "        }" +
                                "    </style>" +
                                "</head>" +
                                "" +
                                "<body background=\"img/background.jpg\">" +
                                "<div class=\"container\">" +
                                "    <div class=\"header\">" +
                                "        <ul class=\"nav nav-pills pull-right\"></ul>" +
                                "    </div>" +
                                "    <div class=\"jumbotron\">" +
                                "        <h1>Discord</h1>" +
                                "        <p class=\"lead\">Click the button below to login with your EVE Online account.</p>" +
                                "        <p><a href=\"https://login.eveonline.com/oauth/authorize?response_type=code&amp;redirect_uri=" + callbackurl + "&amp;client_id=" + client_id + "\"><img src=\"https://images.contentful.com/idjq7aai9ylm/4fSjj56uD6CYwYyus4KmES/4f6385c91e6de56274d99496e6adebab/EVE_SSO_Login_Buttons_Large_Black.png\"/></a></p>" +
                                "    </div>" +
                                "</div>" +
                                "<!-- /container -->" +
                                "</body>" +
                                "</html>");
                        }
                        else if (request.Url.LocalPath == "/callback.php")
                        {
                            var assembly = Assembly.GetEntryAssembly();
                            var temp = assembly.GetManifestResourceNames();
                            var resource = assembly.GetManifestResourceStream("Opux.Discord-01.png");
                            var buffer = new byte[resource.Length];
                            resource.Read(buffer, 0, Convert.ToInt32(resource.Length));
                            var image = Convert.ToBase64String(buffer);
                            string accessToken = "";
                            string responseString;
                            string verifyString;
                            var uid = GetUniqID();
                            var code = "";
                            var add = false;

                            if (!String.IsNullOrWhiteSpace(request.Url.Query))
                            {
                                code = request.Url.Query.TrimStart('?').Split('=')[1];

                                using (HttpClient tokenclient = new HttpClient())
                                {
                                    var values = new Dictionary<string, string>
                                    {
                                        { "grant_type", "authorization_code" },
                                        { "code", $"{code}"}
                                    };
                                    tokenclient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(client_id + ":" + secret))}");
                                    var content = new FormUrlEncodedContent(values);
                                    var tokenresponse = await tokenclient.PostAsync("https://login.eveonline.com/oauth/token", content);
                                    responseString = await tokenresponse.Content.ReadAsStringAsync();
                                    accessToken = (string)JObject.Parse(responseString)["access_token"];
                                }
                                using (HttpClient verifyclient = new HttpClient())
                                {
                                    verifyclient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                                    var tokenresponse = await verifyclient.GetAsync("https://login.eveonline.com/oauth/verify");
                                    verifyString = await tokenresponse.Content.ReadAsStringAsync();

                                    var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
                                    var corps = new Dictionary<string, string>();
                                    var alliance = new Dictionary<string, string>();

                                    foreach (var config in authgroups)
                                    {
                                        var configChildren = config.GetChildren();

                                        var corpID = configChildren.FirstOrDefault(x => x.Key == "corpID").Value ?? "";
                                        var allianceID = configChildren.FirstOrDefault(x => x.Key == "allianceID").Value ?? "";
                                        var corpMemberRole = configChildren.FirstOrDefault(x => x.Key == "corpMemberRole").Value ?? "";
                                        var allianceMemberRole = configChildren.FirstOrDefault(x => x.Key == "allianceMemberRole").Value ?? "";

                                        if (Convert.ToInt32(corpID) != 0)
                                        {
                                            corps.Add(corpID, corpMemberRole);
                                        }
                                        if (Convert.ToInt32(allianceID) != 0)
                                        {
                                            alliance.Add(allianceID, allianceMemberRole);
                                        }

                                    }

                                    var CharacterID = JObject.Parse(verifyString)["CharacterID"];
                                    JObject characterDetails;
                                    JObject corporationDetails;
                                    JObject allianceDetails;

                                    using (HttpClient webclient = new HttpClient())
                                    using (HttpResponseMessage _characterDetails = await webclient.GetAsync($"https://esi.tech.ccp.is/latest/characters/{CharacterID}"))
                                    using (HttpContent _characterDetailsContent = _characterDetails.Content)
                                    {
                                        var allianceID = "";
                                        var corpID = "";
                                        characterDetails = JObject.Parse(await _characterDetailsContent.ReadAsStringAsync());
                                        characterDetails.TryGetValue("corporation_id", out JToken corporationid);
                                        using (HttpResponseMessage _corporationDetails = await webclient.GetAsync($"https://esi.tech.ccp.is/latest/corporations/{corporationid}"))
                                        using (HttpContent _corporationDetailsContent = _corporationDetails.Content)
                                        {
                                            corporationDetails = JObject.Parse(await _corporationDetailsContent.ReadAsStringAsync());
                                            corporationDetails.TryGetValue("alliance_id", out JToken allianceid);
                                            string i = (allianceid.HasValues ? "0" : allianceid.ToString());
                                            string c = (corporationid.HasValues ? "0" : corporationid.ToString());
                                            allianceID = i;
                                            corpID = c;
                                            if (allianceID != "0")
                                            {
                                                using (HttpResponseMessage _allianceDetails = await webclient.GetAsync($"https://esi.tech.ccp.is/latest/alliances/{allianceid}"))
                                                using (HttpContent _allianceDetailsContent = _allianceDetails.Content)
                                                {
                                                    allianceDetails = JObject.Parse(await _allianceDetailsContent.ReadAsStringAsync());
                                                }
                                            }
                                        }

                                        if (corps.ContainsKey(corpID))
                                        {
                                            add = true;
                                        }
                                        if (alliance.ContainsKey(allianceID))
                                        {
                                            add = true;
                                        }
                                    }
                                    if (add && (string)JObject.Parse(responseString)["error"] != "invalid_request" && (string)JObject.Parse(verifyString)["error"] != "invalid_token")
                                    {
                                        var characterID = CharacterID;
                                        characterDetails.TryGetValue("corporation_id", out JToken corporationid);
                                        corporationDetails.TryGetValue("alliance_id", out JToken allianceid);
                                        var authString = uid;
                                        var active = "1";
                                        var dateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                        var query = "INSERT INTO pendingUsers(characterID, corporationID, allianceID, authString, groups, active, dateCreated) " +
                                        $"VALUES (\"{characterID}\", \"{corporationid}\", \"{allianceid}\", \"{authString}\", \"[]\", \"{active}\", \"{dateCreated}\") ON DUPLICATE KEY UPDATE " +
                                        $"corporationID = \"{corporationid}\", allianceID = \"{allianceid}\", authString = \"{authString}\", groups = \"[]\", active = \"{active}\", dateCreated = \"{dateCreated}\"";
                                        var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
                                        await response.WriteContentAsync("<!doctype html>" +
                                            "<html>" +
                                            "<head>" +
                                            "    <title>Discord Authenticator</title>" +
                                            "    <meta name=\"viewport\" content=\"width=device-width\">" +
                                            "    <link rel=\"stylesheet\" href=\"https://djyhxgczejc94.cloudfront.net/frameworks/bootstrap/3.0.0/themes/cirrus/bootstrap.min.css\">" +
                                            "    <script type=\"text/javascript\" src=\"https://ajax.googleapis.com/ajax/libs/jquery/2.0.3/jquery.min.js\"></script>" +
                                            "    <script type=\"text/javascript\" src=\"https://netdna.bootstrapcdn.com/bootstrap/3.1.1/js/bootstrap.min.js\"></script>" +
                                            "    <style type=\"text/css\">" +
                                            "        /* Space out content a bit */" +
                                            "        body {" +
                                            "            padding-top: 20px;" +
                                            "            padding-bottom: 20px;" +
                                            "        }" +
                                            "        /* Everything but the jumbotron gets side spacing for mobile first views */" +
                                            "        .header, .marketing, .footer {" +
                                            "            padding-left: 15px;" +
                                            "            padding-right: 15px;" +
                                            "        }" +
                                            "        /* Custom page header */" +
                                            "        .header {" +
                                            "            border-bottom: 1px solid #e5e5e5;" +
                                            "        }" +
                                            "        /* Make the masthead heading the same height as the navigation */" +
                                            "        .header h3 {" +
                                            "            margin-top: 0;" +
                                            "            margin-bottom: 0;" +
                                            "            line-height: 40px;" +
                                            "            padding-bottom: 19px;" +
                                            "        }" +
                                            "        /* Custom page footer */" +
                                            "        .footer {" +
                                            "            padding-top: 19px;" +
                                            "            color: #777;" +
                                            "            border-top: 1px solid #e5e5e5;" +
                                            "        }" +
                                            "        /* Customize container */" +
                                            "        @media(min-width: 768px) {" +
                                            "            .container {" +
                                            "                max-width: 730px;" +
                                            "            }" +
                                            "        }" +
                                            "        .container-narrow > hr {" +
                                            "            margin: 30px 0;" +
                                            "        }" +
                                            "        /* Main marketing message and sign up button */" +
                                            "        .jumbotron {" +
                                            "            text-align: center;" +
                                            "            border-bottom: 1px solid #e5e5e5;" +
                                            "            color: #0D191D;" +
                                            "        }" +
                                            "        .jumbotron .btn {" +
                                            "            font-size: 21px;" +
                                            "            padding: 14px 24px;" +
                                            "        }" +
                                            "        /* Supporting marketing content */" +
                                            "        .marketing {" +
                                            "            margin: 40px 0;" +
                                            "        }" +
                                            "        .marketing p + h4 {" +
                                            "            margin-top: 28px;" +
                                            "        }" +
                                            "        /* Responsive: Portrait tablets and up */" +
                                            "        @media screen and(min-width: 768px) {" +
                                            "            /* Remove the padding we set earlier */" +
                                            "            .header, .marketing, .footer {" +
                                            "                padding-left: 0;" +
                                            "                padding-right: 0;" +
                                            "            }" +
                                            "            /* Space out the masthead */" +
                                            "            .header {" +
                                            "                margin-bottom: 30px;" +
                                            "            }" +
                                            "            /* Remove the bottom border on the jumbotron for visual effect */" +
                                            "            .jumbotron {" +
                                            "                border-bottom: 0;" +
                                            "            }" +
                                            "        }" +
                                            "    </style>" +
                                            "</head>" +
                                            "<body>" +
                                            "<div class=\"container\">" +
                                            "    <div class=\"header\">" +
                                            "        <ul class=\"nav nav-pills pull-right\"></ul>" +
                                            "    </div>" +
                                            "    <div class=\"jumbotron\">" +
                                            "        <h1>Discord</h1>" +
                                            "        <p class=\"lead\">Sign in complete.</p>" +
                                            "        <p>If you're not already signed into the server use the link below to get invited. (or right click and copy-link for the Windows/OSX Client)</p>" +
                                            "        <p><a href=\"" + url + "\" target=\"_blank\"><img src=\"data:image/png;base64," + image + "\" width=\"350px\"/></a></p>" +
                                            "        <p>Once you're in chat copy and paste the entire line below to have the bot add you to the correct roles.</p>" +
                                            "        <p><b>!auth " + uid + "</b></p>" +
                                            "    </div>" +
                                            "</div>" +
                                            "<!-- /container -->" +
                                            "</body>" +
                                            "</html>");
                                    }
                                    else
                                    {
                                        var message = "ERROR";
                                        if (!add)
                                        {
                                            message = "You are not Corp/Alliance or Blue";
                                        }
                                        await response.WriteContentAsync("<!doctype html>" +
                                           "<html>" +
                                           "<head>" +
                                           "    <title>Discord Authenticator</title>" +
                                           "    <meta name=\"viewport\" content=\"width=device-width\">" +
                                           "    <link rel=\"stylesheet\" href=\"https://djyhxgczejc94.cloudfront.net/frameworks/bootstrap/3.0.0/themes/cirrus/bootstrap.min.css\">" +
                                           "    <script type=\"text/javascript\" src=\"https://ajax.googleapis.com/ajax/libs/jquery/2.0.3/jquery.min.js\"></script>" +
                                           "    <script type=\"text/javascript\" src=\"https://netdna.bootstrapcdn.com/bootstrap/3.1.1/js/bootstrap.min.js\"></script>" +
                                           "    <style type=\"text/css\">" +
                                           "        /* Space out content a bit */" +
                                           "        body {" +
                                           "            padding-top: 20px;" +
                                           "            padding-bottom: 20px;" +
                                           "        }" +
                                           "        /* Everything but the jumbotron gets side spacing for mobile first views */" +
                                           "        .header, .marketing, .footer {" +
                                           "            padding-left: 15px;" +
                                           "            padding-right: 15px;" +
                                           "        }" +
                                           "        /* Custom page header */" +
                                           "        .header {" +
                                           "            border-bottom: 1px solid #e5e5e5;" +
                                           "        }" +
                                           "        /* Make the masthead heading the same height as the navigation */" +
                                           "        .header h3 {" +
                                           "            margin-top: 0;" +
                                           "            margin-bottom: 0;" +
                                           "            line-height: 40px;" +
                                           "            padding-bottom: 19px;" +
                                           "        }" +
                                           "        /* Custom page footer */" +
                                           "        .footer {" +
                                           "            padding-top: 19px;" +
                                           "            color: #777;" +
                                           "            border-top: 1px solid #e5e5e5;" +
                                           "        }" +
                                           "        /* Customize container */" +
                                           "        @media(min-width: 768px) {" +
                                           "            .container {" +
                                           "                max-width: 730px;" +
                                           "            }" +
                                           "        }" +
                                           "        .container-narrow > hr {" +
                                           "            margin: 30px 0;" +
                                           "        }" +
                                           "        /* Main marketing message and sign up button */" +
                                           "        .jumbotron {" +
                                           "            text-align: center;" +
                                           "            border-bottom: 1px solid #e5e5e5;" +
                                           "            color: #0D191D;" +
                                           "        }" +
                                           "        .jumbotron .btn {" +
                                           "            font-size: 21px;" +
                                           "            padding: 14px 24px;" +
                                           "        }" +
                                           "        /* Supporting marketing content */" +
                                           "        .marketing {" +
                                           "            margin: 40px 0;" +
                                           "        }" +
                                           "        .marketing p + h4 {" +
                                           "            margin-top: 28px;" +
                                           "        }" +
                                           "        /* Responsive: Portrait tablets and up */" +
                                           "        @media screen and(min-width: 768px) {" +
                                           "            /* Remove the padding we set earlier */" +
                                           "            .header, .marketing, .footer {" +
                                           "                padding-left: 0;" +
                                           "                padding-right: 0;" +
                                           "            }" +
                                           "            /* Space out the masthead */" +
                                           "            .header {" +
                                           "                margin-bottom: 30px;" +
                                           "            }" +
                                           "            /* Remove the bottom border on the jumbotron for visual effect */" +
                                           "            .jumbotron {" +
                                           "                border-bottom: 0;" +
                                           "            }" +
                                           "        }" +
                                           "    </style>" +
                                           "</head>" +
                                           "<body>" +
                                           "<div class=\"container\">" +
                                           "    <div class=\"header\">" +
                                           "        <ul class=\"nav nav-pills pull-right\"></ul>" +
                                           "    </div>" +
                                           "    <div class=\"jumbotron\">" +
                                           "        <h1>Discord</h1>" +
                                           "        <p class=\"lead\">Sign in ERROR.</p>" +
                                           "        <p>" + message + "</p>" +
                                           "    </div>" +
                                           "</div>" +
                                           "<!-- /container -->" +
                                           "</body>" +
                                           "</html>");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        response.MethodNotAllowed();
                    }
                    // Close the HttpResponse to send it back to the client.
                    response.Close();
                };
                listener.Start();
            }

        }

        private static string GetUniqID()
        {
            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            double t = ts.TotalMilliseconds / 1000;

            int a = (int)Math.Floor(t);
            int b = (int)((t - Math.Floor(t)) * 1000000);

            return a.ToString("x8") + b.ToString("x5");
        }
        #endregion

        //AuthUser
        #region AuthUser
        internal async static Task AuthUser(ICommandContext context, string remainder)
        {
            var query = $"SELECT * FROM pendingUsers WHERE authString=\"{remainder}\"";
            var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
            if (responce.Count() == 0)
            {
                await context.Channel.SendMessageAsync($"{context.Message.Author.Mention} Key Invalid! Please auth using !auth");
            }
            else if (responce[0]["active"].ToString() == "0")
            {
                await context.Channel.SendMessageAsync($"{context.Message.Author.Mention} Key is not active Please re-auth using !auth");
            }
            else if (responce[0]["active"].ToString() == "1")
            {
                var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
                var corps = new Dictionary<string, string>();
                var alliance = new Dictionary<string, string>();

                foreach (var config in authgroups)
                {
                    var configChildren = config.GetChildren();

                    var corpID2 = configChildren.FirstOrDefault(x => x.Key == "corpID").Value ?? "";
                    var allianceID2 = configChildren.FirstOrDefault(x => x.Key == "allianceID").Value ?? "";
                    var corpMemberRole = configChildren.FirstOrDefault(x => x.Key == "corpMemberRole").Value ?? "";
                    var allianceMemberRole = configChildren.FirstOrDefault(x => x.Key == "allianceMemberRole").Value ?? "";

                    if (Convert.ToInt32(corpID2) != 0)
                    {
                        corps.Add(corpID2, corpMemberRole);
                    }
                    if (Convert.ToInt32(allianceID2) != 0)
                    {
                        alliance.Add(allianceID2, allianceMemberRole);
                    }
                }

                var characterID = responce[0]["characterID"].ToString();

                var responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/characters/{characterID}/?datasource=tranquility");
                var characterData = JsonConvert.DeserializeObject<CharacterData>(await responceMessage.Content.ReadAsStringAsync());

                responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/corporations/{characterData.Corporation_id}/?datasource=tranquility");
                var corporationData = JsonConvert.DeserializeObject<CorporationData>(await responceMessage.Content.ReadAsStringAsync());

                responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/alliances/{corporationData.Alliance_id}/?datasource=tranquility");
                var allianceData = JsonConvert.DeserializeObject<AllianceData>(await responceMessage.Content.ReadAsStringAsync());

                var allianceID = (corporationData.Alliance_id.ToString() == "" ? "0" : corporationData.Alliance_id.ToString());
                var corpID = (characterData.Corporation_id.ToString() == "" ? "0" : characterData.Corporation_id.ToString());

                var enable = false;

                if (corps.ContainsKey(corpID))
                {
                    enable = true;
                }
                if (alliance.ContainsKey(allianceID))
                {
                    enable = true;
                }

                if (enable)
                {
                    var rolesToAdd = new List<SocketRole>();
                    var rolesToTake = new List<SocketRole>();

                    try
                    {
                        var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                        var alertChannel = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);

                        var discordGuild = Program.Client.GetGuild(guildID);
                        var discordUser = Program.Client.GetGuild(guildID).GetUser(context.Message.Author.Id);

                        //Check for Corp roles
                        if (corps.ContainsKey(corpID))
                        {
                            var cinfo = corps.FirstOrDefault(x => x.Key == corpID);
                            rolesToAdd.Add(discordGuild.Roles.FirstOrDefault(x => x.Name == cinfo.Value));
                        }

                        //Check for Alliance roles
                        if (alliance.ContainsKey(allianceID))
                        {
                            var ainfo = alliance.FirstOrDefault(x => x.Key == allianceID);
                            rolesToAdd.Add(discordGuild.Roles.FirstOrDefault(x => x.Name == ainfo.Value));
                        }

                        foreach (var r in rolesToAdd)
                        {
                            if (discordUser.Roles.FirstOrDefault(x => x.Id == r.Id) == null)
                            {
                                var channel = discordGuild.GetTextChannel(alertChannel);
                                await channel.SendMessageAsync($"Granting Roles to {characterData.Name}");
                                await discordUser.AddRolesAsync(rolesToAdd);
                            }
                        }
                        var query2 = $"UPDATE pendingUsers SET active=\"0\" WHERE authString=\"{remainder}\"";
                        var responce2 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query2);

                        await context.Channel.SendMessageAsync($"{context.Message.Author.Mention},:white_check_mark: **Success**: " +
                            $"{characterData.Name} has been successfully authed.");

                        var eveName = characterData.Name;
                        var discordID = discordUser.Id;
                        var active = "yes";
                        var addedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        query2 = "INSERT INTO authUsers(eveName, characterID, discordID, role, active, addedOn) " +
                        $"VALUES (\"{eveName}\", \"{characterID}\", \"{discordID}\", \"[]\", \"{active}\", \"{addedOn}\") ON DUPLICATE KEY UPDATE " +
                        $"eveName = \"{eveName}\", discordID = \"{discordID}\", role = \"[]\", active = \"{active}\", addedOn = \"{addedOn}\"";

                        responce2 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query2);

                        var corpTickers = Convert.ToBoolean(Program.Settings.GetSection("auth")["corpTickers"]);
                        var nameEnforce = Convert.ToBoolean(Program.Settings.GetSection("auth")["nameEnforce"]);

                        if (corpTickers || nameEnforce)
                        {
                            var Nickname = "";
                            if (corpTickers)
                            {
                                Nickname = $"[{corporationData.Ticker}] ";
                            }
                            if (nameEnforce)
                            {
                                Nickname += $"{eveName}";
                            }
                            else
                            {
                                Nickname += $"{discordUser.Username}";
                            }
                            await discordUser.ModifyAsync(x => x.Nickname = Nickname);
                        }
                    }

                    catch (Exception ex)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Failed adding Roles to User {characterData.Name}, Reason: {ex.Message}", ex));
                    }
                }
            }
        }
        #endregion

        //Needs Corp and Standings added
        #region AuthCheck
        internal async static Task AuthCheck(ICommandContext Context)
        {
            //Check inactive users are correct
            if (DateTime.Now > _lastAuthCheck.AddMilliseconds(Convert.ToInt32(Program.Settings.GetSection("config")["authInterval"]) * 1000 * 60) || Context != null)
            {
                _lastAuthCheck = DateTime.Now;

                await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Running Auth Check"));

                var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
                var exemptRoles = Program.Settings.GetSection("auth").GetSection("exempt").GetChildren().ToList();
                var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                var logchan = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);
                var discordUsers = Program.Client.GetGuild(guildID).Users;
                var discordGuild = Program.Client.GetGuild(guildID);
                var corps = new Dictionary<string, string>();
                var alliance = new Dictionary<string, string>();

                foreach (var config in authgroups)
                {
                    var configChildren = config.GetChildren();

                    var corpID = configChildren.FirstOrDefault(x => x.Key == "corpID").Value ?? "";
                    var allianceID = configChildren.FirstOrDefault(x => x.Key == "allianceID").Value ?? "";
                    var corpMemberRole = configChildren.FirstOrDefault(x => x.Key == "corpMemberRole").Value ?? "";
                    var allianceMemberRole = configChildren.FirstOrDefault(x => x.Key == "allianceMemberRole").Value ?? "";

                    if (Convert.ToInt32(corpID) != 0)
                    {
                        corps.Add(corpID, corpMemberRole);
                    }
                    if (Convert.ToInt32(allianceID) != 0)
                    {
                        alliance.Add(allianceID, allianceMemberRole);
                    }

                }

                foreach (var u in discordUsers)
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Running Auth Check on {u.Username}"));
                    if (u.Id == Program.Client.CurrentUser.Id)
                        continue;

                    string query = $"SELECT * FROM authUsers WHERE discordID={u.Id}";
                    var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);

                    if (responce.Count > 0)
                    {
                        var characterID = responce.OrderByDescending(x => x["id"]).FirstOrDefault()["characterID"];

                        var responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/characters/{characterID}/?datasource=tranquility");
                        var characterData = JsonConvert.DeserializeObject<CharacterData>(await responceMessage.Content.ReadAsStringAsync());

                        responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/corporations/{characterData.Corporation_id}/?datasource=tranquility");
                        var corporationData = JsonConvert.DeserializeObject<CorporationData>(await responceMessage.Content.ReadAsStringAsync());

                        responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/alliances/{corporationData.Alliance_id}/?datasource=tranquility");
                        var allianceData = JsonConvert.DeserializeObject<AllianceData>(await responceMessage.Content.ReadAsStringAsync());

                        var allianceID = (corporationData.Alliance_id.ToString() == "" ? "0" : corporationData.Alliance_id.ToString());
                        var corpID = (characterData.Corporation_id.ToString() == "" ? "0" : characterData.Corporation_id.ToString());

                        var roles = new List<SocketRole>();
                            var rolesOrig = new List<SocketRole>(u.Roles);
                            var remroles = new List<SocketRole>();
                            roles.Add(u.Roles.FirstOrDefault(x => x.Name == "@everyone"));
                            foreach (var role in exemptRoles)
                            {
                                var exemptRole = u.Roles.FirstOrDefault(x => x.Name == role.Key);
                                if (exemptRole != null)
                                    roles.Add(exemptRole);
                            }

                            //Check for Corp roles
                            if (corps.ContainsKey(corpID))
                            {
                                var cinfo = corps.FirstOrDefault(x => x.Key == corpID);
                                roles.Add(discordGuild.Roles.FirstOrDefault(x => x.Name == cinfo.Value));
                            }

                            //Check for Alliance roles
                            if (alliance.ContainsKey(allianceID))
                            {
                                var ainfo = alliance.FirstOrDefault(x => x.Key == allianceID);
                                roles.Add(discordGuild.Roles.FirstOrDefault(x => x.Name == ainfo.Value));
                            }

                            bool changed = false;

                            foreach (var role in rolesOrig)
                            {
                                if (roles.FirstOrDefault(x => x.Id == role.Id) == null)
                                {
                                    remroles.Add(role);
                                    changed = true;
                                }
                            }

                            foreach (var role in roles)
                            {
                                if (rolesOrig.FirstOrDefault(x => x.Id == role.Id) == null)
                                    changed = true;
                            }

                            if (changed)
                            {
                                roles.Remove(u.Roles.FirstOrDefault(x => x.Name == "@everyone"));
                                var channel = discordGuild.GetTextChannel(logchan);
                                await channel.SendMessageAsync($"Adjusting roles for {u.Username}");
                                await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Adjusting roles for {u.Username}"));
                                await u.AddRolesAsync(roles);
                                await u.RemoveRolesAsync(remroles);
                            }

                            var eveName = characterData.Name;

                            var corpTickers = Convert.ToBoolean(Program.Settings.GetSection("auth")["corpTickers"]);
                            var nameEnforce = Convert.ToBoolean(Program.Settings.GetSection("auth")["nameEnforce"]);

                        if (corpTickers || nameEnforce)
                        {
                            if (corporationData.Ticker == "")
                            {
                                var Nickname = "";
                                if (corpTickers)
                                {
                                    Nickname = $"[{corporationData.Ticker}] ";
                                }
                                if (nameEnforce)
                                {
                                    Nickname += $"{eveName}";
                                }
                                else
                                {
                                    Nickname += $"{u.Username}";
                                }
                                if (Nickname != u.Nickname && !String.IsNullOrWhiteSpace(u.Nickname) || String.IsNullOrWhiteSpace(u.Nickname) && u.Username != Nickname)
                                {
                                    await u.ModifyAsync(x => x.Nickname = Nickname);
                                    await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Changed name of {u.Nickname} to {Nickname}"));
                                }
                            }
                        }                     
                    }
                    else
                    {
                        var rroles = new List<SocketRole>();
                        var rrolesOrig = new List<SocketRole>(u.Roles);
                        foreach (var role in exemptRoles)
                        {
                            var exemptRole = u.Roles.FirstOrDefault(x => x.Name == role.Key);
                            if (exemptRole != null)
                                rroles.Remove(exemptRole);
                        }

                        rrolesOrig.Remove(u.Roles.FirstOrDefault(x => x.Name == "@everyone"));

                        bool rchanged = false;

                        foreach (var role in rrolesOrig)
                        {
                            if (rroles.FirstOrDefault(x => x.Id == role.Id) == null)
                                rchanged = true;
                        }

                        foreach (var role in rroles)
                        {
                            if (rrolesOrig.FirstOrDefault(x => x.Id == role.Id) == null)
                                rchanged = true;
                        }

                        if (rchanged)
                        {
                            var channel = discordGuild.GetTextChannel(logchan);
                            await channel.SendMessageAsync($"Resetting roles for {u.Username}");
                            await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Resetting roles for {u.Username}"));
                            await u.RemoveRolesAsync(rroles);
                        }
                    }
                }
                if (Context != null)
                {
                    var channel = discordGuild.GetTextChannel(logchan);
                    await channel.SendMessageAsync($"{Context.Message.Author.Mention} REAUTH COMPLETED");
                }
            }
        }
        #endregion

        //Complete
        #region killFeed
        private static async Task KillFeed(CommandContext Context)
        {
            try
            {
                _lastFeedCheck = DateTime.Now;
                Dictionary<string, IEnumerable<IConfigurationSection>> feedGroups = new Dictionary<string, IEnumerable<IConfigurationSection>>();

                UInt64 guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                UInt64 logchan = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);
                var discordGuild = Program.Client.Guilds.FirstOrDefault(X => X.Id == guildID);
                var redisQID = Program.Settings.GetSection("killFeed")["reDisqID"].ToString();
                ITextChannel channel = null;
                var redisqResponse = await (await Program._httpClient.GetAsync(String.IsNullOrEmpty(redisQID) ?
                    $"https://redisq.zkillboard.com/listen.php" : $"https://redisq.zkillboard.com/listen.php?queueID={redisQID}")).Content.ReadAsStringAsync();
                var kill = JsonConvert.DeserializeObject<ZKillboardRedisq>(redisqResponse).Package;

                if (kill == null)
                {
                    return;
                }

                var bigKillGlobal = Convert.ToInt64(Program.Settings.GetSection("killFeed")["bigKill"]);
                var bigKillGlobalChan = Convert.ToUInt64(Program.Settings.GetSection("killFeed")["bigKillChannel"]);
                var iD = kill.Killmail.KillID_str;
                var killTime = kill.Killmail.KillTime;
                var ship = kill.Killmail.Victim.ShipType.Name;
                var value = kill.Zkb.TotalValue;
                var victimCharacter = kill.Killmail.Victim.Character;
                var victimCorp = kill.Killmail.Victim.Corporation;
                var victimAlliance = kill.Killmail.Victim.Alliance;
                var attackers = kill.Killmail.Attackers;
                var sysName = kill.Killmail.SolarSystem.Name;
                var systemId = kill.Killmail.SolarSystem.Id_str;
                var losses = Convert.ToBoolean(Program.Settings.GetSection("killFeed")["losses"]);
                var radius = Convert.ToInt16(Program.Settings.GetSection("killFeed")["radius"]);
                var radiusSystem = Program.Settings.GetSection("killFeed")["radiusSystem"];
                var radiusChannel = Convert.ToUInt64(Program.Settings.GetSection("killFeed")["radiusChannel"]);

                var post = false;
                var globalBigKill = false;
                var bigKill = false;
                var radiusKill = false;
                var jumpsAway = 0;

                foreach (var i in Program.Settings.GetSection("killFeed").GetSection("groupsConfig").GetChildren().ToList())
                {
                    var minimumValue = Convert.ToInt64(i["minimumValue"]);
                    var minimumLossValue = Convert.ToInt64(i["minimumLossValue"]);
                    var allianceID = Convert.ToInt32(i["allianceID"]);
                    var corpID = Convert.ToInt32(i["corpID"]);
                    var channelGroup = Convert.ToUInt64(i["channel"]);
                    var bigKillValue = Convert.ToInt64(i["bigKill"]);
                    var bigKillChannel = Convert.ToUInt64(i["bigKillChannel"]);
                    var SystemID = "0";

                    if (radius > 0)
                    {

                        var SystemName = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/search/?categories=solarsystem&strict=true&datasource=tranquility" +
                            $"&language=en-us&search={radiusSystem}&strict=true");
                        var SystemNameContent = SystemName.Content;
                        var httpresult = JsonConvert.DeserializeObject<SolarSystemSearch>(await SystemNameContent.ReadAsStringAsync());

                        SystemID = httpresult.Solarsystem[0].ToString();
                        var systemID = kill.Killmail.SolarSystem.Id_str;

                        var radiusSystems = await Program._httpClient.GetStringAsync($"https://esi.tech.ccp.is/latest/route/{SystemID}/{systemId}/?datasource=tranquility&flag=shortest");

                        var data = JArray.Parse(radiusSystems);

                        var gg = data.Count() - 1;
                        if (gg < radius)
                        {
                            jumpsAway = gg;
                            radiusKill = true;
                        }
                    }

                    if (bigKillGlobal != 0 && value >= bigKillGlobal)
                    {
                        channel = discordGuild.GetTextChannel(bigKillGlobalChan);
                        globalBigKill = true;
                        post = true;
                    }
                    else if (allianceID == 0 && corpID == 0)
                    {
                        if (bigKillValue != 0 && value >= bigKillValue && !globalBigKill)
                        {
                            channel = discordGuild.GetTextChannel(bigKillChannel);
                            bigKill = true;
                            post = true;
                        }
                        else
                        {
                            channel = discordGuild.GetTextChannel(channelGroup);
                            var totalValue = value;
                            if (minimumValue == 0 || minimumValue <= totalValue)
                                post = true;
                        }
                    }
                    else if (!globalBigKill)
                    {
                        channel = discordGuild.GetTextChannel(channelGroup);
                        if (victimAlliance != null)
                        {
                            if (victimAlliance.Id == allianceID && losses == true ||
                                victimCorp.Id == corpID && losses == true)
                            {
                                if (bigKillValue != 0 && value >= bigKillValue)
                                {
                                    channel = discordGuild.GetTextChannel(bigKillChannel);
                                    bigKill = true;
                                    post = true;
                                }
                                else
                                {
                                    if (minimumLossValue == 0 || minimumLossValue <= value)
                                        post = true;
                                }
                            }
                        }
                        else if (victimCorp.Id == corpID && losses == true)
                        {
                            if (bigKillValue != 0 && value >= bigKillValue)
                            {
                                channel = discordGuild.GetTextChannel(bigKillChannel);
                                bigKill = true;
                                post = true;
                            }
                            else
                            {
                                if (minimumLossValue == 0 || minimumLossValue <= value)
                                    post = true;
                            }
                        }
                        foreach (var attacker in attackers.ToList())
                        {
                            if (attacker.Alliance != null)
                            {
                                if (attacker.Alliance.Id == allianceID ||
                                    attacker.Corporation.Id == corpID)
                                {
                                    if (bigKillValue != 0 && value >= bigKillValue)
                                    {
                                        channel = discordGuild.GetTextChannel(bigKillChannel);
                                        bigKill = true;
                                        post = true;
                                    }
                                    else
                                    {
                                        if (minimumValue == 0 || minimumValue <= value)
                                            post = true;
                                    }
                                }
                                else if (attacker.Corporation.Id == corpID)
                                {
                                    if (bigKillValue != 0 && value >= bigKillValue)
                                    {
                                        channel = discordGuild.GetTextChannel(bigKillChannel);
                                        bigKill = true;
                                        post = true;
                                    }
                                    else
                                    {
                                        if (minimumValue == 0 || minimumValue <= value)
                                            post = true;
                                    }
                                }
                            }
                        }
                    }

                    if (post || bigKill || globalBigKill || radiusKill)
                    {
                        if (victimCharacter == null)// Kill is probably a structure.
                        {
                            if (victimAlliance == null)
                            {
                                if (radiusKill)
                                {
                                    var _radiusChannel = discordGuild.GetTextChannel(radiusChannel);
                                    var radiusMessage = "";
                                    radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                    radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                        $" [{victimCorp.Name}]** killed in **{sysName}** {Environment.NewLine} https://zkillboard.com/kill/{iD}/";
                                    await _radiusChannel.SendMessageAsync(radiusMessage);
                                }
                                var message = "";
                                if (globalBigKill)
                                {
                                    message = $"**Global Big Kill**{Environment.NewLine}";
                                }
                                else if (bigKill)
                                {
                                    message = $"**Big Kill**{Environment.NewLine}";
                                }
                                if (post)
                                {
                                    message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                        $" [{victimCorp.Name}]** killed in **{sysName}** {Environment.NewLine} " +
                                        $"https://zkillboard.com/kill/{iD}/";
                                    await channel.SendMessageAsync(message);
                                }
                            }
                            else
                            {
                                if (radiusKill)
                                {
                                    var _radiusChannel = discordGuild.GetTextChannel(radiusChannel);
                                    var radiusMessage = "";
                                    radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                    radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                    $" {victimCorp.Name} | [{victimAlliance.Name}]** killed in **{sysName}** {Environment.NewLine} " +
                                    $"https://zkillboard.com/kill/{iD}/";
                                    await _radiusChannel.SendMessageAsync(radiusMessage);
                                }
                                var message = "";
                                if (globalBigKill)
                                {
                                    message = $"**Global Big Kill**{Environment.NewLine}";
                                }
                                else if (bigKill)
                                {
                                    message = $"**Big Kill**{Environment.NewLine}";
                                }
                                if (post)
                                {
                                    message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                        $" {victimCorp.Name} | [{victimAlliance.Name}]** killed in **{sysName}** {Environment.NewLine} " +
                                        $"https://zkillboard.com/kill/{iD}/";
                                    await channel.SendMessageAsync(message);
                                }
                            }
                        }
                        else if (victimAlliance != null)
                        {
                            if (radiusKill)
                            {
                                var _radiusChannel = discordGuild.GetTextChannel(radiusChannel);
                                var radiusMessage = "";
                                radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                $"** ISK flown by **{victimCharacter.Name} |**  **[{victimCorp.Name}] | <{victimAlliance.Name}>** killed in **{sysName}** {Environment.NewLine} " +
                                $"https://zkillboard.com/kill/{iD}/";
                                await _radiusChannel.SendMessageAsync(radiusMessage);
                            }
                            var message = "";
                            if (globalBigKill)
                            {
                                message = $"**Global Big Kill**{Environment.NewLine}";
                            }
                            else if (bigKill)
                            {
                                message = $"**Big Kill**{Environment.NewLine}";
                            }
                            if (post)
                            {
                                message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                    $"** ISK flown by **{victimCharacter.Name} |**  **[{victimCorp.Name}] | <{victimAlliance.Name}>** killed in **{sysName}** {Environment.NewLine} " +
                                    $"https://zkillboard.com/kill/{iD}/";
                                await channel.SendMessageAsync(message);
                            }
                        }
                        else
                        {
                            if (radiusKill)
                            {
                                var _radiusChannel = discordGuild.GetTextChannel(radiusChannel);
                                var radiusMessage = "";
                                radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                $"** ISK flown by **{victimCharacter.Name} |** **[{victimCorp.Name}]** killed in **{sysName}** {Environment.NewLine} " +
                                $"https://zkillboard.com/kill/{iD}/";
                                await _radiusChannel.SendMessageAsync(radiusMessage);
                            }
                            var message = "";
                            if (globalBigKill)
                            {
                                message = $"**Global Big Kill**{Environment.NewLine}";
                            }
                            else if (bigKill)
                            {
                                message = $"**Big Kill**{Environment.NewLine}";
                            }
                            if (post)
                            {
                                message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                    $"** ISK flown by **{victimCharacter.Name} |** **[{victimCorp.Name}]** killed in **{sysName}** {Environment.NewLine} " +
                                    $"https://zkillboard.com/kill/{iD}/";
                                await channel.SendMessageAsync(message);
                            }
                        }
                        await Client_Log(new LogMessage(LogSeverity.Info, "killFeed", $"POSTING Kill/Loss ID:{kill.Killmail.KillID_str} Value:{string.Format("{0:n0}", value)}"));
                    }
                }
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "killFeed", ex.Message, ex));
            }
        }
        #endregion

        //Needs Doing
        #region Notifications
        internal async static Task NotificationFeed(CommandContext Context)
        {
            #region Notification Type Dictionary
            Dictionary<int, string> types = new Dictionary<int, string>{
                {1, "Legacy"},
                {2, "Character deleted"},
                {3, "Give medal to character"},
                {4, "Alliance maintenance bill"},
                {5, "Alliance war declared"},
                {6, "Alliance war surrender"},
                {7, "Alliance war retracted"},
                {8, "Alliance war invalidated by Concord"},
                {9, "Bill issued to a character"},
                {10,    "Bill issued to corporation or alliance"},
                {11,    "Bill not paid because there's not enough ISK available"},
                {12,    "Bill, issued by a character, paid"},
                {13,    "Bill, issued by a corporation or alliance, paid"},
                {14,    "Bounty claimed"},
                {15,    "Clone activated"},
                {16,    "New corp member application"},
                {17,    "Corp application rejected"},
                {18,    "Corp application accepted"},
                {19,    "Corp tax rate changed"},
                {20,    "Corp news report, typically for shareholders"},
                {21,    "Player leaves corp"},
                {22,    "Corp news, new CEO"},
                {23,    "Corp dividend/liquidation, sent to shareholders"},
                {24,    "Corp dividend payout, sent to shareholders"},
                {25,    "Corp vote created"},
                {26,    "Corp CEO votes revoked during voting"},
                {27,    "Corp declares war"},
                {28,    "Corp war has started"},
                {29,    "Corp surrenders war"},
                {30,    "Corp retracts war"},
                {31,    "Corp war invalidated by Concord"},
                {32,    "Container password retrieval"},
                {33,    "Contraband or low standings cause an attack or items being confiscated"},
                {34,    "First ship insurance"},
                {35,    "Ship destroyed, insurance payed"},
                {36,    "Insurance contract invalidated/runs out"},
                {37,    "Sovereignty claim fails (alliance)"},
                {38,    "Sovereignty claim fails (corporation)"},
                {39,    "Sovereignty bill late (alliance)"},
                {40,    "Sovereignty bill late (corporation)"},
                {41,    "Sovereignty claim lost (alliance)"},
                {42,    "Sovereignty claim lost (corporation)"},
                {43,    "Sovereignty claim acquired (alliance)"},
                {44,    "Sovereignty claim acquired (corporation)"},
                {45,    "Alliance anchoring alert"},
                {46,    "Alliance structure turns vulnerable"},
                {47,    "Alliance structure turns invulnerable"},
                {48,    "Sovereignty disruptor anchored"},
                {49,    "Structure won/lost"},
                {50,    "Corp office lease expiration notice"},
                {51,    "Clone contract revoked by station manager"},
                {52,    "Corp member clones moved between stations"},
                {53,    "Clone contract revoked by station manager"},
                {54,    "Insurance contract expired"},
                {55,    "Insurance contract issued"},
                {56,    "Jump clone destroyed"},
                {57,    "Jump clone destroyed"},
                {58,    "Corporation joining factional warfare"},
                {59,    "Corporation leaving factional warfare"},
                {60,    "Corporation kicked from factional warfare on startup because of too low standing to the faction"},
                {61,    "Character kicked from factional warfare on startup because of too low standing to the faction"},
                {62,    "Corporation in factional warfare warned on startup because of too low standing to the faction"},
                {63,    "Character in factional warfare warned on startup because of too low standing to the faction"},
                {64,    "Character loses factional warfare rank"},
                {65,    "Character gains factional warfare rank"},
                {66,    "Agent has moved"},
                {67,    "Mass transaction reversal message"},
                {68,    "Reimbursement message"},
                {69,    "Agent locates a character"},
                {70,    "Research mission becomes available from an agent"},
                {71,    "Agent mission offer expires"},
                {72,    "Agent mission times out"},
                {73,    "Agent offers a storyline mission"},
                {74,    "Tutorial message sent on character creation"},
                {75,    "Tower alert"},
                {76,    "Tower resource alert"},
                {77,    "Station aggression message"},
                {78,    "Station state change message"},
                {79,    "Station conquered message"},
                {80,    "Station aggression message"},
                {81,    "Corporation requests joining factional warfare"},
                {82,    "Corporation requests leaving factional warfare"},
                {83,    "Corporation withdrawing a request to join factional warfare"},
                {84,    "Corporation withdrawing a request to leave factional warfare"},
                {85,    "Corporation liquidation"},
                {86,    "Territorial Claim Unit under attack"},
                {87,    "Sovereignty Blockade Unit under attack"},
                {88,    "Infrastructure Hub under attack"},
                {89,    "Contact add notification"},
                {90,    "Contact edit notification"},
                {91,    "Incursion Completed"},
                {92,    "Corp Kicked"},
                {93,    "Customs office has been attacked"},
                {94,    "Customs office has entered reinforced"},
                {95,    "Customs office has been transferred"},
                {96,    "FW Alliance Warning"},
                {97,    "FW Alliance Kick"},
                {98,    "AllWarCorpJoined Msg"},
                {99,    "Ally Joined Defender"},
                {100,   "Ally Has Joined a War Aggressor"},
                {101,   "Ally Joined War Ally"},
                {102,   "New war system: entity is offering assistance in a war."},
                {103,   "War Surrender Offer"},
                {104,   "War Surrender Declined"},
                {105,   "FacWar LP Payout Kill"},
                {106,   "FacWar LP Payout Event"},
                {107,   "FacWar LP Disqualified Eventd"},
                {108,   "FacWar LP Disqualified Kill"},
                {109,   "Alliance Contract Cancelled"},
                {110,   "War Ally Declined Offer"},
                {111,   "Your Bounty Was Claimed"},
                {112,   "Bounty placed (Char)"},
                {113,   "Bounty Placed (Corp)"},
                {114,   "Bounty Placed (Alliance)"},
                {115,   "Kill Right Available"},
                {116,   "Kill right Available Open"},
                {117,   "Kill Right Earned"},
                {118,   "Kill right Used"},
                {119,   "Kill Right Unavailable"},
                {120,   "Kill Right Unavailable Open"},
                {121,   "Declare War"},
                {122,   "Offered Surrender"},
                {123,   "Accepted Surrender"},
                {124,   "Made War Mutual"},
                {125,   "Retracts War"},
                {126,   "Offered To Ally"},
                {127,   "Accepted Ally"},
                {128,   "Character Application Accept"},
                {129,   "Character Application Reject"},
                {130,   "Character Application Withdraw"},
                {138,   "Clone activated"},
                {140,   "Loss report available"},
                {141,   "Kill report available"},
                {147,   "Entosis Link started"},
                {148,   "Entosis Link enabled a module"},
                {149,   "Entosis Link disabled a module"},
                {131,   "DustAppAcceptedMsg ?"},
                {132,   "DistrictAttacked ?"},
                {133,   "BattlePunishFriendlyFire ?"},
                {134,   "BountyESSTaken ?"},
                {135,   "BountyESSShared ?"},
                {136,   "IndustryTeamAuctionWon ?"},
                {137,   "IndustryTeamAuctionLost ?"},
                {139,   "Corporation invitation accepted (CorpAppInvitedMsg)"},
                {142,   "Corporation application rejected (CorpAppRejectCustomMsg)"},
                {143,   "Friendly fire enable timer started (CorpFriendlyFireEnableTimerStarted)"},
                {144,   "Friendly fire disable timer started (CorpFriendlyFireDisableTimerStarted)"},
                {145,   "Friendly fire enable timer completed (CorpFriendlyFireEnableTimerCompleted)"},
                {146,   "Friendly fire disable timer completed (CorpFriendlyFireDisableTimerCompleted)"},
                {152,   "Infrastructure hub bill about to expire (InfrastructureHubBillAboutToExpire)"},
                {160,   "Sovereignty structure reinforced (SovStructureReinforced)"},
                {161,   "SovCommandNodeEventStarted ?"},
                {162,   "Sovereignty structure destroyed (SovStructureDestroyed)"},
                {163,   "SovStationEnteredFreeport ?"},
                {164,   "IHubDestroyedByBillFailure ?"},
                {166,   "BuddyConnectContactAdd ?"},
                {165,   "Alliance capital changed (AllianceCapitalChanged)"},
                {167,   "Sovereignty structure self destruction requested (SovStructureSelfDestructRequested)"},
                {168,   "Sovereignty structure self destruction canceled (SovStructureSelfDestructCancel)"},
                {169,   "Sovereignty structure self destruction completed (SovStructureSelfDestructFinished)"},
                {181,   "Structure fuel alert (StructureFuelAlert)"},
                {182,   "Structure anchoring started (StructureAnchoring)"},
                {183,   "Structure unanchoring started (StructureUnanchoring)"},
                {184,   "Structure under attack (StructureUnderAttack)"},
                {185,   "Structure Online (StructureOnline)"},
                {186,   "Structure lost shields (StructureLostShields)"},
                {187,   "Structure lost Armor (StructureLostArmor)"},
                {188,   "Structure destroyed (StructureDestroyed)"},
                {198,   "Structure service offline (StructureServicesOffline)"},
                {199,   "Item delivered (StructureItemsDelivered)"},
                {200,   "Points Awarded"},
                {201,   "StructureCourierContractChanged ?"},
                {1012,  "OperationFinished ?"},
                {1030,  "Game time received (GameTimeReceived)"},
                {1031,  "Game time sent (GameTimeSent)"}
            };
            #endregion
            try
            {
                if (DateTime.Now > _nextNotificationCheck)
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", "Running Notification Check"));
                    _lastNotification = Convert.ToInt32(await SQLiteDataQuery("cacheData", "data", "lastNotificationID"));
                    var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                    var channelId = Convert.ToUInt64(Program.Settings.GetSection("notifications")["channelId"]);
                    var chan = Program.Client.GetGuild(guildID).GetTextChannel(channelId);
                    var keyID = "";
                    var vCode = "";
                    var characterID = "";
                    var keys = Program.Settings.GetSection("notifications").GetSection("keys").GetChildren();
                    var keyCount = keys.Count();
                    var nextKey = await SQLiteDataQuery("notifications", "data", "nextKey");
                    var index = 0;
                    var runComplete = false;

                    foreach (var key in keys)
                    {
                        if (key.Key != nextKey && String.IsNullOrWhiteSpace(nextKey))
                        {
                            index++;
                        }
                        if (nextKey == null && !runComplete || String.IsNullOrWhiteSpace(nextKey) && !runComplete || nextKey == key.Key && !runComplete)
                        {
                            characterID = key["characterID"];
                            keyID = key["keyID"];
                            vCode = key["vCode"];

                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Checking characterID:{characterID} keyID:{keyID} vCode:{vCode}"));

                            await EveLib.SetApiKey(keyID, vCode, characterID);
                            var notifications = await EveLib.GetNotifications();
                            var notificationsSort = notifications.OrderBy(x => x.Key);

                            if (notifications.Count > 0)
                            {
                                var notiIDs = new List<int>();

                                foreach (var l in notifications)
                                {
                                    notiIDs.Add((int)l.Key);
                                }

                                var notificationsText = await EveLib.GetNotificationText(notiIDs);

                                foreach (var notification in notificationsSort)
                                {
                                    if ((int)notification.Value["notificationID"] > _lastNotification)
                                    {
                                        var notificationText = notificationsText.FirstOrDefault(x => x.Key == notification.Key).Value;
                                        var notificationType = (int)notification.Value["typeID"];

                                        if (notificationType == 5)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
                                            var cost = notificationText["cost"].AllNodes.ToList()[0];
                                            var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
                                            var delayHours = notificationText["delayHours"].AllNodes.ToList()[0].ToString();
                                            var hostileState = notificationText["hostileState"].AllNodes.ToList()[0].ToString();
                                            var names = await EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
                                            var againstName = names.FirstOrDefault(x => x.Key == againstID);
                                            var declaredByName = names.First(x => x.Key == declaredByID);

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}War declared by {declaredByName.Value} against {againstName.Value}" +
                                                $" Fighting begins in roughly {delayHours} hours");
                                        }
                                        else if (notificationType == 7)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
                                            var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());

                                            var stuff = await EveLib.IDtoName(new List<Int64> { againstID, declaredByID });
                                            var againstName = stuff.FirstOrDefault(x => x.Key == againstID).Value;
                                            var declaredByName = stuff.FirstOrDefault(x => x.Key == declaredByID).Value;

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}{declaredByName} Retracts War Against {againstName}");
                                        }
                                        else if (notificationType == 27)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
                                            var cost = notificationText["cost"].AllNodes.ToList()[0];
                                            var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
                                            var names = await EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
                                            var againstName = names.FirstOrDefault(x => x.Key == againstID);
                                            var declaredByName = names.First(x => x.Key == declaredByID);

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}War declared by {declaredByName.Value} against {againstName.Value}");
                                        }
                                        else if (notificationType == 30)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
                                            var cost = notificationText["cost"].AllNodes.ToList()[0];
                                            var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
                                            var names = await EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
                                            var againstName = names.FirstOrDefault(x => x.Key == againstID);
                                            var declaredByName = names.First(x => x.Key == declaredByID);

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}{declaredByName.Value} Retracts War Against {againstName.Value}");
                                        }
                                        else if (notificationType == 75)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var aggressorAllianceID = Convert.ToInt64(notificationText["aggressorAllianceID"].AllNodes.ToList()[0].ToString());
                                            var aggressorCorpID = Convert.ToInt64(notificationText["aggressorCorpID"].AllNodes.ToList()[0].ToString());
                                            var aggressorID = Convert.ToInt64(notificationText["aggressorID"].AllNodes.ToList()[0].ToString());
                                            var typeID = Convert.ToInt64(notificationText["typeID"].AllNodes.ToList()[0].ToString());
                                            var moonID = Convert.ToInt64(notificationText["moonID"].AllNodes.ToList()[0].ToString());
                                            var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
                                            var armorValue = string.Format("{0:P2}", Convert.ToDouble(notificationText["armorValue"].AllNodes.ToList()[0].ToString()));
                                            var shieldValue = string.Format("{0:P2}", Convert.ToDouble(notificationText["shieldValue"].AllNodes.ToList()[0].ToString()));
                                            var hullValue = string.Format("{0:P2}", Convert.ToDouble(notificationText["hullValue"].AllNodes.ToList()[0].ToString()));
                                            var names = await EveLib.IDtoName(new List<Int64> { aggressorAllianceID, aggressorCorpID, aggressorID, moonID, solarSystemID });
                                            var aggressorAlliance = names.FirstOrDefault(x => x.Key == aggressorAllianceID).Value;
                                            var aggressorCorpName = names.First(x => x.Key == aggressorCorpID).Value;
                                            var aggressorName = names.First(x => x.Key == aggressorID).Value;
                                            var moonName = names.First(x => x.Key == moonID).Value;
                                            var solarSystemName = names.First(x => x.Key == solarSystemID).Value;
                                            var allyLine = aggressorAllianceID != 0 ? $"{Environment.NewLine}Aggressing Alliance: {aggressorAlliance}" : "";
                                            var TypeName = await EveLib.IDtoTypeName(new List<Int64> { typeID });

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}Starbase is under attack{Environment.NewLine}{Environment.NewLine}" +
                                                $"Details{Environment.NewLine}```{Environment.NewLine}System: {moonName}{Environment.NewLine}" +
                                                $"Type: {TypeName.First(x => x.Key == typeID).Value}{Environment.NewLine}{Environment.NewLine}" +
                                                $"Current Shield Level: {shieldValue}{Environment.NewLine}Current Armor Level: {armorValue}{Environment.NewLine}" +
                                                $"Current Hull Level: {hullValue}{Environment.NewLine}{Environment.NewLine}" +
                                                $"Aggressing Pilot: {aggressorName}{Environment.NewLine}Aggressing Corporation: {aggressorCorpName}{allyLine}```");
                                        }
                                        else if (notificationType == 100)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var allyID = Convert.ToInt64(notificationText["allyID"].AllNodes.ToList()[0].ToString());
                                            var defenderID = Convert.ToInt64(notificationText["defenderID"].AllNodes.ToList()[0].ToString());

                                            var stuff = await EveLib.IDtoName(new List<Int64> { allyID, defenderID });
                                            var allyName = stuff.FirstOrDefault(x => x.Key == allyID).Value;
                                            var defenderName = stuff.FirstOrDefault(x => x.Key == defenderID).Value;
                                            var startTime = DateTime.FromFileTimeUtc(Convert.ToInt64(notificationText["startTime"].AllNodes.ToList()[0].ToString()));

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}{allyName} will join the war against {defenderName} at {startTime} EVE.");
                                        }
                                        else if (notificationType == 121)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var aggressorID = Convert.ToInt64(notificationText["entityID"].AllNodes.ToList()[0].ToString());
                                            var defenderID = Convert.ToInt64(notificationText["defenderID"].AllNodes.ToList()[0].ToString());

                                            var stuff = await EveLib.IDtoName(new List<Int64> { aggressorID, defenderID });
                                            var aggressorName = stuff.FirstOrDefault(x => x.Key == aggressorID).Value;
                                            var defenderName = stuff.FirstOrDefault(x => x.Key == defenderID).Value;

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}War declared by {aggressorName} against {defenderName}. Fighting begins in roughly 24 hours.");
                                        }
                                        else if (notificationType == 147)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
                                            var structureTypeID = Convert.ToInt64(notificationText["structureTypeID"].AllNodes.ToList()[0].ToString());
                                            var names = await EveLib.IDtoName(new List<Int64> { solarSystemID });
                                            var typeNames = await EveLib.IDtoTypeName(new List<Int64> { structureTypeID });
                                            var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);
                                            var structureTypeName = typeNames.FirstOrDefault(x => x.Key == structureTypeID);

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}Entosis Link started in {solarSystemName.Value} on {structureTypeName.Value}");
                                        }
                                        else if (notificationType == 160)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var campaignEventType = notificationText["campaignEventType"].AllNodes.ToList()[0];
                                            var solarSystemID = Convert.ToInt64((notificationText["solarSystemID"].AllNodes.ToList()[0].ToString()));
                                            var decloakTime = Convert.ToInt64(notificationText["decloakTime"].AllNodes.ToList()[0].ToString());
                                            var names = await EveLib.IDtoName(new List<Int64> { solarSystemID });
                                            var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);
                                            var decloaktime = DateTime.FromFileTime(decloakTime);

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}Sovereignty structure reinforced in {solarSystemName.Value} nodes will spawn @{decloaktime}");
                                        }
                                        else if (notificationType == 161)
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]}"));
                                            var campaignEventType = notificationText["campaignEventType"].AllNodes.ToList()[0];
                                            var constellationID = notificationText["constellationID"].AllNodes.ToList()[0];
                                            var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
                                            var names = await EveLib.IDtoName(new List<Int64> { solarSystemID });
                                            var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);

                                            await chan.SendMessageAsync($"@everyone {Environment.NewLine}Command nodes decloaking for {solarSystemName.Value}");

                                        }
                                        else
                                        {
                                            try
                                            {
                                                await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Skipping Notification TypeID: {notificationType} " +
                                                    $"Type: {types[notificationType]} {Environment.NewLine} Text: {notificationText}"));
                                            }
                                            catch (KeyNotFoundException)
                                            {
                                                await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Skipping **NEW** Notification TypeID: {notificationType} " +
                                                    $"{Environment.NewLine} Text: {notificationText}"));
                                            }
                                        }
                                        _lastNotification = (int)notification.Value["notificationID"];
                                        await SQLiteDataUpdate("cacheData", "data", "lastNotificationID", _lastNotification.ToString());
                                        runComplete = true;
                                    }
                                }
                            }
                            if (keyCount > 1 && keyCount != index + 1)
                            {
                                await SQLiteDataUpdate("notifications", "data", "nextKey", keys.ToList()[index + 1].Key);
                            }
                            else if (keyCount == index + 1)
                            {
                                await SQLiteDataUpdate("notifications", "data", "nextKey", keys.ToList()[0].Key);
                            }
                            else
                            {
                                await SQLiteDataUpdate("notifications", "data", "nextKey", key.Key);
                            }
                        }
                        var interval = 30 / keyCount;
                        await SQLiteDataUpdate("cacheData", "data", "nextNotificationCheck", DateTime.Now.AddMinutes(interval).ToString());
                        _nextNotificationCheck = DateTime.Now.AddMinutes(interval);

                    }
                }
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "NotificationFeed", ex.Message, ex));
            }
        }
        #endregion

        //Complete
        #region Pricecheck
        internal async static Task PriceCheck(ICommandContext context, string String, string system)
        {
            //var NametoId = "https://esi.tech.ccp.is/latest/search/?categories=inventorytype&datasource=tranquility&language=en-us&search=Plex&strict=true";

            var channel = context.Message.Channel;
            if (String.ToLower() == "short name")
            {
                String = "Item Name";
            }

            var result = await Program._httpClient.GetStringAsync($"https://esi.tech.ccp.is/latest/search/?categories=inventorytype&datasource=tranquility&language=en-us&search={String}&strict=true");

            var searchResults = JsonConvert.DeserializeObject<SearchInventoryType>(result);

            if (string.IsNullOrWhiteSpace(searchResults.Inventorytype.ToString()))
            {
                await channel.SendMessageAsync($"{context.Message.Author.Mention} Item {String} does not exist please try again");

            }
            else
            {
                try
                {
                    if (system == "")
                    {
                        var eveCentralReply = await Program._httpClient.GetStringAsync($"http://api.eve-central.com/api/marketstat/json?typeid={searchResults.Inventorytype[0]}");
                        var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];
                        await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                        await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: **Universe**{Environment.NewLine}" +
                            $"**Buy:**{Environment.NewLine}" +
                            $"```Low: {centralreply.Buy.Min :n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Buy.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Buy.Max:n2}```" +
                            $"{Environment.NewLine}" +
                            $"**Sell**:{Environment.NewLine}" +
                            $"```Low: {centralreply.Sell.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Sell.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Sell.Max:n2}```");
                    }
                    if (system == "jita")
                    {
                        var eveCentralReply = await Program._httpClient.GetStringAsync($"http://api.eve-central.com/api/marketstat/json?typeid={searchResults.Inventorytype[0]}&usesystem=30000142");
                        var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];
                        await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                        await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: **Universe**{Environment.NewLine}" +
                            $"**Buy:**{Environment.NewLine}" +
                            $"```Low: {centralreply.Buy.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Buy.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Buy.Max:n2}```" +
                            $"{Environment.NewLine}" +
                            $"**Sell**:{Environment.NewLine}" +
                            $"```Low: {centralreply.Sell.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Sell.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Sell.Max:n2}```");
                    }
                    if (system == "amarr")
                    {
                        var eveCentralReply = await Program._httpClient.GetStringAsync($"http://api.eve-central.com/api/marketstat/json?typeid={searchResults.Inventorytype[0]}&usesystem=30002187");
                        var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];
                        await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                        await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: **Universe**{Environment.NewLine}" +
                            $"**Buy:**{Environment.NewLine}" +
                            $"```Low: {centralreply.Buy.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Buy.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Buy.Max:n2}```" +
                            $"{Environment.NewLine}" +
                            $"**Sell**:{Environment.NewLine}" +
                            $"```Low: {centralreply.Sell.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Sell.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Sell.Max:n2}```");
                    }
                    if (system == "rens")
                    {
                        var eveCentralReply = await Program._httpClient.GetStringAsync($"http://api.eve-central.com/api/marketstat/json?typeid={searchResults.Inventorytype[0]}&usesystem=30002510");
                        var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];
                        await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                        await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: **Universe**{Environment.NewLine}" +
                            $"**Buy:**{Environment.NewLine}" +
                            $"```Low: {centralreply.Buy.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Buy.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Buy.Max:n2}```" +
                            $"{Environment.NewLine}" +
                            $"**Sell**:{Environment.NewLine}" +
                            $"```Low: {centralreply.Sell.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Sell.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Sell.Max:n2}```");
                    }
                    if (system == "dodixie")
                    {
                        var eveCentralReply = await Program._httpClient.GetStringAsync($"http://api.eve-central.com/api/marketstat/json?typeid={searchResults.Inventorytype[0]}&usesystem=30002659");
                        var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];
                        await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                        await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: **Universe**{Environment.NewLine}" +
                            $"**Buy:**{Environment.NewLine}" +
                            $"```Low: {centralreply.Buy.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Buy.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Buy.Max:n2}```" +
                            $"{Environment.NewLine}" +
                            $"**Sell**:{Environment.NewLine}" +
                            $"```Low: {centralreply.Sell.Min:n2}{Environment.NewLine}" +
                            $"Avg: {centralreply.Sell.Avg:n2}{Environment.NewLine}" +
                            $"High: {centralreply.Sell.Max:n2}```");
                    }
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "PC", ex.Message, ex));
                }
            }
        }
        #endregion

        //Time
        #region Time
        internal async static Task EveTime(ICommandContext context)
        {
            try
            {
                var format = Program.Settings.GetSection("config")["timeformat"];
                var utcTime = DateTime.UtcNow.ToString(format);
                await context.Message.Channel.SendMessageAsync($"{context.Message.Author.Mention} Current EVE Time is {utcTime}");
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "EveTime", ex.Message, ex));
            }
        }
        #endregion

        //MOTD
        #region MOTD
        internal async static Task MOTD(ICommandContext context)
        {
            try
            {
                var keyID = Program.Settings.GetSection("motd")["motdkeyID"];
                var vCode = Program.Settings.GetSection("motd")["motdvCode"];
                var CharID = Program.Settings.GetSection("motd")["motdcharid"];
                await EveLib.SetMOTDKey(keyID, vCode, CharID);

                var chanName = Program.Settings.GetSection("motd")["MOTDChan"];

                var rowlist = await EveLib.GetChatChannels();
                foreach (var r in rowlist)
                {
                    var ChName = r["displayName"];
                    string Channel = ChName.ToString();
                    string ChannelName = chanName.ToString();
                    if (Channel == ChannelName)
                    {
                        var comments = r["motd"];
                        string com = comments.ToString();
                        com = com.Replace("<br>", " \n ")
                            .Replace("<u>", "__").Replace("</u>", "__")
                            .Replace("<b>", "**").Replace("</b>", "**")
                            .Replace("<i>", "*").Replace("</i>", "*")
                            .Replace("&amp", "&");

                        com = StripTagsCharArray(com);
                        com = com.Replace("&lt;", "<").Replace("&gt;", ">");

                        var restricted = Convert.ToUInt64(Program.Settings.GetSection("config")["restricted"]);
                        var channel = Convert.ToUInt64(context.Channel.Id);
                        if (channel == restricted)
                        {
                            await context.Message.Channel.SendMessageAsync($" {context.Message.Author.Mention} I cant do that *here.*");
                        }
                        else
                        {
                            await context.Message.Channel.SendMessageAsync($"{context.Message.Author.Mention}{Environment.NewLine}{com}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "MOTD", ex.Message, ex));
            }
        }
        #endregion

        //Update Topic
        #region Update Topic
        internal async static Task TopicMOTD(ICommandContext context)
        {
            try
            {
                if (DateTime.Now > _lastTopicCheck.AddMilliseconds(Convert.ToInt32(Program.Settings.GetSection("motd")["topicInterval"]) * 1000 * 60))
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "CheckTopic", "Running Topic Check"));
                    _motdtopic = Convert.ToString(await SQLiteDataQuery("cacheData", "data", "motd"));
                    {
                        var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                        var channelId = Convert.ToUInt64(Program.Settings.GetSection("motd")["motdtopicchan"]);
                        var chan1 = Program.Client.GetGuild(guildID).GetTextChannel(channelId);
                        var keyID = Program.Settings.GetSection("motd")["motdkeyID"];
                        var vCode = Program.Settings.GetSection("motd")["motdvCode"];
                        var CharID = Program.Settings.GetSection("motd")["motdcharid"];
                        await EveLib.SetMOTDKey(keyID, vCode, CharID);

                        var chanName = Program.Settings.GetSection("motd")["MOTDChan"];

                        var rowlist = await EveLib.GetChatChannels();
                        foreach (var r in rowlist)
                        {
                            var ChName = r["displayName"];
                            string Channel = ChName.ToString();
                            string ChannelName = chanName.ToString();
                            if (Channel == ChannelName)
                            {
                                var comments = r["motd"];
                                string com = comments.ToString();
                                com = com.Replace("<br>", " \n ")
                                    .Replace("<u>", "__").Replace("</u>", "__")
                                    .Replace("<b>", "**").Replace("</b>", "**")
                                    .Replace("<i>", "*").Replace("</i>", "*")
                                    .Replace("&amp", "&");

                                com = StripTagsCharArray(com);
                                com = com.Replace("&lt;", "<").Replace("&gt;", ">");

                                if (com != _motdtopic)
                                {
                                    var chanid = Convert.ToUInt64(Program.Settings.GetSection("motd")["motdtopicchan"]);
                                    var chan = (ITextChannel)Program.Client.Guilds.FirstOrDefault().GetTextChannel(chanid); ;

                                    await SQLiteDataUpdate("cacheData", "data", "motd", com.ToString());
                                    await chan.ModifyAsync(x => x.Topic = com);
                                    await chan1.SendMessageAsync($"@everyone Channel topic has been updated..");
                                }
                            }
                        }
                    }
                    _lastTopicCheck = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "MOTDTopic", ex.Message, ex));
            }
        }
        #endregion

        //FleetUp Baby
        #region FleetUp
        internal static async Task FleetUp()
        {
            //Check Fleetup Operations
            var lastChecked = await SQLiteDataQuery("cacheData", "data", "fleetUpLastChecked");

            if (DateTime.Now > DateTime.Parse(lastChecked).AddMinutes(1))
            {
                var UserId = Program.Settings.GetSection("fleetup")["UserId"];
                var APICode = Program.Settings.GetSection("fleetup")["APICode"];
                var GroupID = Program.Settings.GetSection("fleetup")["GroupID"];
                var channelid = Convert.ToUInt64(Program.Settings.GetSection("fleetup")["channel"]);
                var guildId = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                var lastopid = await SQLiteDataQuery("cacheData", "data", "fleetUpLastPostedOperation");

                var Json = await Program._httpClient.GetStringAsync($"http://api.fleet-up.com/Api.svc/Ohigwbylcsuz56ue3O6Awlw5e/{UserId}/{APICode}/Operations/{GroupID}");
                var result = JsonConvert.DeserializeObject<Fleetupapi>(Json);
                foreach (var operation in result.Data)
                {
                    if ((int)operation.OperationId > Convert.ToInt32(lastopid))
                    {
                        var name = operation.Subject;
                        var startTime = operation.Start;
                        var locationinfo = operation.LocationInfo;
                        var location = operation.Location;
                        var details = operation.Details;
                        var url = $"http://fleet-up.com/Operation#{operation.OperationId}";

                        var channel = Program.Client.GetGuild(guildId).GetTextChannel(channelid);

                        var message = $"@everyone {Environment.NewLine}{Environment.NewLine}" +
                            $"**New Operation Posted** {Environment.NewLine}{Environment.NewLine}" +
                            $"```Title - {name} {Environment.NewLine}" +
                            $"Form Up Time - {startTime} {Environment.NewLine}" +
                            $"Form Up System - {location} - {locationinfo} {Environment.NewLine}" +
                            $"Details - {details}{Environment.NewLine}" +
                            $"```{Environment.NewLine}{url}";

                        var sendres = await channel.SendMessageAsync(message);

                        await sendres.AddReactionAsync(new Emoji("✅"));
                        await sendres.AddReactionAsync(new Emoji("❔"));
                        await sendres.AddReactionAsync(new Emoji("❌"));

                        await SQLiteDataUpdate("cacheData", "data", "fleetUpLastPostedOperation", operation.OperationId.ToString());
                    }
                }
                await SQLiteDataUpdate("cacheData", "data", "fleetUpLastChecked", DateTime.Now.ToString());
            }

        }

        internal static async Task Ops(ICommandContext context)
        {
            var UserId = Program.Settings.GetSection("fleetup")["UserId"];
            var APICode = Program.Settings.GetSection("fleetup")["APICode"];
            var GroupID = Program.Settings.GetSection("fleetup")["GroupID"];
            var channelid = Convert.ToUInt64(Program.Settings.GetSection("fleetup")["channel"]);
            var guildId = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
            var lastopid = await SQLiteDataQuery("cacheData", "data", "fleetUpLastPostedOperation");

            var channel = Program.Client.GetGuild(guildId).GetChannel(channelid);

            var Json = await Program._httpClient.GetStringAsync($"http://api.fleet-up.com/Api.svc/Ohigwbylcsuz56ue3O6Awlw5e/{UserId}/{APICode}/Operations/{GroupID}");
            var result = JsonConvert.DeserializeObject<Fleetupapi>(Json);
            var message = $"{context.Message.Author.Mention}, {Environment.NewLine}";
            var count = message.Count();
            if (result.Data.Count() == 0)
            {
                await context.Message.Channel.SendMessageAsync($"{message}No Ops Scheduled");
            }
            else
            {
                foreach (var operation in result.Data)
                {
                    var name = operation.Subject;
                    var startTime = operation.StartString;
                    var locationinfo = operation.LocationInfo;
                    var location = operation.Location;
                    var details = operation.Details;
                    var url = $"http://fleet-up.com/Operation#{operation.OperationId}";

                    var message_temp = $"```Title - {name} {Environment.NewLine}" +
                                $"Form Up Time - {startTime} {Environment.NewLine}" +
                                $"Form Up System - {location} - {locationinfo} {Environment.NewLine}" +
                                $"Details - {details}```" +
                                $"{url}{Environment.NewLine}{Environment.NewLine}";

                    if (message.Count() + message_temp.Count() >= 2000)
                    {
                        if (message.Count() != count)
                        {
                            await context.Message.Channel.SendMessageAsync($"{message}");
                            message = $"{context.Message.Author.Mention}, {Environment.NewLine}";
                        }
                        else
                        {
                            message += $"{message_temp}";
                            await context.Message.Channel.SendMessageAsync($"{message}");
                            message = $"{context.Message.Author.Mention}, {Environment.NewLine}";
                        }
                    }
                    else
                    {
                        message += $"{message_temp}";
                    }
                }
            }
            if (message != $"{context.Message.Author.Mention}, {Environment.NewLine}")
                await context.Message.Channel.SendMessageAsync($"{message}");

            await Client_Log(new LogMessage(LogSeverity.Info, "FleetOps", $"Sending Ops to {context.Message.Channel} for {context.Message.Author}"));
        }
        #endregion

        //Jabber Broadcasts
        #region Jabber
        internal static async Task Jabber()
        {
            var username = Program.Settings.GetSection("jabber")["username"];
            var password = Program.Settings.GetSection("jabber")["password"];
            var domain = Program.Settings.GetSection("jabber")["domain"];

            if (!_jabberRunning)
            {
                try
                {
                    var xmppWrapper = new ReconnectXmppWrapper(domain, username, password);
                    xmppWrapper.Connect(null);
                    _jabberRunning = true;
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "Jabber", ex.Message, ex));
                }
            }


        }

        internal static async void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Chatstate != Chatstate.Composing && !string.IsNullOrWhiteSpace(e.Message.Value))
            {
                if (Convert.ToBoolean(Program.Settings.GetSection("jabber").GetSection("filter").Value))
                {
                    foreach (var filter in Program.Settings.GetSection("jabber").GetSection("filters").GetChildren().ToList())
                    {
                        if (e.Message.Value.ToLower().Contains(filter.Key.ToLower()))
                        {
                            var prepend = Program.Settings.GetSection("jabber")["prepend"];
                            var channel = Program.Client.GetGuild(Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"])).GetTextChannel(Convert.ToUInt64(filter.Value));
                            await channel.SendMessageAsync($"{prepend + Environment.NewLine}From: {e.Message.From.User} {Environment.NewLine} Message: ```{e.Message.Value}```");
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(e.Message.Value))
                {
                    var prepend = Program.Settings.GetSection("jabber")["prepend"];
                    var channel = Program.Client.GetGuild(Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"])).GetTextChannel(Convert.ToUInt64(Program.Settings.GetSection("jabber")["defchan"]));
                    await channel.SendMessageAsync($"{prepend + Environment.NewLine}From: {e.Message.From.User} {Environment.NewLine} Message: ```{e.Message.Value}```");
                }
            }
        }
        #endregion

        //About
        #region About
        internal async static Task About(ICommandContext context)
        {
            if (AppContext.BaseDirectory.Contains("netcoreapp2.0"))
            {
                var directory = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(
                Directory.GetParent(AppContext.BaseDirectory).FullName).FullName).FullName).FullName).FullName);
            }
            else
            {
                var directory = Path.Combine(AppContext.BaseDirectory);
            }
            //using (var repo = new Repository(directory))
            //{
            var channel = context.Channel;
            var botid = Program.Client.CurrentUser.Id;
            var MemoryUsed = ByteSize.FromBytes(Process.GetCurrentProcess().PrivateMemorySize64);
            var RunTime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var Guilds = Program.Client.Guilds.Count;
            var TotalUsers = 0;
            foreach (var guild in Program.Client.Guilds)
            {
                TotalUsers = TotalUsers + guild.Users.Count;
            }

            await channel.SendMessageAsync($"{context.User.Mention},{Environment.NewLine}{Environment.NewLine}" +
                $"```Developer: Jimmy06 (In-game Name: Jimmy06){Environment.NewLine}{Environment.NewLine}" +
                $"Bot ID: {botid}{Environment.NewLine}{Environment.NewLine}" +
                //$"Current Version: {repo.Head.Tip.Id}{Environment.NewLine}" +
                //$"Current Branch: {repo.Head.FriendlyName}{Environment.NewLine}" +
                $"Run Time: {RunTime.Days}:{RunTime.Hours}:{RunTime.Minutes}:{RunTime.Seconds}{Environment.NewLine}{Environment.NewLine}" +
                $"Statistics:{Environment.NewLine}" +
                $"Memory Used: {Math.Round(MemoryUsed.LargestWholeNumberValue, 2)} {MemoryUsed.LargestWholeNumberSymbol}{Environment.NewLine}" +
                $"Total Connected Guilds: {Guilds}{Environment.NewLine}" +
                $"Total Users Seen: {TotalUsers}```");
            //$"GitHub URL: <{repo.Config.ToList().FirstOrDefault(x => x.Key == "remote.origin.url").Value}>");
            //}

            await Task.CompletedTask;
        }
        #endregion

        //Char
        #region Char
        internal async static Task Char(ICommandContext context, string x)
        {
            var channel = context.Channel;
            var responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/search/?categories=character&datasource=tranquility&language=en-us&search={x}&strict=false");
            var characterID = JsonConvert.DeserializeObject<CharacterID>(await responceMessage.Content.ReadAsStringAsync()).Character.FirstOrDefault();
            responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/characters/{characterID}/?datasource=tranquility");
            var characterData = JsonConvert.DeserializeObject<CharacterData>(await responceMessage.Content.ReadAsStringAsync());
            responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/corporations/{characterData.Corporation_id}/?datasource=tranquility");
            var corporationData = JsonConvert.DeserializeObject<CorporationData>(await responceMessage.Content.ReadAsStringAsync());
            responceMessage = await Program._httpClient.GetAsync($"https://zkillboard.com/api/kills/characterID/{characterID}/");
            var zkillContent = JsonConvert.DeserializeObject<List<Kill>>(await responceMessage.Content.ReadAsStringAsync());
            Kill zkillLast = zkillContent.Count > 0 ? zkillContent[0] : new Kill();

            responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/universe/systems/{zkillLast.SolarSystemID}/?datasource=tranquility&language=en-us");
            var systemData = JsonConvert.DeserializeObject<SystemData>(await responceMessage.Content.ReadAsStringAsync());

            var lastShipType = "Unknown";

            if (zkillLast.Victim != null && zkillLast.Victim.CharacterID == characterID)
            {
                lastShipType = zkillLast.Victim.ShipTypeID.ToString();
            }
            else if(zkillLast.Victim != null)
            {
                foreach (var attacker in zkillLast.Attackers)
                {
                    if (attacker.CharacterID == characterID)
                    {
                        lastShipType = attacker.ShipTypeID.ToString();
                    }
                }
            }

            responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/universe/types/{lastShipType}/?datasource=tranquility&language=en-us");
            var lastShip = JsonConvert.DeserializeObject<Ship>(await responceMessage.Content.ReadAsStringAsync());
            var lastSeen = zkillLast.KillTime;

            responceMessage = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/alliances/{characterData.Alliance_id}/?datasource=tranquility");
            var allianceData = JsonConvert.DeserializeObject<AllianceData>(await responceMessage.Content.ReadAsStringAsync());
            var alliance = allianceData.Alliance_name ?? "None";

            await channel.SendMessageAsync($"```Name: {characterData.Name}{Environment.NewLine}" +
                $"DOB: {characterData.Birthday}{Environment.NewLine}{Environment.NewLine}" +
                $"Corporation Name: {corporationData.Corporation_name}{Environment.NewLine}" +
                $"Alliance Name: {alliance}{Environment.NewLine}{Environment.NewLine}" +
                $"Last System: {systemData.Name}{Environment.NewLine}" +
                $"Last Ship: {lastShip.Name}{Environment.NewLine}" +
                $"Last Seen: {lastSeen}{Environment.NewLine}```" +
                $"ZKill: https://zkillboard.com/character/{characterID}/");

            responceMessage.Dispose();

            await Task.CompletedTask;
        }
        #endregion

        //Corp
        #region Corp
        internal async static Task Corp(ICommandContext context, string x)
        {
            var channel = context.Channel;
            var responceMessage = await Program._httpClient.GetStringAsync(
                $"https://esi.tech.ccp.is/latest/search/?categories=corporation&datasource=tranquility&language=en-us&search={x}&strict=true");
            var corpContent = JsonConvert.DeserializeObject<CorporationSearch>(responceMessage);

            if (corpContent.Corporation == null)
            {
                await channel.SendMessageAsync($"{context.User.Mention}, Corp not found please try again");
            }
            else
            {
                responceMessage = await Program._httpClient.GetStringAsync($"https://esi.tech.ccp.is/latest/corporations/{corpContent.Corporation[0]}/?datasource=tranquility");
                var CorpDetailsContent = JsonConvert.DeserializeObject<CorporationData>(responceMessage);
                responceMessage = await Program._httpClient.GetStringAsync($"https://esi.tech.ccp.is/latest/characters/{CorpDetailsContent.Ceo_id}/?datasource=tranquility");
                var CEONameContent = JsonConvert.DeserializeObject<CharacterData>(responceMessage);
                responceMessage = await Program._httpClient.GetStringAsync($"https://esi.tech.ccp.is/latest/alliances/{CorpDetailsContent.Alliance_id}/?datasource=tranquility");
                var allyContent = JsonConvert.DeserializeObject<AllianceData>(responceMessage);
                var alliance = allyContent.Alliance_name;

                await channel.SendMessageAsync($"```Corp Name: {CorpDetailsContent.Corporation_name}{Environment.NewLine}" +
                        $"Corp Ticker: {CorpDetailsContent.Ticker}{Environment.NewLine}" +
                        $"CEO: {CEONameContent.Name}{Environment.NewLine}" +
                        $"Alliance Name: {alliance}{Environment.NewLine}" +
                        $"Member Count: {CorpDetailsContent.Member_count}{Environment.NewLine}```" +
                        $"ZKill: https://zkillboard.com/corporation/{corpContent.Corporation[0]}/");
            }

            await Task.CompletedTask;
        }
        #endregion

        //Discord Stuff
        #region Discord Modules
        internal static async Task InstallCommands()
        {
            Program.Client.MessageReceived += HandleCommand;
            await Program.Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        internal static async Task HandleCommand(SocketMessage messageParam)
        {

            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (!(message.HasCharPrefix(Program.Settings.GetSection("config")["commandprefix"].ToCharArray()[0], ref argPos) || message.HasMentionPrefix
                    (Program.Client.CurrentUser, ref argPos))) return;

            var context = new CommandContext(Program.Client, message);

            var result = await Program.Commands.ExecuteAsync(context, argPos, Program.ServiceCollection);
            if (!result.IsSuccess && result.ErrorReason == "Unknown command.")
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
        #endregion

        //Complete
        #region MysqlQuery
        internal static async Task<IList<IDictionary<string, object>>> MysqlQuery(string connstring, string query)
        {
            using (MySqlConnection conn = new MySqlConnection(connstring))
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                List<IDictionary<string, object>> list = new List<IDictionary<string, object>>(); ;
                cmd.CommandText = query;
                try
                {
                    conn.ConnectionString = connstring;
                    conn.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            var record = new Dictionary<string, object>();

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                var key = reader.GetName(i);
                                var value = reader[i];
                                record.Add(key, value);
                            }

                            list.Add(record);
                        }

                        return list;
                    }
                }
                catch (MySqlException ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "mySQL", query + " " + ex.Message, ex));
                }
                await Task.Yield();
                return list;
            }
        }
        #endregion

        //SQLite Query
        #region SQLiteQuery
        internal async static Task<string> SQLiteDataQuery(string table, string field, string name)
        {
            using (SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};"))
            using (SqliteCommand querySQL = new SqliteCommand($"SELECT {field} FROM {table} WHERE name = @name", con))
            {
                await con.OpenAsync();
                querySQL.Parameters.Add(new SqliteParameter("@name", name));
                try
                {
                    using (SqliteDataReader r = await querySQL.ExecuteReaderAsync())
                    {
                        var result = await r.ReadAsync();
                        return r.GetString(0) ?? "";
                    }
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                    return null;
                }
            }
        }
        internal async static Task<List<int>> SQLiteDataQuery(string table)
        {
            using (SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};"))
            using (SqliteCommand querySQL = new SqliteCommand($"SELECT * FROM {table}", con))
            {
                await con.OpenAsync();
                try
                {
                    using (SqliteDataReader r = await querySQL.ExecuteReaderAsync())
                    {
                        var list = new List<int>();
                        while (await r.ReadAsync())
                        {
                            list.Add(Convert.ToInt32(r["Id"]));
                        }

                        return list;
                    }
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                    return null;
                }
            }
        }
        #endregion

        //SQLite Update
        #region SQLiteUpdate
        internal async static Task SQLiteDataUpdate(string table, string field, string name, string data)
        {
            using (SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};"))
            using (SqliteCommand insertSQL = new SqliteCommand($"UPDATE {table} SET {field} = @data WHERE name = @name", con))
            {
                await con.OpenAsync();
                insertSQL.Parameters.Add(new SqliteParameter("@name", name));
                insertSQL.Parameters.Add(new SqliteParameter("@data", data));
                try
                {
                    insertSQL.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                }
            }
        }
        #endregion

        //SQLite Delete
        #region SQLiteDelete
        internal async static Task SQLiteDataDelete(string table, string name)
        {
            using (SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};"))
            using (SqliteCommand insertSQL = new SqliteCommand($"REMOVE FROM {table} WHERE name = @name", con))
            {
                await con.OpenAsync();
                insertSQL.Parameters.Add(new SqliteParameter("@name", name));
                try
                {
                    insertSQL.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                }
            }
        }
        #endregion

        //StripHTML Tags From string
        #region StripHTML
        /// <summary>
        /// Remove HTML from string with Regex.
        /// </summary>
        public static string StripTagsRegex(string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }

        /// <summary>
        /// Compiled regular expression for performance.
        /// </summary>
        static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        /// <summary>
        /// Remove HTML from string with compiled Regex.
        /// </summary>
        public static string StripTagsRegexCompiled(string source)
        {
            return _htmlRegex.Replace(source, string.Empty);
        }

        /// <summary>
        /// Remove HTML tags from string using char array.
        /// </summary>
        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
        #endregion

    }
}
