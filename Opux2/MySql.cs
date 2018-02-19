using Discord;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Opux2
{
    public class mySql
    {
        public static async Task<IList<IDictionary<string, object>>> MysqlQuery(string query)
        {
            using (MySqlConnection conn = new MySqlConnection())
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                List<IDictionary<string, object>> list = new List<IDictionary<string, object>>(); ;
                cmd.CommandText = query;
                try
                {
                    if (Base.MySqlAvaliable)
                    {
                        var MySqlConfig = Base.Configuration.GetSection("MySqlConfig");

                        conn.ConnectionString = $"datasource={MySqlConfig["hostname"]};port={MySqlConfig["port"]};" +
                            $"username={MySqlConfig["username"]};password={MySqlConfig["password"]};database={MySqlConfig["database"]};";
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
                }
                catch (MySqlException ex)
                {
                    if (Base.MySqlAvaliable)
                    {
                        await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "mySQL", query + " " + ex.Message, ex));
                    }
                }
                return null;
            }
        }

        internal static async Task MySqlCheck()
        {
            using (MySqlConnection conn = new MySqlConnection())
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                try
                {
                    var MySqlConfig = Base.Configuration.GetSection("MySqlConfig");

                    conn.ConnectionString = $"datasource={MySqlConfig["hostname"]};port={MySqlConfig["port"]};" +
                            $"username={MySqlConfig["username"]};password={MySqlConfig["password"]};database={MySqlConfig["database"]};";
                    await conn.OpenAsync();
                    await conn.CloseAsync();
                    await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "MySqlHelpers", "MySql Query Enabled"));
                    Base.MySqlAvaliable = true;
                }
                catch (MySqlException ex)
                {
                    await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, "MySqlHelpers", "MySql Query Disabled check your MySql Config", ex));
                }
            }

        }
    }
}
