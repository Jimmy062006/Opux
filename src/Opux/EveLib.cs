using Discord;
using JSONStuff;
using Newtonsoft.Json.Linq;
using Opux;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using YamlDotNet.RepresentationModel;

namespace EveLibCore
{
    public class EveLib
    {

        public EveLib()
        {

        }

        public static string VCode { get; private set; }
        public static string KeyID { get; private set; }
        public static string CharacterID { get; private set; }

        public static string MotdVCode { get; private set; }
        public static string MotdKeyID { get; private set; }
        public static string MotdCharID { get; private set; }

        private static string XMLUrl = "https://api.eveonline.com/";

        public async static Task<bool> SetApiKey(string keyid, string vcode, string characterid)
        {
            try
            {
                KeyID = keyid;
                VCode = vcode;
                CharacterID = characterid;
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "EveLib", $"GetChatChannels failed {ex.Message}", ex));

                return false;
            }
        }

        public async static Task<bool> SetMOTDKey(string keyid, string vcode, string characterid)
        {
            try
            {
                MotdVCode = vcode;
                MotdKeyID = keyid;
                MotdCharID = characterid;
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "EveLib", $"GetChatChannels failed {ex.Message}", ex));

                return false;
            }
        }

        public async static Task<Dictionary<int, JToken>> GetNotifications()
        {
            try
            {
                var document = new XmlDocument();
                var dictionary = new Dictionary<int, JToken>();

                var xml = await Program._httpClient.GetStreamAsync($"{XMLUrl}char/Notifications.xml.aspx?keyID={KeyID}&vCode={VCode}&characterID={CharacterID}");
                var xmlReader = XmlReader.Create(xml, new XmlReaderSettings { Async = true });
                var complete = await xmlReader.ReadAsync();
                var result = new JObject();
                if (complete)
                {
                    document.Load(xmlReader);
                    result = JObject.Parse(JSON.XmlToJSON(document));
                }

                IDictionary<string, JToken> rowList = (JObject)result["eveapi"]["result"]["rowset"];

                if (rowList["row"] == null)
                {

                }
                else if (rowList["row"].Type != JTokenType.Array)
                {
                    dictionary.Add((int)rowList["row"]["notificationID"], rowList["row"]);
                }
                else if (rowList["row"].Type == JTokenType.Array)
                {
                    foreach (var r in rowList["row"])
                    {
                        dictionary.Add((int)r["notificationID"], r);
                    }
                }
                else
                {
                    dictionary.Add((int)result["eveapi"]["result"]["rowset"]["notificationID"], result["eveapi"]["result"]["rowset"]);
                }

                return dictionary;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "EveLib", $"GetChatChannels failed {ex.Message}", ex));

                return null;
            }
        }

        public async static Task<Dictionary<int, YamlNode>> GetNotificationText(List<int> notificationID)
        {
            var document = new XmlDocument();
            var dictionary = new Dictionary<int, YamlNode>();

            try
            {
                if (notificationID.Count > 100)
                    notificationID.RemoveRange(100, notificationID.Count - 100);

                var commaseperated = string.Join(",", notificationID);

                var xml2 = await Program._httpClient.GetStreamAsync($"{XMLUrl}char/NotificationTexts.xml.aspx?keyID={KeyID}&vCode={VCode}&characterID={CharacterID}&IDs={commaseperated}");
                var xmlReader2 = XmlReader.Create(xml2, new XmlReaderSettings { Async = true });
                var complete2 = await xmlReader2.ReadAsync();
                var result = new JObject();
                if (complete2)
                {
                    document.Load(xmlReader2);
                    var tmp = JSON.XmlToJSON(document);
                    result = JObject.Parse(tmp);
                }

                var rowlist = result["eveapi"]["result"]["rowset"]["row"].ToList();
                if (rowlist[0].Children().Count() == 0)
                {
                    var row = result["eveapi"]["result"]["rowset"]["row"];
                    var value = row["#cdata-section"].ToString();
                    var input = new StringReader(value);
                    var yaml = new YamlStream();
                    yaml.Load(input);

                    dictionary.Add((int)row["notificationID"], yaml.Documents[0].RootNode);
                }
                else
                {
                    foreach (var r in rowlist)
                    {
                        try
                        {
                            var value = r["#cdata-section"].ToString();
                            var input = new StringReader(value);
                            var yaml = new YamlStream();
                            yaml.Load(input);

                            dictionary.Add((int)r["notificationID"], yaml.Documents[0].RootNode);
                        }
                        catch { }
                    }
                }

                return dictionary;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "EveLib", ex.Message, ex));

                return null;
            }
        }

        public async static Task<Dictionary<Int64, string>> IDtoName(List<Int64> ids)
        {
            try
            {
                var commaseperated = string.Join(",", ids);
                var document = new XmlDocument();
                var dictonary = new Dictionary<Int64, string>();

                var xml = await Program._httpClient.GetStreamAsync($"{XMLUrl}/eve/CharacterName.xml.aspx?ids={commaseperated}");
                var xmlReader = XmlReader.Create(xml, new XmlReaderSettings { Async = true });
                var complete = await xmlReader.ReadAsync();
                var result = new JObject();
                if (complete)
                {
                    document.Load(xmlReader);
                    result = JObject.Parse(JSON.XmlToJSON(document));
                }
                var test = result["eveapi"]["result"]["rowset"]["row"];
                if (ids.Count() == 1)
                {
                    dictonary.Add((Int64)result["eveapi"]["result"]["rowset"]["row"]["characterID"],
                        (string)result["eveapi"]["result"]["rowset"]["row"]["name"]);
                }
                else if (ids.Count() > 1)
                {
                    foreach (var row in result["eveapi"]["result"]["rowset"]["row"])
                    {
                        dictonary.Add((Int64)row["characterID"], (string)row["name"]);
                    }
                }
                return dictonary;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "EveLib", $"GetChatChannels failed {ex.Message}", ex));

                return null;
            }
        }

        public async static Task<Dictionary<Int64, string>> IDtoTypeName(List<Int64> ids)
        {
            try
            {
                var commaseperated = string.Join(",", ids);
                var document = new XmlDocument();
                var dictonary = new Dictionary<Int64, string>();

                var xml = await Program._httpClient.GetStreamAsync($"{XMLUrl}/eve/TypeName.xml.aspx?ids={commaseperated}");
                var xmlReader = XmlReader.Create(xml, new XmlReaderSettings { Async = true });
                var complete = await xmlReader.ReadAsync();
                var result = new JObject();
                if (complete)
                {
                    document.Load(xmlReader);
                    result = JObject.Parse(JSON.XmlToJSON(document));
                }
                if (ids.Count() == 1)
                {
                    dictonary.Add((Int64)result["eveapi"]["result"]["rowset"]["row"]["typeID"],
                        (string)result["eveapi"]["result"]["rowset"]["row"]["typeName"]);
                }
                else if (ids.Count() > 1)
                {
                    foreach (var row in result["eveapi"]["result"]["rowset"]["row"])
                    {
                        dictonary.Add((Int64)row["typeID"], (string)row["typeName"]);
                    }
                }

                return dictonary;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "EveLib", $"GetChatChannels failed {ex.Message}", ex));

                return null;
            }
        }

        public async static Task<List<JToken>> GetChatChannels()
        {
            try
            {
                var chanName = Program.Settings.GetSection("config")["MOTDChan"];

                var document = new XmlDocument();

                var xml = await Program._httpClient.GetStreamAsync($"{XMLUrl}char/ChatChannels.xml.aspx?" +
                    $"keyID={MotdKeyID}&vCode={MotdVCode}&characterID={MotdCharID}");
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

                return rowlist;
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "EveLib", $"GetChatChannels failed {ex.Message}", ex));

                return null;
            }
        }
    }
}
