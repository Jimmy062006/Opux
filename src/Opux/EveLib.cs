using JSONStuff;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Opux;
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

        public async Task<bool> SetApiKey(string keyid, string vcode, string characterid)
        {
            KeyID = keyid;
            VCode = vcode;
            CharacterID = characterid;
            await Task.CompletedTask;
            return true;
        }

        public async Task<Dictionary<int, JToken>> GetNotifications()
        {
            using (HttpClient webRequest = new HttpClient())
            {
                var document = new XmlDocument();
                var dictonary = new Dictionary<int, JToken>();

                var xml = await webRequest.GetStreamAsync($"https://api.eveonline.com/char/Notifications.xml.aspx?keyID={KeyID}&vCode={VCode}&characterID={CharacterID}");
                var xmlReader = XmlReader.Create(xml, new XmlReaderSettings { Async = true });
                var complete = await xmlReader.ReadAsync();
                var result = new JObject();
                if (complete)
                {
                    document.Load(xmlReader);
                    result = JObject.Parse(JSON.XmlToJSON(document));
                }

                IDictionary<string, JToken> rowList = (JObject)result["eveapi"]["result"]["rowset"];

                foreach (var r in rowList["row"])
                {
                    dictonary.Add((int) r["notificationID"], r);
                }
                //var listlistlist = (JArray)listlist["row"];

                return dictonary;
            }
        }

        public async Task<Dictionary<int, YamlNode>> GetNotificationText(List<int> notificationID)
        {
            var document = new XmlDocument();
            var dictionary = new Dictionary<int, YamlNode>();

            using (HttpClient webRequest = new HttpClient())
            {
                try
                {
                    var commaseperated = string.Join(",", notificationID);

                    var xml2 = await webRequest.GetStreamAsync($"https://api.eveonline.com/char/NotificationTexts.xml.aspx?keyID={KeyID}&vCode={VCode}&characterID={CharacterID}&IDs={commaseperated}");
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
                    foreach (var r in rowlist)
                    {
                        var value = r["#cdata-section"].ToString();
                        // Setup the input
                        var input = new StringReader(value);

                        // Load the stream
                        var yaml = new YamlStream();
                        yaml.Load(input);

                        dictionary.Add((int)r["notificationID"], yaml.Documents[0].RootNode);

                    }

                    return dictionary;


                    //// Examine the stream
                    //var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                    //foreach (var entry in mapping.Children)
                    //{
                    //    Console.WriteLine(((YamlScalarNode)entry.Key).Value);
                    //}

                    //// List all the items
                    //var items = (YamlSequenceNode)mapping.Children[new YamlScalarNode("items")];
                    //foreach (YamlMappingNode item in items)
                    //{
                    //    Console.WriteLine(
                    //        "{0}\t{1}",
                    //        item.Children[new YamlScalarNode("part_no")],
                    //        item.Children[new YamlScalarNode("descrip")]
                    //    );
                    //}
                }
                catch (Exception ex)
                {
                    await Functions.Client_Log(new Discord.LogMessage(Discord.LogSeverity.Error, "EveLib", ex.Message, ex));
                }
                return null;
            }
        }

        public async Task<Dictionary<Int64, string>> IDtoName(List<Int64> ids)
        {
            using (HttpClient webRequest = new HttpClient())
            {
                var commaseperated = string.Join(",", ids);
                var document = new XmlDocument();
                var dictonary = new Dictionary<Int64, string>();

                var xml = await webRequest.GetStreamAsync($"https://api.eveonline.com//eve/CharacterName.xml.aspx?ids={commaseperated}");
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
        }

        public async Task<Dictionary<Int64, string>> IDtoTypeName(List<Int64> ids)
        {
            using (HttpClient webRequest = new HttpClient())
            {
                var commaseperated = string.Join(",", ids);
                var document = new XmlDocument();
                var dictonary = new Dictionary<Int64, string>();

                var xml = await webRequest.GetStreamAsync($"https://api.eveonline.com//eve/TypeName.xml.aspx?ids={commaseperated}");
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
        }
    }
}
