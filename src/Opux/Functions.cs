﻿using ByteSizeLib;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ESIClient.Dotcore.Api;
using ESIClient.Dotcore.Client;
using ESIClient.Dotcore.Model;
using EveLibCore;
using JsonClasszAPIKill;
using Matrix.Xmpp.Chatstates;
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
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TentacleSoftware.TeamSpeakQuery;
using TentacleSoftware.TeamSpeakQuery.NotifyResult;
using TentacleSoftware.TeamSpeakQuery.ServerQueryResult;
using YamlDotNet.RepresentationModel;
using static Opux.JsonClasses;

namespace Opux
{
	internal class Functions
	{
		static DateTime _lastAuthCheck = DateTime.Now;
		internal static DateTime _nextNotificationCheck = DateTime.FromFileTime(0);
		static int _lastNotification;
		static bool _avaliable = false;
		static bool _running = false;
		static bool _jabberRunning = false;
		static bool _teamspeakRunning = false;
		static string _motdtopic;
		static int lastPosted = 0;
		static DateTime _lastTopicCheck = DateTime.Now;
		public static DateTime TsLastRan { get; private set; }
		public static bool ZkillInit { get; private set; }
		public static List<int> Lastkill = new List<int>();

		//Timer is setup here
		#region Timer stuff
		public async static void RunTick(object stateInfo)
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
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
			}
		}

		private async static Task Async_Tick(object args)
		{
			try
			{
				if (Program.debug)
					Console.WriteLine($"Checking authWeb Enabled");
				if (Convert.ToBoolean(Program.Settings.GetSection("config")["authWeb"]))
				{
					if (Program.debug)
						Console.WriteLine($"Checking authWeb");
					await AuthWeb();
				}
				if (Program.debug)
					Console.WriteLine($"Checking authCheck enabled");
				if (Convert.ToBoolean(Program.Settings.GetSection("config")["authCheck"]))
				{
					if (Program.debug)
						Console.WriteLine($"Checking authCheck");
					await AuthCheck(null);
				}
				if (Program.debug)
					Console.WriteLine($"Checking killFeed enabled");
				if (Convert.ToBoolean(Program.Settings.GetSection("config")["killFeed"]))
				{
					if (Program.debug)
						Console.WriteLine($"Checking killFeed");
					//await KillFeed(null);
					ZKillMain();
				}
				if (Program.debug)
					Console.WriteLine($"Checking notificationFeed Enabled");
				if (Convert.ToBoolean(Program.Settings.GetSection("config")["notificationFeed"]))
				{
					if (Program.debug)
						Console.WriteLine($"Checking notificationFeed");
					await NotificationFeed(null);
				}
				if (Program.debug)
					Console.WriteLine($"Checking fleetUp Enabled");
				if (Program.debug)
					Console.WriteLine($"Checking update topic Enabled");
				if (Convert.ToBoolean(Program.Settings.GetSection("motd")["updatetopic"]))
				{
					if (Program.debug)
						Console.WriteLine($"Checking update topic");
					await TopicMOTD(null);
				}
				if (Program.debug)
					Console.WriteLine($"Checking jabber Enabled");
				if (Convert.ToBoolean(Program.Settings.GetSection("config")["jabber"]))
				{
					if (Program.debug)
						Console.WriteLine($"Checking jabber");
					await Jabber();
				}
				if (Convert.ToBoolean(Program.Settings.GetSection("config")["teamspeak"]))
				{
					if (Program.debug)
						Console.WriteLine($"Checking teamspeak");
					await TeamspeakCheck();
				}

				_running = false;
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
				_running = false;
			}
		}
		#endregion

		//Events are attached here
		#region EVENTS
		internal async static Task Event_UserJoined(SocketGuildUser arg)
		{
			if (Convert.ToBoolean(Program.Settings.GetSection("config")["welcome"]))
			{
				var channel = arg.Guild.DefaultChannel;
				var authurl = Program.Settings.GetSection("auth")["authurl"];
				if (!string.IsNullOrWhiteSpace(authurl))
				{
					await channel.SendMessageAsync($"Welcome {arg.Mention} to the server, To gain access please auth at {authurl} ");
				}
				else
				{
					await channel.SendMessageAsync($"Welcome {arg.Mention} to the server");
				}
			}
		}

		internal static async Task Ready()
		{
			_avaliable = true;

			await Program.Client.GetGuild(Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]))
				.CurrentUser.ModifyAsync(x => x.Nickname = Program.Settings.GetSection("config")["name"]);
			await Program.Client.SetGameAsync(Program.Settings.GetSection("config")["game"]);

			await Task.CompletedTask;
		}
		#endregion

		//Auth
		#region SendAuthMessage
		internal static async Task SendAuthMessage(ICommandContext context)
		{
			var channel = context.Message.Channel;
			var authurl = Program.Settings.GetSection("auth")["authurl"];
			var Ids = new List<ulong>(context.Message.MentionedUserIds);
			var mentioneduser = await context.Guild.GetUserAsync(Ids[0]);
			if (!String.IsNullOrWhiteSpace(authurl))
			{
				await channel.SendMessageAsync($"{mentioneduser.Mention}, To gain access please auth at {authurl} ");
			}
		}
		#endregion

		#region AuthWeb
		internal static System.Net.Http.HttpListener listener;

		internal async static Task AuthWeb()
		{
			var callbackurl = Program.Settings.GetSection("auth")["callbackurl"];
			var client_id = Program.Settings.GetSection("auth")["client_id"];
			var secret = Program.Settings.GetSection("auth")["secret"];
			var url = Program.Settings.GetSection("auth")["url"];
			var port = Convert.ToInt32(Program.Settings.GetSection("auth")["port"]);
			var ESIFailure = false;

			if (listener == null || !listener.IsListening)
			{

				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "AuthWeb", "Starting AuthWeb Server"));
				listener = new System.Net.Http.HttpListener(IPAddress.Any, port);

				listener.Request += async (sender, context) =>
				{

					var allianceID = "";
					var corpID = "";

					var request = context.Request;
					var response = context.Response;

					if (request.HttpMethod == HttpMethod.Get.ToString())
					{
						if (request.Url.LocalPath == "/" || request.Url.LocalPath == $"{port}/")
						{

							response.Headers.Add("Content-Type", "text/html");

							await response.WriteContentAsync("<!doctype html>" +
								"<html>" +
								"<head>" +
								"    <title>Discord Authenticator</title>" +
								"    <meta name=\"viewport\" content=\"width=device-width\">" +
								"    <link rel=\"stylesheet\" href=\"https://code.divshot.com/bootstrap-theme-cirrus/dist/css/bootstrap.css\">" +
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
								"        <p><a href=\"https://login.eveonline.com/oauth/authorize?response_type=code&amp;redirect_uri=" + callbackurl + "&amp;client_id=" + client_id + "\"><img src=\"https://web.ccpgamescdn.com/eveonlineassets/developers/eve-sso-login-black-large.png\"/></a></p>" +
								"    </div>" +
								"</div>" +
								"<!-- /container -->" +
								"</body>" +
								"</html>");
						}
						else if (request.Url.LocalPath == "/callback.php" || request.Url.LocalPath == $"{port}/callback.php")
						{
							try
							{
								var _AuthWebHttpClient = new HttpClient();
								var assembly = Assembly.GetEntryAssembly();
								var temp = assembly.GetManifestResourceNames();
								var resource = assembly.GetManifestResourceStream("Opux.Discord-01.png");
								var buffer = new byte[resource.Length];
								resource.ReadExactly(buffer, 0, Convert.ToInt32(resource.Length));
								var image = Convert.ToBase64String(buffer);
								string accessToken = "";
								string responseString;
								string verifyString;
								var uid = GetUniqID();
								var code = "";
								var add = false;

								if (!string.IsNullOrWhiteSpace(request.Url.Query))
								{
									_AuthWebHttpClient.DefaultRequestHeaders.Clear();
									_AuthWebHttpClient.DefaultRequestHeaders.Add("User-Agent", "OpuxBot");

									code = request.Url.Query.TrimStart('?').Split('=')[1];

									var values = new Dictionary<string, string> { { "grant_type", "authorization_code" }, { "code", $"{code}" } };

									_AuthWebHttpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(client_id + ":" + secret))}");
									var content = new FormUrlEncodedContent(values);

									var tokenresponse = await _AuthWebHttpClient.PostAsync("https://login.eveonline.com/oauth/token", content);
									responseString = await tokenresponse.Content.ReadAsStringAsync();
									accessToken = (string)JObject.Parse(responseString)["access_token"];
									_AuthWebHttpClient.DefaultRequestHeaders.Clear();

									_AuthWebHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
									tokenresponse = await _AuthWebHttpClient.GetAsync("https://login.eveonline.com/oauth/verify");
									verifyString = await tokenresponse.Content.ReadAsStringAsync();
									_AuthWebHttpClient.DefaultRequestHeaders.Clear();

									var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
									var corps = new Dictionary<string, string>();
									var alliance = new Dictionary<string, string>();

									foreach (var config in authgroups)
									{
										var configChildren = config.GetChildren();

										corpID = configChildren.FirstOrDefault(x => x.Key == "corpID").Value ?? "";
										allianceID = configChildren.FirstOrDefault(x => x.Key == "allianceID").Value ?? "";
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
									//JObject characterDetails;
									JObject corporationDetails;
									JObject allianceDetails;
									CharacterApi characterApi = new();
									var charList = new List<int?>
									{
										CharacterID.Value<int>()
									};

									//var _characterDetails = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{CharacterID}");
									var _characterDetails = await characterApi.PostCharactersAffiliationAsyncWithHttpInfo(charList);

									string charData = null;
									if (_characterDetails.StatusCode != 200)
									{
										charData = _characterDetails.Data.ToString();
										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "AuthWeb", $"Character Failure ID:{CharacterID} Error: {_characterDetails.StatusCode} : {charData}"));

										ESIFailure = true;
									}

									var _characterDetailsContent = _characterDetails.Data;

									//characterDetails = JObject.Parse(await _characterDetailsContent.ReadAsStringAsync());
									//characterDetails.TryGetValue("corporation_id", out JToken corporationid);

									var corporationid = _characterDetailsContent[0].CorporationId;

									var _corporationDetails = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/corporations/{corporationid}");
									if (!_corporationDetails.IsSuccessStatusCode)
									{
										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "AuthWeb", $"Corp Failure ID:{CharacterID} Error: {_characterDetails.StatusCode} : {charData}"));

										ESIFailure = true;
									}

									var _corporationDetailsContent = _corporationDetails.Content;

									corporationDetails = JObject.Parse(await _corporationDetailsContent.ReadAsStringAsync());
									corporationDetails.TryGetValue("alliance_id", out JToken allianceid);
									string i = (allianceid == null ? "0" : allianceid.ToString());
									string c = (corporationid == null ? "0" : corporationid.ToString());
									allianceID = i;
									corpID = c;
									if (allianceID != "0")
									{
										var _allianceDetails = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/alliances/{allianceid}");
										if (!_allianceDetails.IsSuccessStatusCode)
										{
											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "AuthWeb", $"ESI Failure: cID:{CharacterID}"));
											foreach (var h in Program._httpClient.DefaultRequestHeaders)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "AuthWeb", $"key:{h.Key} value:{h.Value}"));
											}
											ESIFailure = true;
										}
										var _allianceDetailsContent = _allianceDetails.Content;

										allianceDetails = JObject.Parse(await _allianceDetailsContent.ReadAsStringAsync());
									}

									if (corps.ContainsKey(corpID))
									{
										add = true;
									}
									if (alliance.ContainsKey(allianceID))
									{
										add = true;
									}

									if (!ESIFailure && add && (string)JObject.Parse(responseString)["error"] != "invalid_request" && (string)JObject.Parse(verifyString)["error"] != "invalid_token")
									{
										var characterID = CharacterID;
										//characterDetails.TryGetValue("corporation_id", out corporationid);
										corporationDetails.TryGetValue("alliance_id", out allianceid);
										var authString = uid;
										var active = "1";
										var dateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

										var query = "INSERT INTO pendingUsers(characterID, corporationID, allianceID, authString, groups, active, dateCreated) " +
										$"VALUES (\"{characterID}\", \"{corporationid}\", \"{allianceid}\", \"{authString}\", \"[]\", \"{active}\", \"{dateCreated}\") ON DUPLICATE KEY UPDATE " +
										$"corporationID = \"{corporationid}\", allianceID = \"{allianceid}\", authString = \"{authString}\", groups = \"[]\", active = \"{active}\", dateCreated = \"{dateCreated}\"";
										var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);

										response.Headers.Add("Content-Type", "text/html");

										await response.WriteContentAsync("<!doctype html>" +
											"<html>" +
											"<head>" +
											"    <title>Discord Authenticator</title>" +
											"    <meta name=\"viewport\" content=\"width=device-width\">" +
											"    <link rel=\"stylesheet\" href=\"https://code.divshot.com/bootstrap-theme-cirrus/dist/css/bootstrap.css\">" +
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
											"        <p><b>" + Program.Settings.GetSection("config")["commandprefix"] + "auth " + uid + " </b></p>" +
											"    </div>" +
											"</div>" +
											"<!-- /container -->" +
											"</body>" +
											"</html>");
									}
									else if (!ESIFailure)
									{
										var message = "ERROR";
										if (!add)
										{
											message = "You are not Corp/Alliance or Blue";
										}

										response.Headers.Add("Content-Type", "text/html");

										await response.WriteContentAsync("<!doctype html>" +
										   "<html>" +
										   "<head>" +
										   "    <title>Discord Authenticator</title>" +
										   "    <meta name=\"viewport\" content=\"width=device-width\">" +
										   "    <link rel=\"stylesheet\" href=\"https://code.divshot.com/bootstrap-theme-cirrus/dist/css/bootstrap.css\">" +
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
									else if (ESIFailure)
									{
										var message = "ESI Failure, Please try again later";

										response.Headers.Add("Content-Type", "text/html");

										await response.WriteContentAsync("<!doctype html>" +
										   "<html>" +
										   "<head>" +
										   "    <title>Discord Authenticator</title>" +
										   "    <meta name=\"viewport\" content=\"width=device-width\">" +
										   "    <link rel=\"stylesheet\" href=\"https://code.divshot.com/bootstrap-theme-cirrus/dist/css/bootstrap.css\">" +
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
							catch (Exception ex)
							{
								await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "AuthWeb", $"Error: {ex.Message}", ex));
							}
						}
						else
						{
							response.MethodNotAllowed();
						}
						response.Close();
					}
				};
				listener.Start();
			}
		}

		internal static async Task Dupes(ICommandContext context, SocketUser user)
		{
			var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
			var logchan = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);
			var discordUsers = Program.Client.GetGuild(guildID).Users;
			//var discordGuild = Program.Client.GetGuild(guildID);
			var logChannel = Program.Client.GetGuild(guildID).GetTextChannel(logchan);

			if (user == null)
			{
				foreach (var u in discordUsers)
				{
					int count = 0;
					var query = $"SELECT * FROM authUsers WHERE discordID = {u.Id} ORDER BY addedOn DESC";
					var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
					foreach (var r in responce)
					{
						if (count != 0)
						{
							var query2 = $"DELETE FROM authUsers WHERE id = {r["id"]}";
							var responce2 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query2);
							var query3 = $"SELECT id FROM authUsers WHERE id = {r["id"]}";
							var responce3 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query3);
							if (responce3.Count == 0)
							{
								await logChannel.SendMessageAsync($"Deleting Old Duplicate discordID for {r["eveName"]}");
							}
						}
						count++;
					}
				}
			}
			else
			{
				int count = 0;
				var query = $"SELECT * FROM authUsers WHERE discordID = {user.Id} ORDER BY addedOn DESC";
				var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
				foreach (var r in responce)
				{
					if (count != 0)
					{
						var query2 = $"DELETE FROM authUsers WHERE id = {r["id"]}";
						var responce2 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query2);
						var query3 = $"SELECT id FROM authUsers WHERE id = {r["id"]}";
						var responce3 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query3);
						if (responce3.Count == 0)
						{
							await logChannel.SendMessageAsync($"Deleting Old Duplicate discordID for {r["eveName"]}");
						}
					}
					count++;
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
			try
			{
				var ESIFailed = false;
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

					CharacterApi characterApi = new();
					var charList = new List<int?>
					{
						Convert.ToInt32(characterID),
					};

					var _characterDetails = await characterApi.PostCharactersAffiliationAsyncWithHttpInfo(charList);


					var responceMessage = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{characterID}/?datasource=tranquility");
					var characterData = JsonConvert.DeserializeObject<CharacterData>(await responceMessage.Content.ReadAsStringAsync());
					var _characterData = _characterDetails.Data;



					var corporationid = _characterData[0].CorporationId;


					if (_characterDetails.StatusCode != 200)
					{
						ESIFailed = true;
					}

					responceMessage = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/corporations/{corporationid}/?datasource=tranquility");

					var corporationData = JsonConvert.DeserializeObject<CorporationData>(await responceMessage.Content.ReadAsStringAsync());
					if (!responceMessage.IsSuccessStatusCode)
					{
						ESIFailed = true;
					}

					var allianceID = _characterData[0].AllianceId.ToString();
					var corpID = _characterData[0].CorporationId.ToString();

					var enable = false;

					if (corps.ContainsKey(corpID))
					{
						enable = true;
					}
					if (allianceID != null && alliance.ContainsKey(allianceID))
					{
						enable = true;
					}

					if (enable && !ESIFailed)
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
									await channel.SendMessageAsync($"Granting Roles to {characterData.name}");
									await discordUser.AddRolesAsync(rolesToAdd);
								}
							}
							var query2 = $"UPDATE pendingUsers SET active=\"0\" WHERE authString=\"{remainder}\"";
							var responce2 = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query2);

							await context.Channel.SendMessageAsync($"{context.Message.Author.Mention},:white_check_mark: **Success**: " +
								$"{characterData.name} has been successfully authed.");

							var eveName = characterData.name;
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
									Nickname = $"[{corporationData.ticker}] ";
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

								await Dupes(null, discordUser);
							}
						}

						catch (Exception ex)
						{
							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Failed adding Roles to User {characterData.name}, Reason: {ex.Message}", ex));
						}
					}
					else
					{
						await context.Channel.SendMessageAsync($"ESI Failure please try again later");
						await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "authUser", "ESI Failure"));
					}
				}
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "AuthUser", $"Error: {ex.Message}", ex));
			}
		}
		#endregion

		//Needs Corp and Standings added
		#region AuthCheck
		internal async static Task AuthCheck(ICommandContext Context)
		{
			//Check inactive users are correct
			if (DateTime.Now > _lastAuthCheck.AddMilliseconds(Convert.ToInt32(Program.Settings.GetSection("auth")["authInterval"]) * 1000 * 60) || Context != null)
			{
				_lastAuthCheck = DateTime.Now;

				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Running Auth Check"));

				var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
				var exemptRoles = Program.Settings.GetSection("auth").GetSection("exempt").GetChildren().ToArray();
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
					try
					{
						await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Running Auth Check on {u.Username}"));
						if (u.Id == Program.Client.CurrentUser.Id)
							continue;

						string query = $"SELECT * FROM authUsers WHERE discordID={u.Id}";
						var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);

						if (responce.Count > 0)
						{

							var characterID = responce.OrderByDescending(x => x["id"]).FirstOrDefault()["characterID"];

							CharacterApi characterApi = new();
							var charList = new List<int?>
							{
								Convert.ToInt32(characterID)
							};

							//var _characterDetails = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{CharacterID}");
							var _characterDetails = await characterApi.PostCharactersAffiliationAsyncWithHttpInfo(charList);

							var responceMessage = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{characterID}/?datasource=tranquility");
							var characterData = JsonConvert.DeserializeObject<CharacterData>(await responceMessage.Content.ReadAsStringAsync());

							var _characterData = _characterDetails.Data;

							if (_characterDetails.StatusCode != 200 || _characterData == null)
							{
								await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Potential characterData {_characterDetails.StatusCode} ESI Failure for {u.Nickname}"));
								continue;
							}

							responceMessage = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/corporations/{_characterData[0].CorporationId}/?datasource=tranquility");
							var corporationData = JsonConvert.DeserializeObject<CorporationData>(await responceMessage.Content.ReadAsStringAsync());
							if (!responceMessage.IsSuccessStatusCode || corporationData == null)
							{
								await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Potential corpData {responceMessage.StatusCode} ESI Failure for {u.Nickname}"));
								continue;
							}

							var roles = new List<SocketRole>();
							var rolesOrig = new List<SocketRole>(u.Roles);
							var remroles = new List<SocketRole>();
							roles.Add(u.Roles.FirstOrDefault(x => x.Name == "@everyone"));
							foreach (var role in exemptRoles)
							{
								var exemptRole = u.Roles.FirstOrDefault(x => x.Name == role.Value);
								if (exemptRole != null)
									roles.Add(exemptRole);
							}

							var active = false;

							//Check for Corp roles
							if (corps.ContainsKey(_characterData[0].CorporationId.ToString()))
							{
								var cinfo = corps.FirstOrDefault(x => x.Key == _characterData[0].CorporationId.ToString());
								roles.Add(discordGuild.Roles.FirstOrDefault(x => x.Name == cinfo.Value));
								active = true;
							}

							//Check for Alliance roles
							if (_characterData[0].AllianceId != null)
							{
								if (alliance.ContainsKey(_characterData[0].AllianceId.ToString()))
								{
									var ainfo = alliance.FirstOrDefault(x => x.Key == _characterData[0].AllianceId.ToString());
									roles.Add(discordGuild.Roles.FirstOrDefault(x => x.Name == ainfo.Value));
									active = true;
								}
							}

							if (!active)
							{
								string queryUpdate = $"UPDATE authUsers SET active=\"no\" WHERE discordID=\"{u.Id}\"";
								var responceUpdate = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryUpdate);
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
								await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Adjusting roles for {u.Username}"));
								await u.AddRolesAsync(roles);
								await u.RemoveRolesAsync(remroles);
							}

							var eveName = characterData.name;

							var corpTickers = Convert.ToBoolean(Program.Settings.GetSection("auth")["corpTickers"]);
							var nameEnforce = Convert.ToBoolean(Program.Settings.GetSection("auth")["nameEnforce"]);

							if (corpTickers || nameEnforce)
							{
								var Nickname = "";
								if (corpTickers)
								{
									Nickname = $"[{corporationData.ticker}] ";
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
									await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Changed name of {u.Nickname} to {Nickname}"));
								}
							}
						}
						else
						{
							var rroles = new List<SocketRole>();
							var rolesOrig = new List<SocketRole>(u.Roles);
							var rrolesOrig = rolesOrig;
							foreach (var rrole in rolesOrig)
							{
								var exemptRole = exemptRoles.FirstOrDefault(x => x.Value == rrole.Name);
								if (exemptRole == null)
								{
									rroles.Add(rrole);
								}
							}

							rolesOrig.Remove(u.Roles.FirstOrDefault(x => x.Name == "@everyone"));
							rroles.Remove(u.Roles.FirstOrDefault(X => X.Name == "@everyone"));

							bool rchanged = false;

							if (rroles != rolesOrig)
							{
								foreach (var exempt in rroles)
								{
									if (exemptRoles.FirstOrDefault(x => x.Value == exempt.Name) == null)
										rchanged = true;
								}
							}

							if (rchanged)
							{
								try
								{
									var channel = discordGuild.GetTextChannel(logchan);
									await channel.SendMessageAsync($"Resetting roles for {u.Username}");
									await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Resetting roles for {u.Username}"));
									await u.RemoveRolesAsync(rroles);
								}
								catch (Exception ex)
								{
									await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Error removing roles: {ex.Message}", ex));
								}
							}
						}
					}
					catch (Exception ex)
					{
						await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Fatal Error: {ex.Message}", ex));
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

		//Auth Maintenence
		#region authMaint
		internal async static Task AuthMaint(ICommandContext Context)
		{

			var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
			var exemptRoles = Program.Settings.GetSection("auth").GetSection("exempt").GetChildren().ToArray();
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

			string query = $"SELECT * FROM authUsers";
			var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);

			if (responce.Count > 0)
			{
				foreach (var r in responce)
				{
					var user = discordGuild.GetUser(Convert.ToUInt64(r["discordID"]));
					if (user == null)
					{
						string delQuery = $"DELETE FROM authUsers WHERE id = {r["id"]}";
						var delResponce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
					}
					else
					{

					}
				}
			}
		}
		#endregion

		//Complete Update to Embeds
		#region killFeed

		private static string QueueID = Program.Settings.GetSection("killFeed")["reDisqID"];
		private const int TimeToWait = 1;
		private static readonly string Url = $"https://zkillredisq.stream/listen.php?queueID={QueueID}&ttw={TimeToWait}";

		public static async void ZKillMain()
		{
			try
			{


				if (!ZkillInit)
				{
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"zKill", $"Started Listening to zKill Redisq"));

					var cts = new CancellationTokenSource();

					ZkillInit = true;
					await Task.Run(() => PollRedisQAsync(cts.Token));
				}
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, $"Ws_Socket", $"Web Socket Error {ex.Message}"));
			}
		}

		private static async Task PollRedisQAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					var response = await Program._zKillhttpClient.GetStringAsync(Url, cancellationToken);
					var result = JsonConvert.DeserializeObject(response);

					OnMessage(response);
				}
				catch (TaskCanceledException)
				{
					break; // Graceful exit
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
				}

				await Task.Delay(1000, cancellationToken); // optional delay
			}
		}

		private static async void OnMessage(string result)
		{
			try
			{

				var kill = JsonConvert.DeserializeObject<ZKillAPI>(result).package;

                if (kill != null)
                {
                    Debug.WriteLine($"Processing {kill.killID} {kill.killmail.killmail_time}");
                    if (Lastkill.Contains(kill.killID))
					{
						Debug.WriteLine($"Skipping {kill.killID}");
						return;
					}

					if (kill.killID == 127389994 || kill.killID == 127390431 || kill.killmail.attackers.Where(x => x.corporation_id == 98604007).Any())
					{
						string g = "";
					}

					var count = Lastkill.Count();
					if (count >= 50)
					{
						Lastkill.RemoveRange(0, count - 50);
					}

					if (kill != null)
					{
						var iD = kill.killID;
						var killTime = kill.killmail.killmail_time;
						var shipID = kill.killmail.victim.ship_type_id;
						var value = kill.zkb.totalValue;
						var victimCharacterID = kill.killmail.victim.character_id;
						var victimCorpID = kill.killmail.victim.corporation_id;
						var victimAllianceID = kill.killmail.victim.alliance_id;
						var attackers = kill.killmail.attackers;
						var systemId = kill.killmail.solar_system_id;
						var npckill = kill.zkb.npc;

						CharacterApi characterApi = new CharacterApi();
						CorporationApi corporationApi = new CorporationApi();
						AllianceApi allianceApi = new AllianceApi();
						UniverseApi universeApi = new UniverseApi();
						SearchApi searchApi = new SearchApi();
						RoutesApi routeApi = new RoutesApi();

						GetCharactersCharacterIdOk victim;
						GetCorporationsCorporationIdOk victimCorp;
						GetAlliancesAllianceIdOk victimAlliance;

						var loop = 1;
					GetInfo:

						try
						{
							victim = victimCharacterID != 0 ? (await characterApi.GetCharactersCharacterIdAsync(victimCharacterID)) : null;
							victimCorp = victimCorpID != 0 ? (await corporationApi.GetCorporationsCorporationIdAsync(victimCorpID)) : null;
							victimAlliance = victimAllianceID != 0 ? await allianceApi.GetAlliancesAllianceIdAsync(victimAllianceID) : null;
						}
						catch
						{
							if (loop == 5)
							{
								await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, $"killFeed", $"ESI Error Tryed {loop} times and giving up"));
								return;
							}

							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, $"killFeed", $"ESI Issue Trying Again KillId {kill.killmail.killmail_id} " +
								$"(Loop {loop})"));
							loop++;
							goto GetInfo;
						}

						string victimName;
						if (victimCharacterID == 0)
						{
							victimName = universeApi.GetUniverseTypesTypeId(kill.killmail.victim.ship_type_id).Name;
						}
						else
						{
							victimName = victim.Name;
						}

						var system = await universeApi.GetUniverseSystemsSystemIdAsync(systemId);
						var ship = await universeApi.GetUniverseTypesTypeIdAsync(shipID);
						var secstatus = Math.Round((decimal)system.SecurityStatus, 1).ToString("N2");

						Dictionary<string, IEnumerable<IConfigurationSection>> feedGroups = new Dictionary<string, IEnumerable<IConfigurationSection>>();

						ulong guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
						ulong logchan = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);
						var discordGuild = Program.Client.Guilds.FirstOrDefault(X => X.Id == guildID);
						var bigKillGlobal = Convert.ToInt64(Program.Settings.GetSection("killFeed")["bigKill"]);
						var bigKillGlobalChan = Convert.ToUInt64(Program.Settings.GetSection("killFeed")["bigKillChannel"]);
						var losses = Convert.ToBoolean(Program.Settings.GetSection("killFeed")["losses"]);
						var radius = Convert.ToInt16(Program.Settings.GetSection("killFeed")["radius"]);
						var radiusSystem = Program.Settings.GetSection("killFeed")["radiusSystem"];
						var radiusChannel = Convert.ToUInt64(Program.Settings.GetSection("killFeed")["radiusChannel"]);

						var groupenable = Convert.ToBoolean(Program.Settings.GetSection("killFeed")["groupenable"]);
						var groupalertChannel = Convert.ToUInt64(Program.Settings.GetSection("killFeed")["groupalertChannel"]);
						var groups = Program.Settings.GetSection("killFeed").GetSection("groups").Get<List<int>>();

						var postedRadius = false;
						var postedGlobalBigKill = false;

						ulong lastChannel = 0;

						foreach (var i in Program.Settings.GetSection("killFeed").GetSection("groupsConfig").GetChildren().ToList())
						{
							var minimumValue = Convert.ToInt64(i["minimumValue"]);
							var minimumLossValue = Convert.ToInt64(i["minimumLossValue"]);
							var allianceID = Convert.ToInt32(i["allianceID"]);
							var corpID = Convert.ToInt32(i["corpID"]);
							var bigKillValue = Convert.ToInt64(i["bigKill"]);
							var bigKillChannel = Convert.ToUInt64(i["bigKillChannel"]);
							var c = Convert.ToUInt64(i["channel"]);
							int? SystemID = 0;

							if (!string.IsNullOrWhiteSpace(radiusSystem))
							{
								try
								{
									var StartinWH = system.Name[0] == 'J' && int.TryParse(system.Name[1..], out int StartinWHInt) || system.Name == "Thera";
									var EndinWH = radiusSystem[0] == 'J' && int.TryParse(radiusSystem[1..], out int EndinWHInt) || radiusSystem == "Thera";
									var test3 = !string.IsNullOrWhiteSpace(radiusSystem) && radiusChannel > 0;

									var SystemName = !string.IsNullOrWhiteSpace(radiusSystem) ? await universeApi.PostUniverseIdsAsync(new List<string> { radiusSystem }) : null;

									SystemID = SystemName.Systems.FirstOrDefault().Id;
									var systemID = kill.killmail.solar_system_id;

									if (!StartinWH && !EndinWH && test3)
									{
										var data = await routeApi.GetRouteOriginDestinationAsync(systemId, SystemID);


										var gg = data.Count() - 1;
										if (gg <= radius && !postedRadius)
										{
											postedRadius = true;
											var jumpsText = data.Count() > 1 ? $"{gg} from {radiusSystem}" : $"in {system.Name} ({secstatus})";
											var builder = new EmbedBuilder()
												.WithColor(new Color(0x989898))
												.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
												.WithAuthor(author =>
												{
													author
														.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
												})
												.WithDescription($"[Radius Kill Reported: {ship.Name} destroyed {jumpsText}](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
												.AddField("Victim", victimName, true)
												.AddField("System", $"{system.Name} ({secstatus})", true)
												.AddField("Corporation", victimCorp.Name)
												.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
												.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
												.AddField("Involved Count", attackers.Count(), true);
											var embed = builder.Build();

											var _radiusChannel = discordGuild.GetTextChannel(radiusChannel);
											await _radiusChannel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

											var stringVal = string.Format("{0:n0} ISK", value);

											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting  Radius Kill: {kill.killmail.killmail_id}  Value: {stringVal}"));

											if (groupenable)
											{
												foreach (var g in groups)
												{
													var posted = new List<int>();
													var TypeName = universeApi.GetUniverseGroupsGroupId(g);
													var validships = attackers;
													foreach (var a in attackers)
													{
														if (!posted.Contains(a.ship_type_id))
														{
															if (TypeName.Types.Contains(a.ship_type_id))
															{
																if (validships.Count() > 0)
																{
																	var builder_group = new EmbedBuilder()
																		.WithColor(new Color(0x989898))
																		.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
																		.WithAuthor(author =>
																		{
																			author
																				.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
																		})
																		.WithDescription($"[{TypeName.Name} Reported Killing a {ship.Name}, {jumpsText}](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
																		.AddField("Victim", victimName, true)
																		.AddField("System", $"{system.Name} ({secstatus})", true)
																		.AddField("Corporation", victimCorp.Name)
																		.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
																		.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
																		.AddField("Involved Count", attackers.Count(), true);
																	var embed_group = builder_group.Build();

																	var _groupalertChannel = discordGuild.GetTextChannel(groupalertChannel);
																	await _groupalertChannel.SendMessageAsync($"", false, embed_group).ConfigureAwait(false);

																	posted.Add(g);

																	await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting  Radius Kill: {kill.killID}  Value: {stringVal}"));
																}
															}
														}
													}
												}
											}
										}
									}
									if (StartinWH && EndinWH && test3 && radius == 0)
									{
										if (system.Name == radiusSystem && !postedRadius)
										{
											postedRadius = true;
											var builder = new EmbedBuilder()
												.WithColor(new Color(0x989898))
												.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
												.WithAuthor(author =>
												{
													author
														.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
												})
												.WithDescription($"[Radius Kill Reported: {ship.Name} destroyed in {system.Name}](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
												.AddField("Victim", victimName, true)
												.AddField("System", $"{system.Name} ({secstatus})", true)
												.AddField("Corporation", victimCorp.Name, true)
												.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
												.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
												.AddField("Involved Count", attackers.Count(), true);
											var embed = builder.Build();

											var _radiusChannel = discordGuild.GetTextChannel(radiusChannel);
											await _radiusChannel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

											var stringVal = string.Format("{0:n0} ISK", value);

											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting  Radius Kill: {kill.killID}  Value: {stringVal}"));
										}
									}
								}
								catch (ApiException ex)
								{
									if (ex.Message == "Error calling GetRouteOriginDestination: {\"error\":\"No route found\"}")
									{
										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Radius Kill: {kill.killID} Inside a WH"));
									}
									else
									{
										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, $"killFeed", $"{ex.Message}"));

									}
								}
							}

							if (bigKillGlobal != 0 && value >= bigKillGlobal && !postedGlobalBigKill)
							{
								postedGlobalBigKill = true;
								var builder = new EmbedBuilder()
									.WithColor(new Color(0xFA2FF4))
									.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
									.WithAuthor(author =>
									{
										author
											.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
									})
									.WithDescription($"[Big Kill Reported: {ship.Name} destroyed in {system.Name} ({secstatus})](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
									.AddField("Victim", victimName)
									.AddField("Corporation", victimCorp.Name, true)
									.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
									.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
									.AddField("Involved Count", attackers.Count(), true);
								var embed = builder.Build();

								var _Channel = discordGuild.GetTextChannel(bigKillGlobalChan);
								await _Channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

								var stringVal = string.Format("{0:n0} ISK", value);

								await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting Global Big Kill: {kill.killID}  Value: {stringVal}"));

							}
							if (allianceID == 0 && corpID == 0)
							{
								if (value >= minimumValue)
								{
									var builder = new EmbedBuilder()
										.WithColor(new Color(0x00FF00))
										.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
										.WithAuthor(author =>
										{
											author
												.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
										})
										.WithDescription($"[Kill Reported: {ship.Name} destroyed in {system.Name} ({secstatus})](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
										.AddField("Victim", victimName, true)
										.AddField("Corporation", victimCorp.Name, true)
										.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
										.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
										.AddField("Involved Count", attackers.Count(), true);
									var embed = builder.Build();

									var Channel = discordGuild.GetTextChannel(Convert.ToUInt64(i["channel"]));
									await Channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

									var stringVal = string.Format("{0:n0} ISK", value);

									await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting Global Kills: {kill.killID}  Value: {stringVal}"));
								}
							}
							else
							{
								//Losses
								if (bigKillValue != 0 && value >= bigKillValue)
								{
									if (victimAllianceID == allianceID && lastChannel != c || victimCorpID == corpID && lastChannel != c)
									{
										var builder = new EmbedBuilder()
											.WithColor(new Color(0xD00000))
											.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
											.WithAuthor(author =>
											{
												author
													.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
											})
											.WithDescription($"[Big Loss Reported: {ship.Name} destroyed in {system.Name} ({secstatus})](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
											.AddField("Victim", victimName, true)
											.AddField("Corporation", victimCorp.Name, true)
											.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
											.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
											.AddField("Involved Count", attackers.Count(), true);
										var embed = builder.Build();

										var Channel = discordGuild.GetTextChannel(Convert.ToUInt64(i["bigKillChannel"]));
										await Channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

										var stringVal = string.Format("{0:n0} ISK", value);

										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting     Big Loss: {kill.killID}  Value: {stringVal}"));

										lastChannel = c;

										continue;
									}
								}
								else
								{
									if (minimumLossValue == 0 || minimumLossValue <= value)
									{
										if (victimAllianceID != 0 && victimAllianceID == allianceID && lastChannel != c || victimCorpID == corpID && lastChannel != c)
										{
											var builder = new EmbedBuilder()
												.WithColor(new Color(0xFF0000))
												.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
												.WithAuthor(author =>
												{
													author
														.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
												})
												.WithDescription($"[Loss Reported: {ship.Name} destroyed in {system.Name} ({secstatus})](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
												.AddField("Victim", victimName, true)
												.AddField("Corporation", victimCorp.Name, true)
												.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
												.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
												.AddField("Involved Count", attackers.Count(), true);
											var embed = builder.Build();

											var Channel = discordGuild.GetTextChannel(Convert.ToUInt64(i["channel"]));
											await Channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

											var stringVal = string.Format("{0:n0} ISK", value);

											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting         Loss: {kill.killID}  Value: {stringVal}"));

											lastChannel = c;

											continue;
										}
									}
								}

								//Killed
								foreach (var attacker in attackers.ToList())
								{
									if (bigKillValue != 0 && value >= bigKillValue && !npckill)
									{
										if (attacker.alliance_id != 0 && attacker.alliance_id == allianceID && lastChannel != c || allianceID == 0 && attacker.corporation_id == corpID && lastChannel != c)
										{
											var builder = new EmbedBuilder()
												.WithColor(new Color(0x00D000))
												.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
												.WithAuthor(author =>
												{
													author
														.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
												})
												.WithDescription($"[Big Kill Reported: {ship.Name} destroyed in {system.Name} ({secstatus})](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
												.AddField("Victim", victimName, true)
												.AddField("Corporation", victimCorp.Name, true)
												.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
												.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
												.AddField("Involved Count", attackers.Count(), true);
											var embed = builder.Build();

											var Channel = discordGuild.GetTextChannel(Convert.ToUInt64(i["bigKillChannel"]));
											await Channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

											var stringVal = string.Format("{0:n0} ISK", value);

											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting     Big Kill: {kill.killID}  Value: {stringVal}"));

											lastChannel = c;

											break;
										}
									}
									else if (!npckill && attacker.alliance_id != 0 && allianceID != 0 && attacker.alliance_id == allianceID && lastChannel != c ||
										!npckill && allianceID == -1 && attacker.corporation_id == corpID && lastChannel != c)
									{
										var builder = new EmbedBuilder()
											.WithColor(new Color(0x00FF00))
											.WithThumbnailUrl($"https://image.eveonline.com/Type/{shipID}_64.png")
											.WithAuthor(author =>
											{
												author
													.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
											})
												.WithDescription($"[Kill Reported: {ship.Name} destroyed in {system.Name} ({secstatus})](https://zkillboard.com/kill/{iD}) \n\r Died {killTime}")
												.AddField("Victim", victim == null ? victimName : victimName, true)
												.AddField("Corporation", victimCorp.Name, true)
												.AddField("Alliance", victimAlliance == null ? "None" : victimAlliance.Name, true)
												.AddField("Total Value", string.Format("{0:n0} ISK", value), true)
												.AddField("Involved Count", attackers.Count(), true);
										var embed = builder.Build();

										var Channel = discordGuild.GetTextChannel(Convert.ToUInt64(i["channel"]));
										await Channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);

										var stringVal = string.Format("{0:n0} ISK", value);

										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Posting         Kill: {kill.killID}  Value: {stringVal}"));

										lastChannel = c;
										lastPosted = iD;
										break;

									}
									else if (kill != null && kill != null && lastPosted != 0 && lastPosted == kill.killID && lastChannel == c)
									{
										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, $"killFeed", $"Skipping kill: {kill.killID} as its been posted recently"));
									}
								}
							}
						}
						Lastkill.Add(kill.killID);
					}
				}
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, $"killFeed", ex.Message, ex));
			}
		}

		#endregion

		//Teamspeak
		#region Teamspeak
		internal static ServerQueryClient TS_client;

		internal async static Task TeamspeakCheck()
		{
			var hostname = Program.Settings.GetSection("teamspeak")["hostname"];
			var queryport = Convert.ToInt16(Program.Settings.GetSection("teamspeak")["queryport"]);
			var username = Program.Settings.GetSection("teamspeak")["username"];
			var password = Program.Settings.GetSection("teamspeak")["password"];
			var serverport = Convert.ToInt16(Program.Settings.GetSection("teamspeak")["serverport"]);

			var botName = Program.Settings.GetSection("config")["name"];
			var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
			var discordUsers = Program.Client.GetGuild(guildID).Users;
			var discordGuild = Program.Client.GetGuild(guildID);

			var queryCheck = $"SELECT * FROM teamspeakUsers";
			try
			{
				var responceCheck = MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryCheck).Result;
			}
			catch (AggregateException ex)
			{
				try
				{
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				}
				catch (MySqlException error) when (error.Number == (int)MySqlErrorCode.NoSuchTable)
				{
					var queryTableAdd = $"CREATE TABLE teamspeakUsers (id VARCHAR(64) NOT NULL, uid varchar(64) NOT NULL, PRIMARY KEY (id))";
					try
					{
						var responceCheck = MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryTableAdd).Result;
					}
					catch
					{

					}
				}
			}

			if (!_teamspeakRunning)
			{
				TS_client = new ServerQueryClient(hostname, queryport, TimeSpan.FromMilliseconds(50));

				await TS_client.Initialize();
				var result = await TS_client.Login(username, password);

				if (result.Success)
				{
					_teamspeakRunning = true;
					await TS_client.Use(UseServerBy.Port, serverport);
					var whoAmI = await TS_client.WhoAmI();
					var server = await TS_client.ServerInfo();
					await TS_client.ServerNotifyRegister(Event.textchannel);
					await TS_client.ServerNotifyRegister(Event.textprivate);
					await TS_client.ServerNotifyRegister(Event.textserver);
					await TS_client.ClientUpdate(Program.Settings.GetSection("config")["name"]);
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Connected to {server.VirtualServerName} as {whoAmI.ClientLoginName}"));
					//await TS_client.SendTextMessage(TextMessageTargetMode.TextMessageTarget_SERVER, server.VirtualServerId, $"{botName} Connected");
					TS_client.ConnectionClosed += Teamspeak_Closed;
					TS_client.NotifyTextMessage += Teamspeak_TextMessage;
				}
			}
			else if (_teamspeakRunning)
			{
				if (DateTime.Now > TsLastRan.AddMinutes(5))
				{
					var clientList = await TS_client.ClientList();
					var serverGroups = await TS_client.SendCommandAsync(new ServerQueryCommand<ServerGroupListResult>(Command.servergrouplist));

					foreach (var client in clientList.Values)
					{
						if (client.ClientType == "0")
						{
							var TSquery = $"SELECT * FROM teamspeakUsers WHERE uid=\"{client.ClientUniqueIdentifier}\"";
							var TSresponce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], TSquery);

							String disID = "";
							IList<IDictionary<string, object>> responce = null;

							if (TSresponce.Count > 0)
							{
								disID = TSresponce == null ? "0" : TSresponce[0]["id"].ToString();
								var query = $"SELECT * FROM authUsers WHERE discordID=\"{disID}\"";
								responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
							}

							var discordUser = discordUsers.FirstOrDefault(x => x.Id.ToString() == disID);

							if (responce == null || responce.Count > 0 && responce[0]["active"].ToString() == "no")
							{
								foreach (int i in Array.ConvertAll(client.ClientServerGroups.Split(','), int.Parse))
								{
									var tmp = serverGroups.Values.FirstOrDefault(x => x.Sgid == i);
									if (tmp.Name != "Server Admin")
									{
										if (tmp.Name != "Guest")
										{
											var result = await TS_client.SendCommandAsync($"servergroupdelclient sgid={tmp.Sgid} cldbid={client.ClientDatabaseId}");
											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Revoking group {tmp.Name} from {client.ClientNickname}"));
										}
									}
								}
								if (discordUser != null)
								{
									var queryDel = $"DELETE FROM teamspeakUsers WHERE id=\"{discordUser.Id}\"";
									var responceDel = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryDel);
								}
							}
							else if (responce.Count > 0 && responce[0]["active"].ToString() == "yes" && discordUser != null)
							{
								if (client.ClientNickname != responce[0]["eveName"].ToString())
								{
									foreach (int i in Array.ConvertAll(client.ClientServerGroups.Split(','), int.Parse))
									{
										var tmp = serverGroups.Values.FirstOrDefault(x => x.Sgid == i);
										if (tmp.Name != "Server Admin")
										{
											var result = await TS_client.SendCommandAsync($"servergroupdelclient sgid={tmp.Sgid} cldbid={client.ClientDatabaseId}");
											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Revoking group {tmp.Name} from {client.ClientNickname}"));
										}
									}
									if (discordUser != null)
									{
										var queryDel = $"DELETE FROM teamspeakUsers WHERE id=\"{discordUser.Id}\"";
										var responceDel = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryDel);
									}
									await discordUser.SendMessageAsync($"Roles have been revoked on Teamspeak due to your Nickname being wrong. Please set your teamspeak Nickname to {responce[0]["eveName"]}");
								}
							}
							else if (responce.Count == 0)
							{
								foreach (int i in Array.ConvertAll(client.ClientServerGroups.Split(','), int.Parse))
								{
									var tmp = serverGroups.Values.FirstOrDefault(x => x.Sgid == i);
									if (tmp.Name != "Server Admin")
									{
										var result = await TS_client.SendCommandAsync($"servergroupdelclient sgid={tmp.Sgid} cldbid={client.ClientDatabaseId}");
										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Revoking group {tmp.Name} from {client.ClientNickname}"));
									}
									if (discordUser != null)
									{
										var queryDel = $"DELETE FROM teamspeakUsers WHERE id=\"{discordUser.Id}\"";
										var responceDel = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryDel);
									}
								}
							}
						}
					}

					TsLastRan = DateTime.Now;
				}
			}
		}

		private static async void Teamspeak_TextMessage(object sender, NotifyTextMessageResult e)
		{
			await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Message From: {e.Invokername}, {e.Msg}"));
		}

		private static async void Teamspeak_Closed(object sender, EventArgs e)
		{
			await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Connection Lost"));
			TS_client = null;
			_teamspeakRunning = false;
		}

		internal static async Task TS_Check(ICommandContext context)
		{
			try
			{
				var hostname = Program.Settings.GetSection("teamspeak")["hostname"];
				var queryport = Convert.ToInt16(Program.Settings.GetSection("teamspeak")["queryport"]);
				var username = Program.Settings.GetSection("teamspeak")["username"];
				var password = Program.Settings.GetSection("teamspeak")["password"];
				var serverport = Convert.ToInt16(Program.Settings.GetSection("teamspeak")["serverport"]);
				var serverpassword = Program.Settings.GetSection("teamspeak")["serverpassword"];

				var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
				var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
				var discordUsers = Program.Client.GetGuild(guildID).Users;
				var discordGuild = Program.Client.GetGuild(guildID);
				var corps = new Dictionary<string, string>();
				var alliance = new Dictionary<string, string>();

				var serverGroups = TS_client.SendCommandAsync(new ServerQueryCommand<ServerGroupListResult>(Command.servergrouplist)).Result.Values.ToList();

				var test = await TS_client.ServerInfo();

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

				var TSUsers = await TS_client.ClientList();
				var query = $"SELECT * FROM authUsers WHERE discordID=\"{context.User.Id}\" AND active=\"yes\"";

				var responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
				if (responce.Count() == 0)
				{
					await context.Channel.SendMessageAsync($"{context.Message.Author.Mention} not authed on Discord yet use !auth");
				}
				else
				{
					var count = 1;
					foreach (var c in TSUsers.Values)
					{
						var teamspeakUIDS = $"SELECT * FROM teamspeakUsers";
						var reponceTeamspeak = MysqlQuery(Program.Settings.GetSection("config")["connstring"], teamspeakUIDS).Result;

						var characterID = responce.OrderByDescending(x => x["id"]).FirstOrDefault()["characterID"];
						var teamspeakEntry = reponceTeamspeak.FirstOrDefault(x => x["id"].ToString() == context.User.Id.ToString());
						var test1 = (responce[0]["eveName"].ToString() == c.ClientNickname);
						var test2 = Convert.ToUInt64(responce[0]["discordID"]) == context.User.Id;
						var test3 = teamspeakEntry == null;

						CharacterData characterData = new CharacterData();
						CorporationData corporationData = new CorporationData();

						if (test1 && test2 && test3)
						{
							try
							{
								var responceMessage = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{characterID}/?datasource=tranquility");
								characterData = JsonConvert.DeserializeObject<CharacterData>(await responceMessage.Content.ReadAsStringAsync());

								responceMessage = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/corporations/{characterData.corporation_id}/?datasource=tranquility");
								corporationData = JsonConvert.DeserializeObject<CorporationData>(await responceMessage.Content.ReadAsStringAsync());

								//Check for Corp roles
								if (corps.ContainsKey(characterData.corporation_id.ToString()))
								{
									var corp = corps.FirstOrDefault(x => x.Key == characterData.corporation_id.ToString());
									var group = serverGroups.FirstOrDefault(x => x.Name == corp.Value);
									if (group != null)
									{
										var result = await TS_client.SendCommandAsync($"servergroupaddclient sgid={group.Sgid} cldbid={c.ClientDatabaseId}");
										if (result.Success)
										{
											var teamspeakAddUIDS = $"INSERT INTO teamspeakUsers (id, uid) values(\"{context.User.Id}\", \"{c.ClientUniqueIdentifier}\")";
											var reponceAddTeamspeak = MysqlQuery(Program.Settings.GetSection("config")["connstring"], teamspeakAddUIDS).Result;

											await context.Channel.SendMessageAsync($"{context.User.Mention}, Roles were given for Corp {corp.Value}");
											await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Giving group {group.Name} to {c.ClientNickname}"));
										}
										else if (result.ErrorId == 2561)
										{
											await context.Channel.SendMessageAsync($"{context.User.Mention}, You all ready have the role for {corp.Value}");
										}
										else
										{
											await context.Channel.SendMessageAsync($"{context.User.Mention}, your request failed.");
										}
									}
									else
									{
										await context.Channel.SendMessageAsync($"{context.User.Mention}, Teamspeak Role {corp.Value} is missing contact the Teamspeak Admin");
									}
								}

								//Check for Alliance roles
								if (characterData.alliance_id != null)
								{
									if (alliance.ContainsKey(characterData.alliance_id.ToString()))
									{
										var ally = alliance.FirstOrDefault(x => x.Key == characterData.alliance_id.ToString());
										var group = serverGroups.FirstOrDefault(x => x.Name == ally.Value);
										var groupCorp = serverGroups.FirstOrDefault(x => x.Name == corporationData.ticker);
										if (group != null)
										{
											var result = await TS_client.SendCommandAsync($"servergroupaddclient sgid={group.Sgid} cldbid={c.ClientDatabaseId}");
											TextResult resultCorp;
											if (Convert.ToBoolean(Program.Settings.GetSection("teamspeak")["corpaswell"]))
											{
												try
												{
													resultCorp = await TS_client.SendCommandAsync($"servergroupaddclient sgid={groupCorp.Sgid} cldbid={c.ClientDatabaseId}");

													if (resultCorp.Success)
													{
														await context.Channel.SendMessageAsync($"{context.User.Mention}, Roles were given for Corp {corporationData.ticker}");
														await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Giving group {group.Name} to {c.ClientNickname}"));
													}
													else if (result.ErrorId == 2561)
													{
														await context.Channel.SendMessageAsync($"{context.User.Mention}, You all ready have the role for {corporationData.ticker}");
													}
													else
													{
														await context.Channel.SendMessageAsync($"{context.User.Mention}, your request failed.");
													}
												}
												catch { }
											}
											if (result.Success)
											{
												var teamspeakAddUIDS = $"INSERT INTO teamspeakUsers (id, uid) values(\"{context.User.Id}\", \"{c.ClientUniqueIdentifier}\")";
												var reponceAddTeamspeak = MysqlQuery(Program.Settings.GetSection("config")["connstring"], teamspeakAddUIDS).Result;
												await context.Channel.SendMessageAsync($"{context.User.Mention}, Roles were given for Alliance {ally.Value}");
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Giving group {group.Name} to {c.ClientNickname}"));
											}
											else if (result.ErrorId == 2561)
											{
												await context.Channel.SendMessageAsync($"{context.User.Mention}, You all ready have the role for {ally.Value}");
											}
											else
											{
												await context.Channel.SendMessageAsync($"{context.User.Mention}, your request failed.");
											}
										}
										else
										{
											await context.Channel.SendMessageAsync($"{context.User.Mention}, Teamspeak Role {ally.Value} is missing contact the Teamspeak Admin");
										}
									}
								}
								continue;
							}
							catch (Exception ex)
							{
								var error = ex;
							}
						}
						else if (responce[0]["eveName"].ToString() == c.ClientNickname && Convert.ToUInt64(responce[0]["discordID"]) == context.User.Id && teamspeakEntry != null)
						{
							await context.Channel.SendMessageAsync($"Please release your Other Teamspeak account binding with !ts reset whilst connected (If this fails contact support)");
							continue;
						}
						else if (TSUsers.Values.Count() == count)
						{
							await context.Channel.SendMessageAsync($"Please connect to teamspeak first http://www.teamspeak.com/invite/{HttpUtility.UrlEncode(hostname)}" +
								$"/?port{serverport}&password={HttpUtility.UrlEncode(serverpassword)}&nickname={HttpUtility.UrlEncode(responce[0]["eveName"].ToString())}");
						}
						count++;
					}
				}
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"ERROR {ex.Message}", ex));
			}
		}

		internal static async Task TS_Reset(ICommandContext context)
		{
			var TSquery = $"SELECT * FROM teamspeakUsers WHERE id=\"{context.User.Id}\"";
			var TSresponce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], TSquery);

			String disID = "";
			IList<IDictionary<string, object>> responce = null;

			if (TSresponce.Count > 0)
			{
				disID = TSresponce == null ? "0" : TSresponce[0]["id"].ToString();
				var query = $"SELECT * FROM authUsers WHERE discordID=\"{disID}\"";
				responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
			}

			var clientList = await TS_client.ClientList();

			var client = clientList.Values.SingleOrDefault(x => x.ClientUniqueIdentifier == TSresponce[0]["uid"].ToString());
			var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
			var serverGroups = await TS_client.SendCommandAsync(new ServerQueryCommand<ServerGroupListResult>(Command.servergrouplist));
			var discordUser = Program.Client.GetGuild(guildID).GetUser(context.Message.Author.Id);

			if (client != null && client.ClientServerGroups.Count() != 0)
			{
				foreach (int i in Array.ConvertAll(client.ClientServerGroups.Split(','), int.Parse))
				{
					var tmp = serverGroups.Values.FirstOrDefault(x => x.Sgid == i);
					if (tmp.Name != "Server Admin")
					{
						var result = await TS_client.SendCommandAsync($"servergroupdelclient sgid={tmp.Sgid} cldbid={client.ClientDatabaseId}");
						await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Revoking group {tmp.Name} from {client.ClientNickname}"));
					}
				}
			}
			var queryDel = $"DELETE FROM teamspeakUsers WHERE id=\"{discordUser.Id}\"";
			var responceDel = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryDel);
			await context.Channel.SendMessageAsync($"{context.User.Mention}, Roles have been revoked on Teamspeak because of your reset request");
		}

		internal static async Task TS_Reset(ICommandContext context, string DiscordID)
		{
			var TSquery = $"SELECT * FROM teamspeakUsers WHERE id=\"{DiscordID}\"";
			var TSresponce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], TSquery);

			IList<IDictionary<string, object>> responce = null;

			if (TSresponce.Count > 0)
			{
				//disID = TSresponce == null ? "0" : TSresponce[0]["id"].ToString();
				var query = $"SELECT * FROM authUsers WHERE discordID=\"{DiscordID}\"";
				responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
			}

			var clientList = await TS_client.ClientList();

			var client = clientList.Values.SingleOrDefault(x => x.ClientUniqueIdentifier == TSresponce[0]["uid"].ToString());
			var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
			var serverGroups = await TS_client.SendCommandAsync(new ServerQueryCommand<ServerGroupListResult>(Command.servergrouplist));
			var discordUser = Program.Client.GetGuild(guildID).GetUser(context.Message.Author.Id);

			foreach (int i in Array.ConvertAll(client.ClientServerGroups.Split(','), int.Parse))
			{
				var tmp = serverGroups.Values.FirstOrDefault(x => x.Sgid == i);
				if (tmp.Name != "Server Admin")
				{
					var result = await TS_client.SendCommandAsync($"servergroupdelclient sgid={tmp.Sgid} cldbid={client.ClientDatabaseId}");
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Revoking group {tmp.Name} from {client.ClientNickname}"));
				}
			}
			var queryDel = $"DELETE FROM teamspeakUsers WHERE id=\"{DiscordID}\"";
			var responceDel = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryDel);
			await context.Channel.SendMessageAsync($"{context.User.Mention}, Roles have been revoked on Teamspeak because of your reset request");
		}

		internal static async Task TS_ResetAdmin(ICommandContext context)
		{
			var TSquery = $"SELECT * FROM teamspeakUsers WHERE id=\"{context}\"";
			var TSresponce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], TSquery);

			String disID = "";
			IList<IDictionary<string, object>> responce = null;

			if (TSresponce.Count > 0)
			{
				disID = TSresponce == null ? "0" : TSresponce[0]["id"].ToString();
				var query = $"SELECT * FROM authUsers WHERE discordID=\"{disID}\"";
				responce = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
			}

			var clientList = await TS_client.ClientList();

			var client = clientList.Values.SingleOrDefault(x => x.ClientUniqueIdentifier == TSresponce[0]["uid"].ToString());
			var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
			var serverGroups = await TS_client.SendCommandAsync(new ServerQueryCommand<ServerGroupListResult>(Command.servergrouplist));
			var discordUser = Program.Client.GetGuild(guildID).GetUser(context.Message.Author.Id);

			foreach (int i in Array.ConvertAll(client.ClientServerGroups.Split(','), int.Parse))
			{
				var tmp = serverGroups.Values.FirstOrDefault(x => x.Sgid == i);
				if (tmp.Name != "Server Admin")
				{
					var result = await TS_client.SendCommandAsync($"servergroupdelclient sgid={tmp.Sgid} cldbid={client.ClientDatabaseId}");
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Teamspeak", $"Revoking group {tmp.Name} from {client.ClientNickname}"));
				}
			}
			var queryDel = $"DELETE FROM teamspeakUsers WHERE id=\"{discordUser.Id}\"";
			var responceDel = await MysqlQuery(Program.Settings.GetSection("config")["connstring"], queryDel);
			await context.Channel.SendMessageAsync($"{context.User.Mention}, Roles have been revoked on Teamspeak because of your reset request");
		}
		#endregion

		//Notifications
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
				{160,   "Sovereignty structure reinforced"},
				{161,   "Command Node Event Started"},
				{162,   "Sovereignty structure destroyed"},
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
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", "Running Notification Check"));
					_lastNotification = Convert.ToInt32(await SQLiteDataQuery("cacheData", "data", "lastNotificationID"));
					var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
					var channelId = Convert.ToUInt64(Program.Settings.GetSection("notifications")["channelId"]);
					var keyID = "";
					var vCode = "";
					var characterID = "";
					var keys = Program.Settings.GetSection("notifications").GetSection("keys").GetChildren();
					var filters = Program.Settings.GetSection("notifications").GetSection("filters").GetChildren().ToDictionary(x => x.Key, x => x.Value);
					var keyCount = keys.Count();
					var nextKey = await SQLiteDataQuery("notifications", "data", "nextKey");
					var index = 0;
					var runComplete = false;

					foreach (var key in keys)
					{
						if (key.Key != nextKey && !String.IsNullOrWhiteSpace(nextKey))
						{
							index++;
						}
						if (nextKey == null && !runComplete || String.IsNullOrWhiteSpace(nextKey) && !runComplete || nextKey == key.Key && !runComplete)
						{
							characterID = key["characterID"];
							keyID = key["keyID"];
							vCode = key["vCode"];

							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Checking characterID:{characterID} keyID:{keyID} vCode:{vCode}"));

							await EveLib.SetApiKey(keyID, vCode, characterID);
							var notifications = await EveLib.GetNotifications();
							var notificationsSort = notifications.OrderBy(x => x.Key);

							if (notifications.Count > 0)
							{
								var notiIDs = new List<int>();

								foreach (var l in notifications)
								{
									notiIDs.Add(l.Key);
								}

								var notificationsText = await EveLib.GetNotificationText(notiIDs);

								foreach (var notification in notificationsSort)
								{
									try
									{
										if (filters.ContainsKey(notification.Value["typeID"].ToString()) && (int)notification.Value["notificationID"] > _lastNotification &&
											notificationsText.ContainsKey(notification.Key) || filters.ContainsKey("999999") && (int)notification.Value["notificationID"] > _lastNotification &&
										   notificationsText.ContainsKey(notification.Key))
										{
											var chan = Program.Client.GetGuild(guildID).GetTextChannel(Convert.ToUInt64(filters.FirstOrDefault(
												x => x.Key == notification.Value["typeID"].ToString()).Value));

											YamlNode notificationText = null;

											var notificationType = (int)notification.Value["typeID"];

											try
											{
												notificationText = notificationsText.FirstOrDefault(x => x.Key == notification.Key).Value;
											}
											catch (Exception ex)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"ERROR Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}", ex));

												if (notificationsText != null)
												{
													foreach (var noti in notificationsText)
													{
														await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"NoticationText {noti}"));
													}
												}
											}

											if (notificationType == 5)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
												var cost = notificationText["cost"].AllNodes.ToList()[0];
												var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
												var delayHours = notificationText["delayHours"].AllNodes.ToList()[0].ToString();
												var hostileState = notificationText["hostileState"].AllNodes.ToList()[0].ToString();
												var names = await EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
												var againstName = names.FirstOrDefault(x => x.Key == againstID);
												var declaredByName = names.First(x => x.Key == declaredByID);

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.AddField("Declared BY", declaredByName.Value, true)
													.AddField("Against", againstName.Value, true)
													.AddField("Fighting beins in", $"{delayHours} Hours", true)
													.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 7)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
												var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());

												var stuff = await EveLib.IDtoName(new List<Int64> { againstID, declaredByID });
												var againstName = stuff.FirstOrDefault(x => x.Key == againstID).Value;
												var declaredByName = stuff.FirstOrDefault(x => x.Key == declaredByID).Value;

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.WithDescription($"{declaredByName} Retracts War Against {againstName}")
													.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 16)
											{
												var responce = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{notificationText["charID"]}/?datasource=tranquility");
												CharacterData characterData = new CharacterData();
												if (responce.IsSuccessStatusCode)
												{
													characterData = JsonConvert.DeserializeObject<CharacterData>(await responce.Content.ReadAsStringAsync());
												}

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.AddField("Character", characterData.name)
													.AddField("Text", notificationText["applicationText"])
													.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 27)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
												var cost = notificationText["cost"].AllNodes.ToList()[0];
												var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
												var delayHours = notificationText["delayHours"].AllNodes.ToList()[0].ToString();
												var hostileState = notificationText["hostileState"].AllNodes.ToList()[0].ToString();
												var names = await EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
												var againstName = names.FirstOrDefault(x => x.Key == againstID);
												var declaredByName = names.First(x => x.Key == declaredByID);

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.AddField("Declared BY", declaredByName.Value, true)
													.AddField("Against", againstName.Value, true)
													.AddField("Fighting beins in", $"{delayHours} Hours", true)
													.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 30)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var againstID = Convert.ToInt64(notificationText["againstID"].AllNodes.ToList()[0].ToString());
												var cost = notificationText["cost"].AllNodes.ToList()[0];
												var declaredByID = Convert.ToInt64(notificationText["declaredByID"].AllNodes.ToList()[0].ToString());
												var names = await EveLib.IDtoName(new List<Int64> { declaredByID, againstID });
												var againstName = names.FirstOrDefault(x => x.Key == againstID).Value;
												var declaredByName = names.First(x => x.Key == declaredByID).Value;

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.WithDescription($"{declaredByName} Retracts War Against {againstName}")
													.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 75)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												Int64.TryParse(notificationText["aggressorAllianceID"].AllNodes.ToList()[0].ToString(), out long allyResult);
												var aggressorAllianceID = allyResult;
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
												var TypeName = await EveLib.IDtoTypeName(new List<Int64> { typeID });
												var aggressorAllianceName = aggressorAlliance ?? "None";

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.AddField("Details", $"System: {moonName}", true)
													.AddField("Type", $"{TypeName.First(x => x.Key == typeID).Value}", true)
													.AddField("Current Shield Level", $"{shieldValue}", true)
													.AddField("Current Armor Level", $"{armorValue}", true)
													.AddField("Current Hull Level", $"{hullValue}", true)
													.AddField("Aggressing Pilot", $"{aggressorName}", true)
													.AddField("\u200b", "\u200b", true)
													.AddField("\u200b", "\u200b", true)
													.AddField("Aggressing Corporation", $"{aggressorCorpName}", true)
													.AddField("Aggressing Alliance", $"{aggressorAllianceName}", true)
													.WithTimestamp((DateTime)notification.Value["sentDate"]);

												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 93)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												Int64.TryParse(notificationText["aggressorAllianceID"].AllNodes.ToList()[0].ToString(), out long allyResult);
												var aggressorAllianceID = allyResult;
												var aggressorCorpID = Convert.ToInt64(notificationText["aggressorCorpID"].AllNodes.ToList()[0].ToString());
												var aggressorID = Convert.ToInt64(notificationText["aggressorID"].AllNodes.ToList()[0].ToString());
												var typeID = Convert.ToInt64(notificationText["typeID"].AllNodes.ToList()[0].ToString());
												var planetID = Convert.ToInt64(notificationText["planetID"].AllNodes.ToList()[0].ToString());
												var planetTypeID = Convert.ToInt64(notificationText["planetTypeID"].AllNodes.ToList()[0].ToString());
												var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
												var shieldValue = string.Format("{0:P2}", Convert.ToDouble(notificationText["shieldLevel"].AllNodes.ToList()[0].ToString()));
												var names = await EveLib.IDtoName(new List<Int64> { aggressorAllianceID, aggressorCorpID, aggressorID, planetID, solarSystemID });
												var aggressorAlliance = names.FirstOrDefault(x => x.Key == aggressorAllianceID).Value;
												var aggressorCorpName = names.First(x => x.Key == aggressorCorpID).Value;
												var aggressorName = names.First(x => x.Key == aggressorID).Value;
												var planetName = names.First(x => x.Key == planetID).Value;
												var solarSystemName = names.First(x => x.Key == solarSystemID).Value;
												var allyLine = aggressorAllianceID != 0 ? $"{Environment.NewLine}Aggressing Alliance: {aggressorAlliance}" : "";
												var TypeName = await EveLib.IDtoTypeName(new List<Int64> { typeID });

												var builder = new EmbedBuilder()
												.WithColor(new Color(0xf2882b))
												.WithAuthor(author =>
												{
													author
														.WithName($"New Notification: {types[notificationType]}");
												})
												.AddField("Details", $"System: {solarSystemName} Planet: {planetName}", true)
												.AddField("Type", $"{TypeName.First(x => x.Key == typeID).Value}", true)
												.AddField("Current Shield Level", $"{shieldValue}", true)
												.AddField("Aggressing Pilot", $"{aggressorName}", true)
												.AddField("Aggressing Corporation", $"{aggressorCorpName}{allyLine}", true)
												.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);

											}
											else if (notificationType == 100)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var allyID = Convert.ToInt64(notificationText["allyID"].AllNodes.ToList()[0].ToString());
												var defenderID = Convert.ToInt64(notificationText["defenderID"].AllNodes.ToList()[0].ToString());

												var stuff = await EveLib.IDtoName(new List<Int64> { allyID, defenderID });
												var allyName = stuff.FirstOrDefault(x => x.Key == allyID).Value;
												var defenderName = stuff.FirstOrDefault(x => x.Key == defenderID).Value;
												var startTime = DateTime.FromFileTimeUtc(Convert.ToInt64(notificationText["startTime"].AllNodes.ToList()[0].ToString()));

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.WithDescription($"{allyName} will join the war against {defenderName} at {startTime} EVE")
													.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 121)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var aggressorID = Convert.ToInt64(notificationText["entityID"].AllNodes.ToList()[0].ToString());
												var defenderID = Convert.ToInt64(notificationText["defenderID"].AllNodes.ToList()[0].ToString());

												var stuff = await EveLib.IDtoName(new List<Int64> { aggressorID, defenderID });
												var aggressorName = stuff.FirstOrDefault(x => x.Key == aggressorID).Value;
												var defenderName = stuff.FirstOrDefault(x => x.Key == defenderID).Value;

												var builder = new EmbedBuilder()
												.WithColor(new Color(0xf2882b))
												.WithAuthor(author =>
												{
													author
														.WithName($"New Notification: {types[notificationType]}");
												})
												.WithDescription($"War declared by {aggressorName} against {defenderName}. Fighting begins in roughly 24 hours.")
												.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 130)
											{
												var responce = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{notificationText["charID"]}/?datasource=tranquility");
												CharacterData characterData = new CharacterData();
												if (responce.IsSuccessStatusCode)
												{
													characterData = JsonConvert.DeserializeObject<CharacterData>(await responce.Content.ReadAsStringAsync());
												}

												var builder = new EmbedBuilder()
													.WithColor(new Color(0xf2882b))
													.WithAuthor(author =>
													{
														author
															.WithName($"New Notification: {types[notificationType]}");
													})
													.AddField("Character", characterData.name)
													.AddField("Text", notificationText["applicationText"])
													.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 147)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
												var structureTypeID = Convert.ToInt64(notificationText["structureTypeID"].AllNodes.ToList()[0].ToString());
												var names = await EveLib.IDtoName(new List<Int64> { solarSystemID });
												var typeNames = await EveLib.IDtoTypeName(new List<Int64> { structureTypeID });
												var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);
												var structureTypeName = typeNames.FirstOrDefault(x => x.Key == structureTypeID);

												var builder = new EmbedBuilder()
												.WithColor(new Color(0xf2882b))
												.WithAuthor(author =>
												{
													author
														.WithName($"New Notification: {types[notificationType]}");
												})
												.WithDescription($"Entosis Link started in {solarSystemName.Value} on {structureTypeName.Value}.")
												.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 160)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var campaignEventType = notificationText["campaignEventType"].AllNodes.ToList()[0];
												var solarSystemID = Convert.ToInt64((notificationText["solarSystemID"].AllNodes.ToList()[0].ToString()));
												var decloakTime = Convert.ToInt64(notificationText["decloakTime"].AllNodes.ToList()[0].ToString());
												var names = await EveLib.IDtoName(new List<Int64> { solarSystemID });
												var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);
												var decloaktime = DateTime.FromFileTime(decloakTime);

												var builder = new EmbedBuilder()
												.WithColor(new Color(0xf2882b))
												.WithAuthor(author =>
												{
													author
														.WithName($"New Notification: {types[notificationType]}");
												})
												.AddField("System", $"{solarSystemName.Value}", true)
												.AddField("Decloak Time", $"{decloaktime}", true)
												.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else if (notificationType == 161)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												var campaignEventType = notificationText["campaignEventType"].AllNodes.ToList()[0];
												var constellationID = notificationText["constellationID"].AllNodes.ToList()[0];
												var solarSystemID = Convert.ToInt64(notificationText["solarSystemID"].AllNodes.ToList()[0].ToString());
												var names = await EveLib.IDtoName(new List<Int64> { solarSystemID });
												var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);

												var builder = new EmbedBuilder()
												.WithColor(new Color(0xf2882b))
												.WithAuthor(author =>
												{
													author
														.WithName($"New Notification: {types[notificationType]}");
												})
												.WithDescription($"Command nodes decloaking for {solarSystemName.Value}.")
												.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);

											}
											else if (notificationType == 184)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Sending Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}"));
												Int64.TryParse(notificationText["allianceID"].AllNodes.ToList()[0].ToString(), out long allyResult);
												var aggressorAllianceID = allyResult;
												var armorValue = string.Format("{0:P2}", notificationText["armorPercentage"].AllNodes.ToList()[0].ToString());
												var aggressorID = Convert.ToInt64(notificationText["charID"].AllNodes.ToList()[0].ToString());
												var corpName = notificationText["corpName"];
												var shieldValue = string.Format("{0:P2}", notificationText["shieldPercentage"].AllNodes.ToList()[0].ToString());
												var hullValue = string.Format("{0:P2}", notificationText["hullPercentage"].AllNodes.ToList()[0].ToString());
												var solarSystemID = Convert.ToInt64(notificationText["solarsystemID"].AllNodes.ToList()[0].ToString());
												var structureID = Convert.ToInt64(notificationText["structureID"].AllNodes.ToList()[0].ToString());

												var names = await EveLib.IDtoName(new List<Int64> { aggressorAllianceID, aggressorID, solarSystemID });
												var namess = await EveLib.IDtoTypeName(new List<Int64> { structureID });

												var aggressorAlliance = names.FirstOrDefault(x => x.Key == aggressorAllianceID).Value;
												var aggressorName = names.First(x => x.Key == aggressorID).Value;
												var structureName = namess.First(x => x.Key == structureID).Value;
												var solarSystemName = names.FirstOrDefault(x => x.Key == solarSystemID);
												var allyLine = aggressorAllianceID != 0 ? $"{aggressorAlliance}" : "";

												var builder = new EmbedBuilder()
												.WithColor(new Color(0xf2882b))
												.WithAuthor(author =>
												{
													author
														.WithName($"New Notification: {types[notificationType]}");
												})
												.AddField("System", solarSystemName.Value, true)
												.AddField("Structure", structureName, true)
												.AddField("Current Shield Level", shieldValue, true)
												.AddField("Current Armor Level", armorValue, true)
												.AddField("Current Hull Level", hullValue, true)
												.AddField("Aggressing Pilot", aggressorName, true)
												.AddField("Aggressing Corporation", corpName, true)
												.AddField("Aggressing Alliance", allyLine, true)
												.WithTimestamp((DateTime)notification.Value["sentDate"]);
												var embed = builder.Build();

												await chan.SendMessageAsync($"@everyone", false, embed);
											}
											else
											{
												try
												{
													await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Skipping Notification TypeID: {notificationType} " +
														$"Type: {types[notificationType]} {Environment.NewLine} Text: {notificationText}"));
												}
												catch (KeyNotFoundException)
												{
													await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Skipping **NEW** Notification TypeID: {notificationType} " +
														$"{Environment.NewLine} Text: {notificationText}"));
												}
											}
											_lastNotification = (int)notification.Value["notificationID"];
											await SQLiteDataUpdate("cacheData", "data", "lastNotificationID", _lastNotification.ToString());
										}
										else if ((int)notification.Value["notificationID"] > _lastNotification && notificationsText.ContainsKey(notification.Key))
										{
											var chan = Program.Client.GetGuild(guildID).GetTextChannel(Convert.ToUInt64(filters.FirstOrDefault(
												x => x.Key == notification.Value["typeID"].ToString()).Value));

											YamlNode notificationText = null;

											var notificationType = (int)notification.Value["typeID"];

											try
											{
												notificationText = notificationsText.FirstOrDefault(x => x.Key == notification.Key).Value;
											}
											catch (Exception ex)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"ERROR Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]}", ex));
												if (notificationsText != null)
												{
													foreach (var noti in notificationsText)
													{
														await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"NoticationText {noti}"));
													}
												}
											}

											try
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Skipping Notification TypeID: {notificationType} " +
													$"Type: {types[notificationType]} {Environment.NewLine} Text: {notificationText}"));
											}
											catch (KeyNotFoundException)
											{
												await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Skipping **NEW** Notification TypeID: {notificationType} " +
													$"{Environment.NewLine} Text: {notificationText}"));
											}
											_lastNotification = (int)notification.Value["notificationID"];
											await SQLiteDataUpdate("cacheData", "data", "lastNotificationID", _lastNotification.ToString());
										}
									}
									catch (Exception ex)
									{
										await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "NotificationFeed", $"Error Notification", ex));
									}
								}
								runComplete = true;
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
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "NotificationFeed", ex.Message, ex));
			}
		}
		#endregion

		//Pricecheck Update to Embeds
		#region Pricecheck
		internal async static Task PriceCheck(ICommandContext context, string String, string system)
		{
			var channel = context.Message.Channel;

			try
			{
				HttpResponseMessage ItemID = new HttpResponseMessage();
				if (String.ToLower().StartsWith("search"))
				{
					ItemID = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/search/?categories=inventory_type&datasource=tranquility&language=en-us&search=" +
						$"{String.TrimStart(new char[] { 's', 'e', 'a', 'r', 'c', 'h' })}&strict=false");
				}
				else
				{
					ItemID = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/search/?categories=inventory_type&datasource=tranquility&language=en-us&search=" +
						$"{String.ToLower()}&strict=true");
				}

				if (!ItemID.IsSuccessStatusCode)
				{
					await channel.SendMessageAsync($"{context.Message.Author.Mention} ESI Failure ItemID please try again later");
					await Task.CompletedTask;
				}


				var ItemIDResult = await ItemID.Content.ReadAsStringAsync();

				var ItemIDResults = JsonConvert.DeserializeObject<SearchInventoryType>(ItemIDResult);

				if (ItemIDResults.inventory_type == null || string.IsNullOrWhiteSpace(ItemIDResults.inventory_type.ToString()))
				{
					await channel.SendMessageAsync($"{context.Message.Author.Mention} Item {String} does not exist please try again");
				}
				else if (ItemIDResults.inventory_type.Count() > 1)
				{
					await channel.SendMessageAsync("Multiple results found see DM");

					channel = await context.Message.Author.CreateDMChannelAsync(); // GetOrCreateDMChannelAsync() CreateDMChannelAsync();

					var tmp = JsonConvert.SerializeObject(ItemIDResults.inventory_type);
					var httpContent = new StringContent(tmp);

					var ItemName = await Program._httpClient.PostAsync($"https://esi.evetech.net/latest/universe/names/?datasource=tranquility", httpContent);

					if (!ItemName.IsSuccessStatusCode)
					{
						await channel.SendMessageAsync($"{context.Message.Author.Mention} ESI Failure ItemName please try again later");
						await Task.CompletedTask;
					}

					var ItemNameResult = await ItemName.Content.ReadAsStringAsync();

					var ItemNameResults = JsonConvert.DeserializeObject<List<SearchName>>(ItemNameResult);

					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
					var builder = new EmbedBuilder()
						.WithColor(new Color(0x00D000))
						.WithAuthor(author =>
						{
							author
								.WithName($"Multiple Items found use * for exact search:")
								.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
						})
						.WithDescription("Example: Hyperion*");
					var count = 0;
					foreach (var inventory_type in ItemIDResults.inventory_type)
					{
						if (count < 25)
						{
							builder.AddField($"{ItemNameResults.FirstOrDefault(x => x.id == inventory_type).name}", "\u200b");
						}
						else
						{
							var embed2 = builder.Build();

							await channel.SendMessageAsync($"", false, embed2).ConfigureAwait(false);

							builder.Fields.Clear();
							count = 0;
						}
						count++;
					}

					var embed = builder.Build();

					await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
				}
				else
				{
					try
					{
						var httpContent = new StringContent($"[ {ItemIDResults.inventory_type[0]} ]");

						var ItemName = await Program._httpClient.PostAsync($"https://esi.evetech.net/latest/universe/names/?datasource=tranquility", httpContent);

						if (!ItemName.IsSuccessStatusCode)
						{
							await channel.SendMessageAsync($"{context.Message.Author.Mention} ESI Failure ItemName please try again later");
							await Task.CompletedTask;
						}

						var ItemNameResult = await ItemName.Content.ReadAsStringAsync();

						var ItemNameResults = JsonConvert.DeserializeObject<List<SearchName>>(ItemNameResult)[0];

						var url = "https://api.evemarketer.com/ec";
						if (system == "")
						{

							var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}");
							var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
							var builder = new EmbedBuilder()
								.WithColor(new Color(0x00D000))
								.WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
								.WithAuthor(author =>
								{
									author
										.WithName($"Item: {ItemNameResults.name}")
										.WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
										.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
								})
								.WithDescription($"Global Prices")
								.AddField("Buy", $"Low: {centralreply.buy.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.buy.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.buy.max:N2}", true)
								.AddField("Sell", $"Low: {centralreply.sell.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.sell.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.sell.max:N2}", true)
								.AddField($"Extra Data", $"\u200b")
								.AddField("Buy", $"5%: {centralreply.buy.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.buy.volume}", true)
								.AddField("Sell", $"5%: {centralreply.sell.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.sell.volume:N0}", true);
							var embed = builder.Build();

							await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
						}
						if (system == "jita")
						{
							var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30000142");
							var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

							var builder = new EmbedBuilder()
								.WithColor(new Color(0x00D000))
								.WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
								.WithAuthor(author =>
								{
									author
										.WithName($"Item: {ItemNameResults.name}")
										.WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
										.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
								})
								.WithDescription($"Prices from Jita")
								.AddField("Buy", $"Low: {centralreply.buy.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.buy.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.buy.max:N2}", true)
								.AddField("Sell", value: $"Low: {centralreply.sell.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.sell.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.sell.max:N2}", true)
								.AddField($"Extra Data", $"\u200b")
								.AddField("Buy", $"5%: {centralreply.buy.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.buy.volume}", true)
								.AddField("Sell", value: $"5%: {centralreply.sell.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.sell.volume:N0}", true);
							var embed = builder.Build();

							await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
						}
						if (system == "amarr")
						{
							var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30002187");
							var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

							var builder = new EmbedBuilder()
								.WithColor(new Color(0x00D000))
								.WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
								.WithAuthor(author =>
								{
									author
										.WithName($"Item: {ItemNameResults.name}")
										.WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
										.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
								})
								.WithDescription($"Prices from Amarr")
								.AddField("Buy", $"Low: {centralreply.buy.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.buy.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.buy.max:N2}", true)
								.AddField("Sell", $"Low: {centralreply.sell.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.sell.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.sell.max:N2}", true)
								.AddField($"Extra Data", $"\u200b")
								.AddField("Buy", $"5%: {centralreply.buy.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.buy.volume}", true)
								.AddField("Sell", $"5%: {centralreply.sell.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.sell.volume:N0}", true);
							var embed = builder.Build();

							await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
						}
						if (system == "rens")
						{
							var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30002510");
							var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

							var builder = new EmbedBuilder()
								.WithColor(new Color(0x00D000))
								.WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
								.WithAuthor(author =>
								{
									author
										.WithName($"Item: {ItemNameResults.name}")
										.WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
										.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
								})
								.WithDescription($"Prices from Rens")
								.AddField("Buy", $"Low: {centralreply.buy.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.buy.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.buy.max:N2}", true)
								.AddField("Sell", $"Low: {centralreply.sell.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.sell.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.sell.max:N2}", true)
								.AddField($"Extra Data", $"\u200b")
								.AddField("Buy", $"5%: {centralreply.buy.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.buy.volume}", true)
								.AddField("Sell", $"5%: {centralreply.sell.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.sell.volume:N0}", true);
							var embed = builder.Build();

							await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
						}
						if (system == "dodixie")
						{
							var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30002659");
							var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

							await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

							var builder = new EmbedBuilder()
								.WithColor(new Color(0x00D000))
								.WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
								.WithAuthor(author =>
								{
									author
										.WithName($"Item: {ItemNameResults.name}")
										.WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
										.WithIconUrl("http://opux.space/imgs/shipexplosion.png");
								})
								.WithDescription($"Prices from Dodixie")
								.AddField("Buy", $"Low: {centralreply.buy.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.buy.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.buy.max:N2}", true)
								.AddField("Sell", $"Low: {centralreply.sell.min:N2}{Environment.NewLine}" +
								$"Avg: {centralreply.sell.avg:N2}{Environment.NewLine}" +
								$"High: {centralreply.sell.max:N2}", true)
								.AddField($"Extra Data", $"\u200b")
								.AddField("Buy", $"5%: {centralreply.buy.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.buy.volume}", true)
								.AddField("Sell", $"5%: {centralreply.sell.fivePercent:N2}{Environment.NewLine}" +
								$"Volume: {centralreply.sell.volume:N0}", true);
							var embed = builder.Build();

							await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
						}
					}
					catch (Exception ex)
					{
						await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "PC", ex.Message, ex));
					}
				}
			}
			catch (Exception ex)
			{
				await channel.SendMessageAsync($"{context.Message.Author.Mention}, ERROR Please inform Discord/Bot Owner");
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "PC", ex.Message, ex));
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
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "EveTime", ex.Message, ex));
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

						var restricted = Convert.ToUInt64(Program.Settings.GetSection("motd")["restricted"]);
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
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "MOTD", ex.Message, ex));
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
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "CheckTopic", "Running Topic Check"));
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
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "MOTDTopic", ex.Message, ex));
			}
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
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "Jabber", ex.Message, ex));
				}
			}


		}

		internal static async void OnMessage(object sender, Matrix.Xmpp.Client.MessageEventArgs e)
		{
			var filtered = false;
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
							filtered = true;
							await channel.SendMessageAsync($"{prepend + Environment.NewLine}From: {e.Message.From.User} {Environment.NewLine} Message: ```{e.Message.Value}```");
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(e.Message.Value) && !filtered)
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
			//if (AppContext.BaseDirectory.Contains("netcoreapp2.0"))
			//{
			//    var directory = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(
			//    Directory.GetParent(AppContext.BaseDirectory).FullName).FullName).FullName).FullName).FullName);
			//}
			//else
			//{
			//    var directory = Path.Combine(AppContext.BaseDirectory);
			//}

			var channel = context.Channel;
			var botid = Program.Client.CurrentUser.Id;
			var MemoryUsed = ByteSize.FromBytes(Process.GetCurrentProcess().WorkingSet64);
			var RunTime = DateTime.Now - Process.GetCurrentProcess().StartTime;
			var Guilds = Program.Client.Guilds.Count;
			var TotalUsers = 0;
			foreach (var guild in Program.Client.Guilds)
			{
				TotalUsers += guild.Users.Count;
			}

			await channel.SendMessageAsync($"{context.User.Mention},{Environment.NewLine}{Environment.NewLine}" +
				$"```Developer: Jimmy06 (In-game Name: Jimmy06){Environment.NewLine}{Environment.NewLine}" +
				$"Bot ID: {botid}{Environment.NewLine}{Environment.NewLine}" +
				$"Run Time: {RunTime.Days} Days {RunTime.Hours} Hours {RunTime.Minutes} Minutes {RunTime.Seconds} Seconds{Environment.NewLine}{Environment.NewLine}" +
				$"Statistics:{Environment.NewLine}" +
				$"Memory Used: {Math.Round(MemoryUsed.LargestWholeNumberBinaryValue, 2)} {MemoryUsed.LargestWholeNumberBinarySymbol}{Environment.NewLine}" +
				$"Total Connected Guilds: {Guilds}{Environment.NewLine}" +
				$"Total Users Seen: {TotalUsers}```");

			await Task.CompletedTask;
		}
		#endregion

		//Char
		#region Char
		internal async static Task Char(ICommandContext context, string x)
		{
			var characterName = x.ToLower();
			var channel = context.Channel;

			SearchApi searchApi = new SearchApi();
			CharacterApi characterapi = new CharacterApi();
			CorporationApi corporationApi = new CorporationApi();
			AllianceApi allianceApi = new AllianceApi();
			KillmailsApi killmailsApi = new KillmailsApi();
			UniverseApi universeApi = new UniverseApi();

			var CharacterIDList = await universeApi.PostUniverseIdsAsync(new List<string> { characterName });
			int? Id = 0;

			GetCharactersCharacterIdOk character = null;
			GetCorporationsCorporationIdOk corporation = null;
			GetAlliancesAllianceIdOk alliance = null;

			int cynoCount = 0;
			int covertCount = 0;

			var message = await channel.SendMessageAsync("Checking for Character this may take some time...");

			if (CharacterIDList.Characters != null)
			{

				try
				{

					foreach (var id in CharacterIDList.Characters)
					{
						var testchar = await characterapi.GetCharactersCharacterIdAsync(id.Id);
						if (testchar.Name.ToLower() == characterName)
						{
							Id = id.Id;
							character = testchar;
							break;
						}
					}

					corporation = await corporationApi.GetCorporationsCorporationIdAsync(character.CorporationId);

					if (character.AllianceId != null && character.AllianceId != 0)
					{
						alliance = await allianceApi.GetAlliancesAllianceIdAsync(character.AllianceId);
					}

					//Get last 200 kill
					List<GetKillmailsKillmailIdKillmailHashOk> Kills = new List<GetKillmailsKillmailIdKillmailHashOk>();

					var responce = await Program._httpClient.GetAsync($"https://zkillboard.com/api/kills/characterID/{Id}/");

					List<ZKillAPI> zkillContent = new List<ZKillAPI>();
					if (responce.IsSuccessStatusCode)
					{
						var result = await responce.Content.ReadAsStringAsync();
						var kills = JsonConvert.DeserializeObject<List<ZKillAPI>>(result);

						foreach (var kill in kills)
						{
							Kills.Add(await killmailsApi.GetKillmailsKillmailIdKillmailHashAsync($"{kill.package.zkb.hash}", kill.package.killID));
							break;
						}
					}

					//Get last 200 losses
					List<GetKillmailsKillmailIdKillmailHashOk> Losses = new List<GetKillmailsKillmailIdKillmailHashOk>();

					responce = await Program._httpClient.GetAsync($"https://zkillboard.com/api/losses/characterID/{Id}/");

					List<ZKillAPI> zkillLosses = new List<ZKillAPI>();
					if (responce.IsSuccessStatusCode)
					{
						var result = await responce.Content.ReadAsStringAsync();
						var losses = JsonConvert.DeserializeObject<List<ZKillAPI>>(result);
						foreach (var loss in losses)
						{
							Losses.Add(await killmailsApi.GetKillmailsKillmailIdKillmailHashAsync($"{loss.package.zkb.hash}", loss.package.killID));
							break;
						}
					}

					responce = await Program._httpClient.GetAsync($"https://zkillboard.com/api/stats/characterID/{Id}/");

					CharacterStats characterStats = new CharacterStats();

					if (responce.IsSuccessStatusCode)
					{
						var result = await responce.Content.ReadAsStringAsync();
						characterStats = JsonConvert.DeserializeObject<CharacterStats>(result);
					}

					if (Kills.Count != 0)
					{

					}

					foreach (var loss in Losses)
					{
						if (loss.Victim.CharacterId == Id)
						{
							foreach (var item in loss.Victim.Items)
							{
								if (item.ItemTypeId == 21096)
									cynoCount++;
								if (item.ItemTypeId == 28646)
									covertCount++;
							}
						}
					}

					var lastKill = Kills.Count() > 0 ? Kills.FirstOrDefault() : null;
					var lastLoss = Losses.Count() > 0 ? Losses.FirstOrDefault() : null;

					GetKillmailsKillmailIdKillmailHashOk last = null;

					if (lastKill != null && lastLoss != null)
					{
						last = lastKill.KillmailTime > lastLoss.KillmailTime ? lastKill : lastLoss;
					}

					var lastShipType = "Unknown";

					try
					{
						if (last != null && last.Victim != null && last.Victim.CharacterId == Id)
						{
							lastShipType = (await universeApi.GetUniverseTypesTypeIdAsync(last.Victim.ShipTypeId)).Name;
						}
						else if (last != null && last.Victim != null)
						{
							foreach (var attacker in last.Attackers)
							{
								if (attacker.CharacterId == Id)
								{
									lastShipType = (await universeApi.GetUniverseTypesTypeIdAsync(attacker.ShipTypeId)).Name;
								}
							}
						}
					}
					catch { }

					var lastSeenSystem = Kills.Count > 0 ? (await universeApi.GetUniverseSystemsSystemIdAsync(Kills.FirstOrDefault().SolarSystemId)).Name : "Unknown";
					var lastSeenTime = Kills.Count > 0 ? Kills.FirstOrDefault().KillmailTime.ToString() : "Unknown";

					var dangerous = characterStats.dangerRatio > 75 ? "Dangerous" : "Snuggly";
					var gang = characterStats.gangRatio > 70 ? "chance they are Fleeted" : "chance they are Solo";

					var text1 = characterStats.dangerRatio == 0 ? "Unavailable" : Helpers.GenerateUnicodePercentage(characterStats.dangerRatio);
					var text2 = characterStats.gangRatio == 0 ? "Unavailable" : Helpers.GenerateUnicodePercentage(characterStats.gangRatio);

					var allianceName = alliance == null ? "None" : alliance.Name;

					var builder = new EmbedBuilder()
						.WithDescription($"[zKillboard](https://zkillboard.com/character/{Id}/) / [EVEWho](https://evewho.com/pilot/{HttpUtility.UrlEncode(corporation.Name)})")
						.WithColor(new Color(0x4286F4))
						.WithThumbnailUrl($"https://image.eveonline.com/Character/{Id}_64.jpg")
						.WithAuthor(author =>
						{
							author
								.WithName($"{character.Name}");
						})
						.AddField("Details", $"\u200b")
						.AddField("Corporation:", $"{corporation.Name}", true)
						.AddField("Alliance:", $"{allianceName}", true)
						.AddField("Last Seen Location:", $"{lastSeenSystem}", true)
						.AddField("Last Seen Ship:", $"{lastShipType}", true)
						.AddField("Last Seen:", $"{lastSeenTime}", true)
						.AddField("\u200b", "\u200b")
						//.AddInlineField("Regular Cynos(Last 200 losses)", $"{cynoCount}")
						//.AddInlineField("Covert Cynos(Last 200 losses)", $"{covertCount}")
						.AddField("Threat", $"{text1}{Environment.NewLine}{Environment.NewLine}**{characterStats.dangerRatio}% {dangerous}**", true)
						.AddField("Chance in Fleet", $"{text2}{Environment.NewLine}{Environment.NewLine}**{characterStats.gangRatio}% {gang}**", true);


					var embed = builder.Build();

					await message.ModifyAsync(msg => { msg.Content = ""; msg.Embed = embed; });

					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Char", $"Sending {context.Message.Author} Character Info Request to {context.Message.Channel}" +
						$" {context.Guild.Name}"));

				}
				catch
				{
					await message.ModifyAsync(msg => { msg.Content = "Error during character lookup"; });
				}

				await Task.CompletedTask;
			}
			else
			{
				await message.ModifyAsync(msg => { msg.Content = "Character not found. Please use Exact Character Name"; });
			}
		}
		#endregion

		//Corp
		#region Corp
		internal async static Task Corp(ICommandContext context, string x)
		{
			var channel = context.Channel;
			var responce = await Program._httpClient.GetAsync(
				$"https://esi.evetech.net/latest/search/?categories=corporation&datasource=tranquility&language=en-us&search={x}&strict=true");
			CorpIDLookup corpContent = new CorpIDLookup();
			if (!responce.IsSuccessStatusCode)
			{
				await channel.SendMessageAsync($"{context.User.Mention}, {x} Corporation was not found");
			}
			else
			{
				var corpIDLookup = JsonConvert.DeserializeObject<CorpIDLookup>(await responce.Content.ReadAsStringAsync());
				if (corpIDLookup.corporation != null)
				{
					responce = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/corporations/{corpIDLookup.corporation[0]}/?datasource=tranquility");

					CorporationData corporationData = new CorporationData();
					if (responce.IsSuccessStatusCode)
					{
						corporationData = JsonConvert.DeserializeObject<CorporationData>(await responce.Content.ReadAsStringAsync());
					}
					else if (!responce.IsSuccessStatusCode)
					{
						await channel.SendMessageAsync($"{context.User.Mention}, Co");
					}

					var allianceData = new AllianceData();

					try
					{
						if (corporationData.alliance_id != null)
						{
							responce = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/alliances/{corporationData.alliance_id}/?datasource=tranquility");
							if (responce.IsSuccessStatusCode)
							{
								allianceData = JsonConvert.DeserializeObject<AllianceData>(await responce.Content.ReadAsStringAsync());
							}
						}
					}
					catch (HttpRequestException ex)
					{
						await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "corp", ex.Message, ex));
					}

					responce = await Program._httpClient.GetAsync($"https://esi.evetech.net/latest/characters/{corporationData.ceo_id}/?datasource=tranquility");

					var CEONameContent = JsonConvert.DeserializeObject<CharacterData>(await responce.Content.ReadAsStringAsync());

					var alliance = allianceData.name ?? "None";

					var builder = new EmbedBuilder()
						.WithDescription($"")
						.WithColor(new Color(0x4286F4))
						.WithThumbnailUrl($"https://image.eveonline.com/Corporation/{corpIDLookup.corporation[0]}_64.png")
						.WithAuthor(author =>
						{
							author
								.WithName($"{corporationData.name}");
						})
						.AddField("Details", $"\u200b")
						.AddField("Corporation:", $"{corporationData.name}", true)
						.AddField("Alliance:", $"{alliance}", true)
						.AddField("CEO", $"{CEONameContent.name}", true)
						.AddField("Members", $"{corporationData.member_count}", true);

					var embed = builder.Build();


					await channel.SendMessageAsync($"", false, embed);
					await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "Corp", $"Sending {context.Message.Author} Corporation Info Request to {context.Message.Channel}" +
						$"{context.Guild.Name}"));
				}
				else
				{
					await channel.SendMessageAsync($"{context.User.Mention}, {x} Corporation was not found");
				}
			}
		}
		#endregion

		//Discord Stuff
		#region Discord Modules
		internal static async Task InstallCommands()
		{
			Program.Client.MessageReceived += HandleCommand;
			await Program.Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
		}

		internal static async Task HandleCommand(SocketMessage messageParam)
		{
			if (!(messageParam is SocketUserMessage message)) return;

			int argPos = 0;

			if (!(message.HasCharPrefix(Program.Settings.GetSection("config")["commandprefix"].ToCharArray()[0], ref argPos) || message.HasMentionPrefix
					(Program.Client.CurrentUser, ref argPos))) return;

			var context = new CommandContext(Program.Client, message);

			await Program.Commands.ExecuteAsync(context, argPos, Program.ServiceCollection);
		}
		#endregion

		//Complete
		#region MysqlQuery
		internal static async Task<IList<IDictionary<string, object>>> MysqlQuery(string connstring, string query)
		{
			using MySqlConnection conn = new MySqlConnection(connstring);
			using MySqlCommand cmd = conn.CreateCommand();
			List<IDictionary<string, object>> list = new List<IDictionary<string, object>>(); ;
			cmd.CommandText = query;
			try
			{
				var mysqlConfig = Program.Settings.GetSection("mysqlConfig");

				conn.ConnectionString = $"datasource={mysqlConfig["hostname"]};port={mysqlConfig["port"]};" +
					$"username={mysqlConfig["username"]};password={mysqlConfig["password"]};database={mysqlConfig["database"]};SslMode=none;";
				conn.Open();
				using MySqlDataReader reader = cmd.ExecuteReader();
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
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "mySQL", query + " " + ex.Message, ex));
			}
			await Task.Yield();
			return list;
		}
		#endregion

		//SQLite Query
		#region SQLiteQuery
		internal async static Task<string> SQLiteDataQuery(string table, string field, string name)
		{
			using SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};");
			using SqliteCommand querySQL = new SqliteCommand($"SELECT {field} FROM {table} WHERE name = @name", con);
			await con.OpenAsync();
			querySQL.Parameters.Add(new SqliteParameter("@name", name));
			try
			{
				using SqliteDataReader r = await querySQL.ExecuteReaderAsync();
				var result = await r.ReadAsync();
				return r.GetString(0) ?? "";
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
				return null;
			}
		}
		internal async static Task<List<int>> SQLiteDataQuery(string table)
		{
			using SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};");
			using SqliteCommand querySQL = new SqliteCommand($"SELECT * FROM {table}", con);
			await con.OpenAsync();
			try
			{
				using SqliteDataReader r = await querySQL.ExecuteReaderAsync();
				var list = new List<int>();
				while (await r.ReadAsync())
				{
					list.Add(Convert.ToInt32(r["Id"]));
				}

				return list;
			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
				return null;
			}
		}
		#endregion

		//SQLite Update
		#region SQLiteUpdate
		internal async static Task SQLiteDataUpdate(string table, string field, string name, string data)
		{
			using SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};");
			using SqliteCommand insertSQL = new SqliteCommand($"UPDATE {table} SET {field} = @data WHERE name = @name", con);
			await con.OpenAsync();
			insertSQL.Parameters.Add(new SqliteParameter("@name", name));
			insertSQL.Parameters.Add(new SqliteParameter("@data", data));
			try
			{
				insertSQL.ExecuteNonQuery();

			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
			}
		}
		#endregion

		//SQLite Delete
		#region SQLiteDelete
		internal async static Task SQLiteDataDelete(string table, string name)
		{
			using SqliteConnection con = new SqliteConnection($"Data Source = {Path.Combine(Program.ApplicationBase, "Opux.db")};");
			using SqliteCommand insertSQL = new SqliteCommand($"REMOVE FROM {table} WHERE name = @name", con);
			await con.OpenAsync();
			insertSQL.Parameters.Add(new SqliteParameter("@name", name));
			try
			{
				insertSQL.ExecuteNonQuery();

			}
			catch (Exception ex)
			{
				await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
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
