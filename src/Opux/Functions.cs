using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Xml;
using JSONStuff;
using System.Threading.Tasks;

namespace Opux
{
    internal class Functions
    {
        internal static DateTime lastAuthCheck = DateTime.Now;
        internal static DateTime lastFeedCheck = DateTime.Now;
        internal static DateTime nextNotificationCheck = DateTime.FromFileTime(0);
        internal static int lastNotification;
        internal static bool avaliable = false;
        internal static bool running = false;
        internal static bool authRunning = false;

        //Timer is setup here
        #region Timer stuff
        public static void RunTick(Object stateInfo)
        {
            try
            {
                if (!running && avaliable)
                {
                    running = true;
                    Async_Tick(stateInfo).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Client_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
            }
        }

        private async static Task Async_Tick(object args)
        {
            try
            {
                if (Convert.ToBoolean(Program.Settings.GetSection("config")["authWeb"]))
                {
                    if (!authRunning)
                    {
                        await AuthWeb();
                        await Task.Delay(1000);
                    }
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

                running = false;
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
                running = false;
            }
        }
        #endregion

        //Needs logging to a file added
        #region Logger
        internal static Task Client_Log(LogMessage arg)
        {
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
            Console.WriteLine($"{DateTime.Now,-19} [{arg.Severity,8}] [{arg.Source}]: {arg.Message}");
            if (arg.Exception != null)
            {
                Console.WriteLine(arg.Exception?.StackTrace);
            }
            Console.ForegroundColor = cc;
            return Task.CompletedTask;
        }
        #endregion

        //Events are attached here
        #region EVENTS
        internal static Task Event_GuildAvaliable(SocketGuild arg)
        {
            avaliable = true;
            arg.CurrentUser.ModifyAsync(x => x.Nickname = Program.Settings.GetSection("config")["name"]);
            return Task.CompletedTask;
        }

        internal static Task Event_UserJoined(SocketGuildUser arg)
        {
            var channel = (ITextChannel)arg.Guild.DefaultChannel;
            var authurl = Program.Settings.GetSection("auth")["authurl"];
            if (!String.IsNullOrWhiteSpace(authurl))
            {
                channel.SendMessageAsync($"Welcome {arg.Mention} to the server, To gain access please auth at {authurl} ");
            }
            else
            {
                channel.SendMessageAsync($"Welcome {arg.Mention} to the server");
            }

            return Task.CompletedTask;
        }

        internal static Task Event_Disconnected(Exception arg)
        {
            avaliable = false;
            return Task.CompletedTask;
        }

        internal static Task Event_Connected()
        {
            return Task.CompletedTask;
        }

        internal static Task Event_LoggedIn()
        {
            avaliable = true;
            return Task.CompletedTask;
        }

        internal static Task Event_LoggedOut()
        {
            avaliable = false;
            return Task.CompletedTask;
        }
        #endregion

        //Auth
        #region AuthWeb
        internal async static Task AuthWeb()
        {
            await Client_Log(new LogMessage(LogSeverity.Info, "AuthWeb", "Starting AuthWeb Server"));
            authRunning = true;
            var callbackurl = (string)Program.Settings.GetSection("auth")["callbackurl"];
            var client_id = (string)Program.Settings.GetSection("auth")["client_id"];
            var secret = (string)Program.Settings.GetSection("auth")["secret"];
            var url = (string)Program.Settings.GetSection("auth")["url"];
            var port = Convert.ToInt32(Program.Settings.GetSection("auth")["port"]);
            HttpListener listener = new HttpListener(IPAddress.Any, port);
            if (!listener.IsListening)
            {
                try
                {
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
                                                string i = (allianceid.IsNullOrEmpty() ? "0" : allianceid.ToString());
                                                string c = (corporationid.IsNullOrEmpty() ? "0" : corporationid.ToString());
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
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "authWeb", ex.Message, ex));
                }
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

                var CharacterID = responce[0]["characterID"].ToString();
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
                        string i = (allianceid.IsNullOrEmpty() ? "0" : allianceid.ToString());
                        string c = (corporationid.IsNullOrEmpty() ? "0" : corporationid.ToString());
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

                            var discordGuild = (SocketGuild)context.Guild;
                            var discordUser = (SocketGuildUser)context.Message.Author;

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
                                    var channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == alertChannel);
                                    await channel.SendMessageAsync($"Granting Roles to {characterDetails["name"]}");
                                    await discordUser.AddRolesAsync(rolesToAdd);
                                }
                            }
                            var query2 = $"UPDATE pendingUsers SET active=\"0\" WHERE authString=\"{remainder}\"";
                            var responce2 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query2);

                            await context.Channel.SendMessageAsync($"{context.Message.Author.Mention},:white_check_mark: **Success**: " +
                                $"{characterDetails["name"]} has been successfully authed.");

                            var eveName = characterDetails["name"];
                            var characterID = CharacterID;
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
                                    Nickname = $"[{corporationDetails["ticker"]}] ";
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
                            await Client_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Failed adding Roles to User {characterDetails["name"]}, Reason: {ex.Message}", ex));
                        }
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
            if (DateTime.Now > lastAuthCheck.AddMilliseconds(Convert.ToInt32(Program.Settings.GetSection("config")["authInterval"]) * 1000 * 60) || Context != null)
            {
                try
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Running Auth Check"));
                    //Gather details about corps and alliance's to set roles for
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

                    string query = "select * from authUsers";
                    var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
                    foreach (var u in responce)
                    {
                        var characterID = u["characterID"];
                        var discordID = u["discordID"];
                        var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                        var logchan = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);
                        JObject characterDetails;
                        JObject corporationDetails;
                        JObject allianceDetails;

                        using (HttpClient webclient = new HttpClient())
                        using (HttpResponseMessage _characterDetails = await webclient.GetAsync($"https://esi.tech.ccp.is/latest/characters/{characterID}"))
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
                                string i = (allianceid.IsNullOrEmpty() ? "0" : allianceid.ToString());
                                string c = (corporationid.IsNullOrEmpty() ? "0" : corporationid.ToString());
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

                            var discordGuild = Program.Client.Guilds.FirstOrDefault(X => X.Id == guildID);

                            var discordUser = discordGuild.Users.FirstOrDefault(x => x.Id == Convert.ToUInt64(u["discordID"]));

                            if (discordUser == null)
                            {
                                string remquery = $"DELETE FROM authUsers WHERE discordID = {u["discordID"]}";
                                var remresponce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], remquery);
                                await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Removing {characterDetails["name"]} from Database as they have left discord"));
                                continue;
                            }
                            else
                            {
                                var rolesToAdd = new List<SocketRole>();
                                var rolesToTake = new List<SocketRole>();

                                try
                                {
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
                                            var channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == logchan);
                                            await channel.SendMessageAsync($"Granting Roles to {characterDetails["name"]}");
                                            await discordUser.AddRolesAsync(rolesToAdd);
                                        }
                                    }
                                }

                                catch (Exception ex)
                                {
                                    await Client_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Potential ESI Failiure for {u["eveName"]} skipping, Reason: {ex.Message}", ex));
                                    continue;
                                }

                                //Check if roles when should not have any
                                if (!corps.ContainsKey(corporationid.ToString()) && !alliance.ContainsKey(allianceID))
                                {
                                    if (discordUser != null)
                                    {
                                        var exemptRoles = Program.Settings.GetSection("auth").GetSection("exempt").GetChildren().ToList();

                                        rolesToTake.AddRange(discordUser.Roles);
                                        var exemptCheckRoles = new List<SocketRole>(rolesToTake);
                                        foreach (var r in exemptCheckRoles)
                                        {
                                            var name = r.Name;
                                            if (exemptRoles.FindAll(x => x.Key == name).Count > 0)
                                            {
                                                rolesToTake.Remove(rolesToTake.FirstOrDefault(x => x.Name == r.Name));
                                            }
                                        }
                                        rolesToTake.Remove(rolesToTake.FirstOrDefault(x => x.Name == "@everyone"));
                                        if (rolesToTake.Count > 0)
                                        {
                                            var channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == logchan);
                                            await channel.SendMessageAsync($"Taking Roles from {characterDetails["name"]}");
                                            await discordUser.RemoveRolesAsync(rolesToTake);
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                        lastAuthCheck = DateTime.Now;
                    }
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    //await Logger.logError(ex.Message);
                    await Client_Log(new LogMessage(LogSeverity.Error, "authCheck", ex.Message, ex));
                }
            }
            await Task.CompletedTask;
        }
        #endregion

        //Complete
        #region killFeed
        private static async Task KillFeed(CommandContext Context)
        {
            try
            {
                lastFeedCheck = DateTime.Now;
                Dictionary<string, IEnumerable<IConfigurationSection>> feedGroups = new Dictionary<string, IEnumerable<IConfigurationSection>>();

                UInt64 guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                UInt64 logchan = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);
                var discordGuild = Program.Client.Guilds.FirstOrDefault(X => X.Id == guildID);
                var redisQID = Program.Settings.GetSection("killFeed")["reDisqID"].ToString();
                ITextChannel channel = null;
                using (HttpClient webclient = new HttpClient())
                using (HttpResponseMessage redisqResponse = await webclient.GetAsync(String.IsNullOrEmpty(redisQID) ? $"https://redisq.zkillboard.com/listen.php" : $"https://redisq.zkillboard.com/listen.php?queueID={redisQID}"))
                using (HttpContent _redisqResponseContent = redisqResponse.Content)
                {
                    if (redisqResponse.IsSuccessStatusCode)
                    {
                        var result = await _redisqResponseContent.ReadAsStringAsync();
                        var json = JObject.Parse(result);
                        var killmail = json["package"];
                        if (!killmail.IsNullOrEmpty())
                        {
                            if (killmail.IsNullOrEmpty())
                            {
                                await Client_Log(new LogMessage(LogSeverity.Debug, "killFeed", "Killmail malformed, Probably nothing to post."));
                                return;
                            }

                            var bigKillGlobal = Convert.ToInt64(Program.Settings.GetSection("killFeed")["bigKill"]);
                            var bigKillGlobalChan = Convert.ToUInt64(Program.Settings.GetSection("killFeed")["bigKillChannel"]);
                            var iD = killmail["killmail"]["killID_str"];
                            var killTime = killmail["killmail"]["killTime"];
                            var ship = killmail["killmail"]["victim"]["shipType"]["name"];
                            var value = (double)killmail["zkb"]["totalValue"];
                            var victimCharacter = killmail["killmail"]["victim"]["character"] ?? null;
                            var victimCorp = killmail["killmail"]["victim"]["corporation"];
                            var victimAlliance = killmail["killmail"]["victim"]["alliance"] ?? null;
                            var attackers = killmail["killmail"]["attackers"] ?? null;
                            var sysName = (string)killmail["killmail"]["solarSystem"]["name"];
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

                                if (radius > 0)
                                {
                                    using (HttpClient webClient = new HttpClient())
                                    using (HttpResponseMessage _radiusSystems = await webclient.GetAsync(
                                        $"https://trades.eve-price.com/system-distance/{radiusSystem}/{radius}"))
                                    using (HttpContent _radiusSystemsContent = _radiusSystems.Content)
                                    {
                                        var systemID = (int)killmail["killmail"]["solarSystem"]["id"];
                                        var systems = JObject.Parse(await _radiusSystemsContent.ReadAsStringAsync());
                                        var results = systems["results"];
                                        var gg = results.FirstOrDefault(x => (int)x["solarSystemID"] == systemID);
                                        if (gg != null && gg.Count() > 0)
                                        {
                                            jumpsAway = (int)gg["distance"];
                                            radiusKill = true;
                                        }
                                    }
                                }
                                if (bigKillGlobal != 0 && value >= bigKillGlobal)
                                {
                                    channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == bigKillGlobalChan);
                                    globalBigKill = true;
                                    post = true;
                                }
                                else if (allianceID == 0 && corpID == 0)
                                {
                                    if (bigKillValue != 0 && value >= bigKillValue && !globalBigKill)
                                    {
                                        channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == bigKillChannel);
                                        bigKill = true;
                                        post = true;
                                    }
                                    else
                                    {
                                        channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == channelGroup);
                                        var totalValue = value;
                                        if (minimumValue == 0 || minimumValue <= totalValue)
                                            post = true;
                                    }
                                }
                                else if (!globalBigKill)
                                {
                                    channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == channelGroup);
                                    if (victimAlliance != null)
                                    {
                                        if ((Int32)victimAlliance["id"] == allianceID && losses == true ||
                                            (Int32)victimCorp["id"] == corpID && losses == true)
                                        {
                                            if (bigKillValue != 0 && value >= bigKillValue)
                                            {
                                                channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == bigKillChannel);
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
                                    else if ((Int32)victimCorp["id"] == corpID && losses == true)
                                    {
                                        if (bigKillValue != 0 && value >= bigKillValue)
                                        {
                                            channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == bigKillChannel);
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
                                        if (attacker["alliance"] != null)
                                        {
                                            if ((Int32)attacker["alliance"]["id"] == allianceID ||
                                                (Int32)attacker["corporation"]["id"] == corpID)
                                            {
                                                if (bigKillValue != 0 && value >= bigKillValue)
                                                {
                                                    channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == bigKillChannel);
                                                    bigKill = true;
                                                    post = true;
                                                }
                                                else
                                                {
                                                    if (minimumValue == 0 || minimumValue <= value)
                                                        post = true;
                                                }
                                            }
                                            else if ((Int32)attacker["corporation"]["id"] == corpID)
                                            {
                                                if (bigKillValue != 0 && value >= bigKillValue)
                                                {
                                                    channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == bigKillChannel);
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
                            }

                            if (post || bigKill || globalBigKill || radiusKill)
                            {
                                if (victimCharacter == null)// Kill is probably a structure.
                                {
                                    if (victimAlliance == null)
                                    {
                                        if (radiusKill)
                                        {
                                            var _radiusChannel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == radiusChannel);
                                            var radiusMessage = "";
                                            radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                            radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                                $" [{victimCorp["name"]}]** killed in **{sysName}** {Environment.NewLine} https://zkillboard.com/kill/{iD}/";
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
                                                $" [{victimCorp["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
                                                $"https://zkillboard.com/kill/{iD}/";
                                            await channel.SendMessageAsync(message);
                                        }
                                    }
                                    else
                                    {
                                        if (radiusKill)
                                        {
                                            var _radiusChannel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == radiusChannel);
                                            var radiusMessage = "";
                                            radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                            radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                            $" {victimCorp["name"]} | [{victimAlliance["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
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
                                                $" {victimCorp["name"]} | [{victimAlliance["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
                                                $"https://zkillboard.com/kill/{iD}/";
                                            await channel.SendMessageAsync(message);
                                        }
                                    }
                                }
                                else if (!victimAlliance.IsNullOrEmpty())
                                {
                                    if (radiusKill)
                                    {
                                        var _radiusChannel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == radiusChannel);
                                        var radiusMessage = "";
                                        radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                        radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                        $"** ISK flown by **{victimCharacter["name"]} |**  **[{victimCorp["name"]}] | <{victimAlliance["name"]}>** killed in **{sysName}** {Environment.NewLine} " +
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
                                            $"** ISK flown by **{victimCharacter["name"]} |**  **[{victimCorp["name"]}] | <{victimAlliance["name"]}>** killed in **{sysName}** {Environment.NewLine} " +
                                            $"https://zkillboard.com/kill/{iD}/";
                                        await channel.SendMessageAsync(message);
                                    }
                                }
                                else
                                {
                                    if (radiusKill)
                                    {
                                        var _radiusChannel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == radiusChannel);
                                        var radiusMessage = "";
                                        radiusMessage = $"Killed {jumpsAway} jumps from {Program.Settings.GetSection("killFeed")["radiusSystem"]}{Environment.NewLine}";
                                        radiusMessage += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{string.Format("{0:n0}", value)}" +
                                        $"** ISK flown by **{victimCharacter["name"]} |** **[{victimCorp["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
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
                                            $"** ISK flown by **{victimCharacter["name"]} |** **[{victimCorp["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
                                            $"https://zkillboard.com/kill/{iD}/";
                                        await channel.SendMessageAsync(message);
                                    }
                                }
                                await Client_Log(new LogMessage(LogSeverity.Info, "killFeed", $"POSTING Kill/Loss ID:{killmail["killmail"]["killID"]} Value:{string.Format("{0:n0}", value)}"));
                            }
                        }
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
                {201,   "StructureCourierContractChanged ?"},
                {1012,  "OperationFinished ?"},
                {1030,  "Game time received (GameTimeReceived)"},
                {1031,  "Game time sent (GameTimeSent)"}
            };
            #endregion
            try
            {
                if (DateTime.Now > nextNotificationCheck)
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", "Running Notification Check"));
                    lastNotification = Convert.ToInt32(await SQLiteDataQuery("cacheData","data","lastNotificationID"));
                    var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                    var test = Program.Settings.GetSection("notifications")["characterId"];
                    var channelId = Convert.ToUInt64(Program.Settings.GetSection("notifications")["channelId"]);
                    var chan = (ITextChannel)Program.Client.GetGuild(guildID).GetChannel(channelId);
                    var keyID = "";//= Program.Settings.GetSection("notifications")["keyID"];
                    var vCode= "";//= Program.Settings.GetSection("notifications")["vCode"];
                    var characterID = Program.Settings.GetSection("notifications")["characterID"];
                    var keys = Program.Settings.GetSection("notifications").GetSection("keys").GetChildren();
                    var keyCount = keys.Count();
                    var nextKey = await SQLiteDataQuery("notifications", "data", "nextKey");
                    var index = 0;

                    foreach (var key in keys)
                    {
                        if (nextKey == null || String.IsNullOrWhiteSpace(nextKey) || nextKey == key.Key)
                        {
                            keyID = key["keyID"];
                            vCode = key["vCode"];

                            await Program.EveLib.SetApiKey(keyID, vCode, characterID);
                            var notifications = await Program.EveLib.GetNotifications();
                            var notificationsSort = notifications.OrderBy(x => x.Key);

                            if (notifications.Count > 0)
                            {
                                var notiIDs = new List<int>();

                                foreach (var l in notifications)
                                {
                                    notiIDs.Add((int)l.Key);
                                }

                                var notificationsText = await Program.EveLib.GetNotificationText(notiIDs);

                                foreach (var notification in notificationsSort)
                                {
                                    if ((int)notification.Value["notificationID"] > lastNotification)
                                    {
                                        var notificationText = notificationsText.FirstOrDefault(x => x.Key == notification.Key).Value;
                                        var notificationType = (int)notification.Value["typeID"];

                                        if (notificationType == 121)
                                        {
                                            var aggressorID = Convert.ToInt64(notificationText["entityID"].AllNodes.ToList()[0].ToString());
                                            var defenderID = Convert.ToInt64(notificationText["defenderID"].AllNodes.ToList()[0].ToString());

                                            var stuff = await Program.EveLib.IDtoName(new List<Int64> { aggressorID, defenderID });
                                            var aggressorName = stuff.FirstOrDefault(x => x.Key == aggressorID).Value;
                                            var defenderName = stuff.FirstOrDefault(x => x.Key == defenderID).Value;
                                            await chan.SendMessageAsync($"War declared by **{aggressorName}** against **{defenderName}**. Fighting begins in roughly 24 hours.");
                                        }
                                        else if (notificationType == 100)
                                        {
                                            var allyID = Convert.ToInt64(notificationText["allyID"].AllNodes.ToList()[0].ToString());
                                            var defenderID = Convert.ToInt64(notificationText["defenderID"].AllNodes.ToList()[0].ToString());

                                            var stuff = await Program.EveLib.IDtoName(new List<Int64> { allyID, defenderID });
                                            var allyName = stuff.FirstOrDefault(x => x.Key == allyID).Value;
                                            var defenderName = stuff.FirstOrDefault(x => x.Key == defenderID).Value;
                                            var startTime = DateTime.FromFileTimeUtc(Convert.ToInt64(notificationText["startTime"].AllNodes.ToList()[0].ToString()));
                                            await chan.SendMessageAsync($"**{allyName}** will join the war against **{defenderName}** at {startTime} EVE.");
                                        }
                                        else if (notificationType == 5)
                                        {
                                            var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
                                            var cost = notificationText["cost"].AllNodes.ToList()[0];
                                            var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
                                            var delayHours = notificationText["delayHours"].AllNodes.ToList()[0].ToString();
                                            var hostileState = notificationText["hostileState"].AllNodes.ToList()[0].ToString();
                                            var names = await Program.EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
                                            var againstName = names.FirstOrDefault(x => x.Key == againstID);
                                            var declaredByName = names.First(x => x.Key == declaredByID);

                                            await chan.SendMessageAsync($"War declared by {declaredByName.Value} against {againstName.Value}" +
                                                $" Fighting begins in roughly {delayHours} hours");
                                        }
                                        else if (notificationType == 8)
                                        {
                                            var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
                                            var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
                                            var names = await Program.EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
                                            var againstName = names.FirstOrDefault(x => x.Key == againstID);
                                            var declaredByName = names.First(x => x.Key == declaredByID);

                                            await chan.SendMessageAsync($"CONCORD Invalidates war declared by {declaredByName.Value} against {againstName.Value}");
                                        }
                                        else if (notificationType == 161)
                                        {
                                            var campaignEventType = notificationText["campaignEventType"].AllNodes.ToList()[0];
                                            var constellationID = notificationText["constellationID"].AllNodes.ToList()[0];
                                            var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
                                            var names = await Program.EveLib.IDtoName(new List<Int64> { solarSystemID });
                                            var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);

                                            await chan.SendMessageAsync($"Command nodes decloaking for {solarSystemName.Value}");

                                        }
                                        else if (notificationType == 147)
                                        {
                                            var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
                                            var structureTypeID = Convert.ToInt64(notificationText["structureTypeID"].AllNodes.ToList()[0].ToString());
                                            var names = await Program.EveLib.IDtoName(new List<Int64> { solarSystemID });
                                            var typeNames = await Program.EveLib.IDtoTypeName(new List<Int64> { structureTypeID });
                                            var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);
                                            var structureTypeName = typeNames.FirstOrDefault(x => x.Key == structureTypeID);

                                            await chan.SendMessageAsync($"Entosis Link started in {solarSystemName.Value} on {structureTypeName.Value}");
                                        }
                                        else if (notificationType == 160)
                                        {
                                            var campaignEventType = notificationText["campaignEventType"].AllNodes.ToList()[0];
                                            var solarSystemID = Convert.ToInt64((notificationText["solarSystemID"].AllNodes.ToList()[0].ToString()));
                                            var decloakTime = Convert.ToInt64(notificationText["decloakTime"].AllNodes.ToList()[0].ToString());
                                            var names = await Program.EveLib.IDtoName(new List<Int64> { solarSystemID });
                                            var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);
                                            var decloaktime = DateTime.FromFileTime(decloakTime);

                                            await chan.SendMessageAsync($"Sovereignty structure reinforced in {solarSystemName.Value} nodes will spawn @{decloaktime}");
                                        }
                                        else
                                        {
                                            await Client_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Skipping Notification TypeID: {notificationType} " +
                                                $"Type: {types[notificationType]} {Environment.NewLine} Text: {notificationText}"));
                                        }
                                        lastNotification = (int)notification.Value["notificationID"];
                                        await SQLiteDataUpdate("cacheData", "data", "lastNotificationID", lastNotification.ToString());
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
                            index++;
                        }
                        var interval = 30 / keys.Count();
                        await SQLiteDataUpdate("cacheData", "data", "nextNotificationCheck", DateTime.Now.AddMinutes(interval).ToString());
                        nextNotificationCheck = DateTime.Now.AddMinutes(interval);
                        await Task.CompletedTask;
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
            var NametoId = "https://www.fuzzwork.co.uk/api/typeid.php?typename=";

            using (HttpClient webClient = new HttpClient())
            {
                JObject jObject = new JObject();
                var channel = (ITextChannel)context.Message.Channel;
                if (String.ToLower() == "plex")
                {
                    String = "30 Day Pilot's License Extension (PLEX)";
                }

                var reply = await webClient.GetStringAsync(NametoId + String);
                jObject = JObject.Parse(reply);
                if ((string)jObject["typeName"] == "bad item")
                {
                    await channel.SendMessageAsync($"{context.Message.Author.Mention} Item {String} does not exist please try again");
                    await Task.CompletedTask;
                }
                else
                {
                    try
                    {
                        if (system == "")
                        {
                            var eveCentralReply = await webClient.GetAsync($"http://api.eve-central.com/api/marketstat/json?typeid={jObject["typeID"]}");
                            var eveCentralReplyString = eveCentralReply.Content;
                            var centralreply = JToken.Parse(await eveCentralReply.Content.ReadAsStringAsync());
                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                            await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: **Universe**{Environment.NewLine}" +
                                $"**Buy:**{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["buy"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["buy"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["buy"]["max"]:n2}```" +
                                $"{Environment.NewLine}" +
                                $"**Sell**:{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["sell"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["sell"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["sell"]["max"]:n2}```");
                        }
                        if (system == "jita")
                        {
                            var eveCentralReply = await webClient.GetAsync($"http://api.eve-central.com/api/marketstat/json?typeid={jObject["typeID"]}&usesystem=30000142");
                            var eveCentralReplyString = eveCentralReply.Content;
                            var centralreply = JToken.Parse(await eveCentralReply.Content.ReadAsStringAsync());
                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                            await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: Jita{Environment.NewLine}" +
                                $"**Buy:**{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["buy"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["buy"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["buy"]["max"]:n2}```" +
                                $"{Environment.NewLine}" +
                                $"**Sell**:{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["sell"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["sell"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["sell"]["max"]:n2}```");
                        }
                        if (system == "amarr")
                        {
                            var eveCentralReply = await webClient.GetAsync($"http://api.eve-central.com/api/marketstat/json?typeid={jObject["typeID"]}&usesystem=30002187");
                            var eveCentralReplyString = eveCentralReply.Content;
                            var centralreply = JToken.Parse(await eveCentralReply.Content.ReadAsStringAsync());
                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                            await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: Amarr{Environment.NewLine}" +
                                $"**Buy:**{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["buy"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["buy"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["buy"]["max"]:n2}```" +
                                $"{Environment.NewLine}" +
                                $"**Sell**:{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["sell"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["sell"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["sell"]["max"]:n2}```");
                        }
                        if (system == "rens")
                        {
                            var eveCentralReply = await webClient.GetAsync($"http://api.eve-central.com/api/marketstat/json?typeid={jObject["typeID"]}&usesystem=30002510");
                            var eveCentralReplyString = eveCentralReply.Content;
                            var centralreply = JToken.Parse(await eveCentralReply.Content.ReadAsStringAsync());
                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                            await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: Rens{Environment.NewLine}" +
                                $"**Buy:**{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["buy"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["buy"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["buy"]["max"]:n2}```" +
                                $"{Environment.NewLine}" +
                                $"**Sell**:{Environment.NewLine}" +
                                $"```Low: {centralreply[0]["sell"]["min"]:n2}{Environment.NewLine}" +
                                $"Avg: {centralreply[0]["sell"]["avg"]:n2}{Environment.NewLine}" +
                                $"High: {centralreply[0]["sell"]["max"]:n2}```");
                        }
                        if (system == "dodixe")
                        {
                            var eveCentralReply = await webClient.GetAsync($"http://api.eve-central.com/api/marketstat/json?typeid={jObject["typeID"]}&usesystem=30002659");
                            var eveCentralReplyString = eveCentralReply.Content;
                            var centralreply = JToken.Parse(await eveCentralReply.Content.ReadAsStringAsync());
                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                            await channel.SendMessageAsync($"{context.Message.Author.Mention}, System: Dodixe{Environment.NewLine}" +
                                $"**Buy:**{Environment.NewLine}" +
                                $"      Low: {centralreply[0]["buy"]["min"]:n}{Environment.NewLine}" +
                                $"      Avg: {centralreply[0]["buy"]["avg"]:n}{Environment.NewLine}" +
                                $"      High: {centralreply[0]["buy"]["max"]:n}{Environment.NewLine}" +
                                $"**Sell**:{Environment.NewLine}" +
                                $"      Low: {centralreply[0]["sell"]["min"]:n}{Environment.NewLine}" +
                                $"      Avg: {centralreply[0]["sell"]["avg"]:n}{Environment.NewLine}" +
                                $"      High: {centralreply[0]["sell"]["max"]:n}{Environment.NewLine}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "PC", ex.Message, ex));
                    }
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
                var utcTime = DateTime.UtcNow;
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
                var keyID = Program.Settings.GetSection("notifications").GetSection("chankey")["keyID"];
                var vCode = Program.Settings.GetSection("notifications").GetSection("chankey")["vCode"];
                var characterID = Program.Settings.GetSection("notifications")["characterID"];
                var chanName = Program.Settings.GetSection("config")["MOTDChan"];

                var document = new XmlDocument();

                using (HttpClient webRequest = new HttpClient())
                {
                    var xml = await webRequest.GetStreamAsync($"https://api.eveonline.com/char/ChatChannels.xml.aspx?keyID={keyID}&vCode={vCode}&characterID={characterID}");
                    var xmlReader = XmlReader.Create(xml, new XmlReaderSettings { Async = true });
                    var complete = await xmlReader.ReadAsync();
                    var result = new JObject();
                    if (complete)
                    {
                        document.Load(xmlReader);
                        var tmp = JSON.XmlToJSON(document);
                        result = JObject.Parse(tmp);
                    }

                    var rowlist = result["eveapi"]["result"]["rowset"]["row"].ToList();
                    foreach (var r in rowlist)
                    {
                        var ChName = r["displayName"];
                        string Channel = ChName.ToString();
                        string ChannelName = chanName.ToString();
                        if (Channel == ChannelName)
                        {
                            var comments = r["motd"];
                            comments.Replace("<br>", "\n");
                            var com = comment.ToString();
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

            var result = await Program.Commands.ExecuteAsync(context, argPos, Program.Map);
            if (!result.IsSuccess && result.ErrorReason != "Unknown command.")
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
        #endregion

        //Complete
        #region MysqlQuery
        internal static async Task<IList<IDictionary<string, object>>> MysqlQuery(string connstring, string query)
        {
            using (MySqlConnection conn = new MySqlConnection(connstring))
            {
                MySqlCommand cmd = conn.CreateCommand();
                List<IDictionary<string, object>> list = new List<IDictionary<string, object>>(); ;
                cmd.CommandText = query;
                try
                {
                    conn.ConnectionString = connstring;
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

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
                catch (MySqlException ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "mySQL", query  + " " + ex.Message, ex));
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
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
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
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
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
        #region SQLiteQuery
        internal async static Task SQLiteDataUpdate(string table, string field, string name, string data)
        {
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
            using (SqliteCommand insertSQL = new SqliteCommand($"UPDATE {table} SET {field} = @data WHERE name = @name", con))
            {
                await con.OpenAsync();
                insertSQL.Parameters.Add(new SqliteParameter("@name", name));
                insertSQL.Parameters.Add(new SqliteParameter("@data", data));
                try
                {
                    insertSQL.ExecuteNonQuery();
                    await Task.CompletedTask;
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
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
            using (SqliteCommand insertSQL = new SqliteCommand($"REMOVE FROM {table} WHERE name = @name", con))
            {
                await con.OpenAsync();
                insertSQL.Parameters.Add(new SqliteParameter("@name", name));
                try
                {
                    insertSQL.ExecuteNonQuery();
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                }
            }
        }
        #endregion

    }

    #region JToken null/empty check
    internal static class JsonExtensions
    {
        public static bool IsNullOrEmpty(this JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
                   (token.Type == JTokenType.Null);
        }
    }
    #endregion

}
