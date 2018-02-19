using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace FleetUp
{
    class FleetUp : ModuleBase, IPlugin
    {
        static IConfiguration Configuration { get; set; }
        static bool MySqlExists { get; set; }
        static DateTime _lastRun { get; set; }
        static bool _Running { get; set; }

        public string Name => "FleetUp";

        public string Description => "FleetUp Intergration";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            if ((await mySql.MysqlQuery("SELECT * FROM FleetUp LIMIT 1")) == null)
            {
                Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "FleetUp", "Creating Fleetup Table in MySql Database")).Wait();

                var result = await mySql.MysqlQuery("CREATE TABLE FleetUp (id int(6) NOT NULL AUTO_INCREMENT, channelid bigint(20) unsigned NOT NULL, " +
                    "guildid bigint(20) unsigned NOT NULL, UserId smallint(6) NOT NULL, APICode text NOT NULL, GroupID mediumint(9) NOT NULL, " +
                    "fleetUpLastPostedOperation mediumint(9) NOT NULL, announce_post tinyint(4) NOT NULL, PRIMARY KEY (id) ) ENGINE=InnoDB DEFAULT CHARSET=latin1");

                if ((await mySql.MysqlQuery("SELECT * FROM FleetUp LIMIT 1")) != null)
                {
                    Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "FleetUp", "Table Created")).Wait();
                    MySqlExists = true;
                    _Running = false;
                }
                else
                {
                    Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "FleetUp", "Error Creating Table")).Wait();
                }
            }
            else
            {
                MySqlExists = true;
                _Running = false;
            }
        }

        public async Task Pulse()
        {
            if (MySqlExists == true && DateTime.UtcNow >= _lastRun.AddSeconds(1) && _Running == false)
            {
                _Running = true;

                var fleetupList = await mySql.MysqlQuery("SELECT * FROM FleetUp");

                foreach (var f in fleetupList)
                {
                    var channelRaw = Convert.ToUInt64("channelid");
                    var guildidRaw = Convert.ToUInt64("guildid");
                    var UserId = Convert.ToUInt32(f["UserId"]);
                    var APICode = Convert.ToString(f["APICode"]);
                    var GroupID = Convert.ToUInt32(f["GroupID"]);
                    var lastopid = Convert.ToUInt32(f["fleetUpLastPostedOperation"]);
                    var announce_post = Convert.ToBoolean(f["announce_post"]);
                    var channel = Base.DiscordClient.GetGuild(guildidRaw).GetTextChannel(channelRaw);


                }

                _Running = false;
            }
            await Task.CompletedTask;
        }
    }
}
