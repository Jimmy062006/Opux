using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Opux
{
    internal class Functions
    {
        internal static DateTime lastAuthCheck = DateTime.Now;
        internal static DateTime lastFeedCheck = DateTime.Now;
        internal static DateTime lastNotificationCheck = DateTime.Now.AddMinutes(-30);
        internal static int lastNotification;
        internal static bool avaliable = false;
        internal static bool running = false;

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

        internal static Task Event_Disconnected(Exception arg)
        {
            return Task.CompletedTask;
        }

        internal static Task Event_Connected()
        {
            return Task.CompletedTask;
        }

        internal static Task Event_LoggedIn()
        {
            return Task.CompletedTask;
        }

        internal static Task Event_LoggedOut()
        {
            return Task.CompletedTask;
        }
        #endregion

        //Needs Corp and Standings added
        #region AuthCheck
        internal async static Task AuthCheck(CommandContext Context)
        {
            //Check inactive users are correct
            if (DateTime.Now > lastAuthCheck.AddMilliseconds(Convert.ToInt32(Program.Settings.GetSection("config")["authInterval"]) * 1000 * 60) || Context != null)
            {
                try
                {
                    await Client_Log(new LogMessage(LogSeverity.Info, "authCheck", $"Running authCheck @{DateTime.Now}"));
                    //Gather details about corps and alliance's to set roles for
                    var authgroups = Program.Settings.GetSection("auth").GetSection("authgroups").GetChildren().ToList();
                    var corps = new Dictionary<string, string>();
                    var alliance = new Dictionary<string, string>();

                    foreach (var config in authgroups)
                    {
                        var temp = config.GetChildren();

                        var corpID = temp.FirstOrDefault(x => x.Key == "corpID").Value ?? "";
                        var allianceID = temp.FirstOrDefault(x => x.Key == "allianceID").Value ?? "";
                        var corpMemberRole = temp.FirstOrDefault(x => x.Key == "corpMemberRole").Value ?? "";
                        var allianceMemberRole = temp.FirstOrDefault(x => x.Key == "allianceMemberRole").Value ?? "";

                        if (Convert.ToInt32(corpID) != 0)
                        {
                            corps.Add(corpID, corpMemberRole);
                        }
                        else if (Convert.ToInt32(allianceID) != 0)
                        {
                            alliance.Add(allianceID, allianceMemberRole);
                        }

                    }

                    string query = "select * from authUsers";
                    var responce = await Functions.MysqlQuery(Program.Settings.GetSection("config")["connstring"], query);
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
                            characterDetails = JObject.Parse(await _characterDetailsContent.ReadAsStringAsync());
                            characterDetails.TryGetValue("corporation_id", out JToken corporationid);
                            using (HttpResponseMessage _corporationDetails = await webclient.GetAsync($"https://esi.tech.ccp.is/latest/corporations/{corporationid}"))
                            using (HttpContent _corporationDetailsContent = _corporationDetails.Content)
                            {
                                corporationDetails = JObject.Parse(await _corporationDetailsContent.ReadAsStringAsync());
                                corporationDetails.TryGetValue("alliance_id", out JToken allianceid);
                                string i = (allianceid.IsNullOrEmpty() ? "0" : allianceid.ToString());
                                allianceID = i;
                                if (allianceID != "0")
                                {
                                    using (HttpResponseMessage _allianceDetails = await webclient.GetAsync($"https://esi.tech.ccp.is/latest/alliances/{allianceid}"))
                                    using (HttpContent _allianceDetailsContent = _allianceDetails.Content)
                                    {
                                        allianceDetails = JObject.Parse(await _allianceDetailsContent.ReadAsStringAsync());
                                    }
                                }
                            }

                            var discordGuild = Program.client.Guilds.FirstOrDefault(X => X.Id == guildID);

                            var discordUser = discordGuild.Users.FirstOrDefault(x => x.Id == Convert.ToUInt64(u["discordID"]));
                            var rolesToAdd = new List<SocketRole>();
                            var rolesToTake = new List<SocketRole>();

                            try
                            {
                                //Check for Corp roles
                                if (corps.ContainsKey(corporationid.ToString()))
                                {

                                }
                            }
                            catch
                            {
                                await Client_Log(new LogMessage(LogSeverity.Error, "authCheck", $"Potential ESI Failiure for {u["eveName"]} skipping"));
                                continue;
                            }

                            //Check for Alliance roles
                            if (alliance.ContainsKey(allianceID))
                            {
                                var ainfo = alliance.FirstOrDefault(x => x.Key == allianceID);
                                var channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == logchan);
                                var test = channel;
                                rolesToAdd.Add(discordGuild.Roles.FirstOrDefault(x => x.Name == ainfo.Value));
                                foreach (var r in rolesToAdd)
                                {
                                    if (discordUser.Roles.FirstOrDefault(x => x.Id == r.Id) == null)
                                    {
                                        await channel.SendMessageAsync($"Granting Role {ainfo.Value} to {characterDetails["name"]}");
                                        await discordUser.AddRolesAsync(rolesToAdd);
                                    }
                                }
                            }

                            //Check if roles when should not have any


                            if (!corps.ContainsKey(corporationid.ToString()) && !alliance.ContainsKey(allianceID))
                            {
                                if (discordUser != null)
                                {
                                    rolesToTake.AddRange(discordUser.Roles);
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

                        lastAuthCheck = DateTime.Now;

                        //if (authgroups.FirstOrDefault(x => x.Key == corporationDetails[""].Values))


                        //   await tmp.SendMessageAsync(message);
                    }
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    //await Logger.logError(ex.Message);
                    await Client_Log(new LogMessage(LogSeverity.Error, "authCheck", ex.StackTrace));
                }
            }
            await Task.CompletedTask;
        }
        #endregion

        //Mostly Complete
        #region killFeed
        private static async Task KillFeed(CommandContext Context)
        {
            try
            {
                lastFeedCheck = DateTime.Now;
                Dictionary<string, IEnumerable<IConfigurationSection>> feedGroups = new Dictionary<string, IEnumerable<IConfigurationSection>>();

                UInt64 guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                UInt64 logchan = Convert.ToUInt64(Program.Settings.GetSection("auth")["alertChannel"]);
                var tmp = Program.client.Guilds;
                var discordGuild = tmp.FirstOrDefault(X => X.Id == guildID);
                var redisQID = Program.Settings.GetSection("killFeed")["reDisqID"].ToString();
                ITextChannel channel = null;
                using (HttpClient webclient = new HttpClient())
                using (HttpResponseMessage redisqResponse = await webclient.GetAsync(String.IsNullOrEmpty(redisQID) ? $"https://redisq.zkillboard.com/listen.php" : $"https://redisq.zkillboard.com/listen.php?queueID={redisQID}"))
                using (HttpContent _redisqResponseContent = redisqResponse.Content)
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

                        var iD = killmail["killmail"]["killID_str"];
                        var killTime = killmail["killmail"]["killTime"];
                        var ship = killmail["killmail"]["victim"]["shipType"]["name"];
                        var value = string.Format("{0:n0}", killmail["zkb"]["totalValue"]);
                        var victimCharacter = killmail["killmail"]["victim"]["character"] ?? null;
                        var victimCorp = killmail["killmail"]["victim"]["corporation"];
                        var victimAlliance = killmail["killmail"]["victim"]["alliance"] ?? null;
                        var attackers = killmail["killmail"]["attackers"] ?? null;
                        var sysName = (string)killmail["killmail"]["solarSystem"]["name"];
                        var globalBigKillValue = (double)killmail["zkb"]["totalValue"];

                        var post = false;
                        var globalBigKill = false;
                        var bigKill = false;

                        foreach (var i in Program.Settings.GetSection("killFeed").GetSection("groupsConfig").GetChildren().ToList())
                        {
                            if(Convert.ToInt64(Program.Settings.GetSection("killFeed")["bigKill"]) != 0 &&
                                (double)killmail["zkb"]["totalValue"] >= Convert.ToInt64(Program.Settings.GetSection("killFeed")["bigKill"]))
                            {
                                channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == Convert.ToUInt64(Program.Settings.GetSection("killFeed")["bigKillChannel"]));
                                globalBigKill = true;
                            }
                            else if(Convert.ToInt64(i["bigKill"]) != 0 && (double)killmail["zkb"]["totalValue"] >= Convert.ToInt64(i["bigKill"]))
                            {
                                channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == Convert.ToUInt64(i["bigKillChannel"]));
                                bigKill = true;
                            }
                            else if (Convert.ToInt32(i["allianceID"]) == 0 && Convert.ToInt32(i["corpID"]) == 0)
                            {
                                channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == Convert.ToUInt64(i["channel"]));
                                var minimumValue = Convert.ToInt64(i["minimumValue"]);
                                var totalValue = (double)killmail["zkb"]["totalValue"];
                                if (Convert.ToInt64(i["minimumValue"]) == 0 || minimumValue <= totalValue)
                                    post = true;
                            }
                            else
                            {
                                channel = (ITextChannel)discordGuild.Channels.FirstOrDefault(x => x.Id == Convert.ToUInt64(i["channel"]));
                                if (victimAlliance != null)
                                {
                                    if ((Int32)victimAlliance["id"] == Convert.ToInt32(i["allianceID"]) && Convert.ToBoolean(Program.Settings.GetSection("killFeed")["losses"]) == true ||
                                        (Int32)victimCorp["id"] == Convert.ToInt32(i["corpID"]) && Convert.ToBoolean(Program.Settings.GetSection("killFeed")["losses"]) == true)
                                    {
                                        if (Convert.ToInt64(i["minimumLossValue"]) == 0 || Convert.ToInt64(i["minimumLossValue"]) <= (double)killmail["zkb"]["totalValue"])
                                            post = true;
                                    }
                                }
                                else if ((Int32)victimCorp["id"] == Convert.ToInt32(i["corpID"]) && Convert.ToBoolean(Program.Settings.GetSection("killFeed")["losses"]) == true)
                                {
                                    if (Convert.ToInt64(i["minimumLossValue"]) == 0 || Convert.ToInt64(i["minimumLossValue"]) <= (double)killmail["zkb"]["totalValue"])
                                        post = true;
                                }
                                foreach (var attacker in attackers.ToList())
                                {
                                    if (attacker["alliance"] != null)
                                    {
                                        if ((Int32)attacker["alliance"]["id"] == Convert.ToInt32(i["allianceID"]) ||
                                            (Int32)attacker["corporation"]["id"] == Convert.ToInt32(i["corpID"]))
                                        {
                                            if (Convert.ToInt64(i["minimumValue"]) == 0 || Convert.ToInt64(i["minimumValue"]) <= (double)killmail["zkb"]["totalValue"])
                                                post = true;
                                        }
                                        else if ((Int32)attacker["corporation"]["id"] == Convert.ToInt32(i["corpID"]))
                                        {
                                            if (Convert.ToInt64(i["minimumValue"]) == 0 || Convert.ToInt64(i["minimumValue"]) <= (double)killmail["zkb"]["totalValue"])
                                                post = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (post || bigKill || globalBigKill)
                        {
                            if (victimCharacter == null)// Kill is probably a structure.
                            {
                                if (victimAlliance == null)
                                {
                                    var message = "";
                                    if (bigKill || globalBigKill)
                                        message = $"**Global Big Kill**{Environment.NewLine}";
                                    message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{value}" +
                                        $" [{victimCorp["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
                                        $"https://zkillboard.com/kill/{iD}/";
                                    await channel.SendMessageAsync(message);
                                }
                                else
                                {
                                    var message = "";
                                    if (bigKill || globalBigKill)
                                        message = $"**Big Kill**{Environment.NewLine}";
                                    message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{value}" +
                                        $" {victimCorp["name"]} | [{victimAlliance["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
                                        $"https://zkillboard.com/kill/{iD}/";
                                    await channel.SendMessageAsync(message);

                                }
                            }
                            else if (!victimAlliance.IsNullOrEmpty())
                            {
                                var message = "";
                                if (bigKill || globalBigKill)
                                    message = $"**BIGBILL**{Environment.NewLine}";
                                message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{value}" +
                                    $"** ISK flown by **{victimCharacter["name"]} |**  **[{victimCorp["name"]}] | <{victimAlliance["name"]}>** killed in **{sysName}** {Environment.NewLine} " +
                                    $"https://zkillboard.com/kill/{iD}/";
                                await channel.SendMessageAsync(message);

                            }
                            else
                            {
                                var message = "";
                                if (bigKill || globalBigKill)
                                    message = $"**BIGBILL**{Environment.NewLine}";
                                message += $"{killTime}{Environment.NewLine}{Environment.NewLine}**{ship}** worth **{value}" +
                                    $"** ISK flown by **{victimCharacter["name"]} |** **[{victimCorp["name"]}]** killed in **{sysName}** {Environment.NewLine} " +
                                    $"https://zkillboard.com/kill/{iD}/";
                                await channel.SendMessageAsync(message);

                            }
                            await Client_Log(new LogMessage(LogSeverity.Info, "killFeed", $"POSTING Kill/Loss ID:{killmail["killmail"]["killID"]} Value:{killmail["zkb"]["totalValue"]}"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "killFeed", ex.StackTrace));
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
                if (DateTime.Now > lastNotificationCheck.AddMinutes(30))
                {
                    var guildID = Convert.ToUInt64(Program.Settings.GetSection("config")["guildId"]);
                    var channelD = Convert.ToUInt64(Program.Settings.GetSection("notifications")["channelID"]);
                    var chan = (ITextChannel)Program.client.GetGuild(guildID).GetChannel(channelD);
                    await Program.eveLib.SetApiKey("5899425", "7cnJu6f1zUtDcBqUpJvZ2W3iRICgThJphQU1uyKejapXe5OZYQpXQ1HugtevCwOU", "1561889551");
                    var notifications = await Program.eveLib.GetNotifications();
                    var notificationsSort = notifications.OrderBy(x => x.Key);

                    var notiIDs = new List<int>();

                    foreach (var l in notifications)
                    {
                        notiIDs.Add((int)l.Key);
                    }

                    var notificationsText = await Program.eveLib.GetNotificationText(notiIDs);

                    foreach (var notification in notificationsSort)
                    {
                        if ((int)notification.Value["notificationID"] > lastNotification)
                        {
                            var notificationText = notificationsText.FirstOrDefault(x => x.Key == notification.Key).Value;

                            if ((int)notification.Value["typeID"] == 102)
                            {
                                var aggressorID = (int)notificationText["aggressorID"];
                                var defenderID = (int)notificationText["defenderID"];

                                var stuff = await Program.eveLib.IDtoName(new List<int> { aggressorID, defenderID });
                                var aggressorName = stuff.FirstOrDefault(x => x.Key == aggressorID).Value;
                                var defenderName = stuff.FirstOrDefault(x => x.Key == defenderID).Value;
                                await chan.SendMessageAsync($"War declared by **{aggressorName}** against **{defenderName}**. Fighting begins in roughly 24 hours.");
                            }
                            lastNotification = (int)notification.Value["notificationID"];
                        }
                    }

                    lastNotificationCheck = DateTime.Now;
                    await Task.CompletedTask;
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
        internal async static Task PriceCheck(CommandContext context, string String, string system)
        {
            var NametoId = "https://www.fuzzwork.co.uk/api/typeid.php?typename=";
            {
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
                            var message = ex.Message;
                        }
                    }
                }
            }
        }
        #endregion

        //Discord Stuff
        #region Discord Modules
        internal static async Task InstallCommands()
        {
            Program.client.MessageReceived += HandleCommand;
            await Program.commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        internal static async Task HandleCommand(SocketMessage messageParam)
        {

            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (!(message.HasCharPrefix(Program.Settings.GetSection("config")["commandprefix"].ToCharArray()[0], ref argPos) || message.HasMentionPrefix
                  (Program.client.CurrentUser, ref argPos))) return;

            var context = new CommandContext(Program.client, message);

            var result = await Program.commands.ExecuteAsync(context, argPos, Program.map);
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
                    await Client_Log(new LogMessage(LogSeverity.Error, "mySQL", ex.StackTrace));
                }
                await Task.Yield();
                return list;
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