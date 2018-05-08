using System.Collections.Generic;
using System.Linq;
using TentacleSoftware.TeamSpeakQuery;
using TentacleSoftware.TeamSpeakQuery.ServerQueryResult;

namespace Opux
{
    public class ServerGroupInfoResult : ServerQueryBaseResult
    {
        [PropertyMapping("sgid", Required = true)]
        public int Sgid { get; set; }

        [PropertyMapping("name", Required = true)]
        public string Name { get; set; }

        [PropertyMapping("type", Required = true)]
        public string Type { get; set; }

        [PropertyMapping("iconid", Required = true)]
        public string Iconid { get; set; }

        [PropertyMapping("savedb", Required = true)]
        public string Savedb { get; set; }

        [PropertyMapping("sortid", Required = true)]
        public string Sortid { get; set; }

        [PropertyMapping("namemode", Required = true)]
        public string Namemode { get; set; }

        [PropertyMapping("n_modifyp", Required = true)]
        public string N_modifyp { get; set; }

        [PropertyMapping("n_member_addp", Required = true)]
        public string n_member_addp { get; set; }

        [PropertyMapping("n_member_removep", Required = true)]
        public string N_member_removep { get; set; }
    }

    public class ServerGroupListResult : ServerQueryBaseResult
    {
        public List<ServerGroupInfoResult> Values { get; set; }

        public override bool Parse(string message)
        {
            // Is this an error response?
            if (base.Parse(message))
            {
                return true;
            }

            Values = message.ToResultList<ServerGroupInfoResult>();

            if (Values.Any())
            {
                Success = true;
                Response = message;

                return true;
            }

            return false;
        }
    }
}
