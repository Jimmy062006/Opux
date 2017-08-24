using System;
using System.Collections.Generic;
using System.Text;

namespace Opux
{
    class JsonClasses
    {
        public class CharacterID
        {
            public int[] character { get; set; }
        }

        public class CharacterData
        {
            public int corporation_id { get; set; }
            public DateTime birthday { get; set; }
            public string name { get; set; }
            public string gender { get; set; }
            public int race_id { get; set; }
            public int bloodline_id { get; set; }
            public string description { get; set; }
            public int alliance_id { get; set; }
            public int ancestry_id { get; set; }
            public float security_status { get; set; }
        }

        public class CorporationData
        {
            public string corporation_name { get; set; }
            public string ticker { get; set; }
            public int member_count { get; set; }
            public int ceo_id { get; set; }
            public string corporation_description { get; set; }
            public float tax_rate { get; set; }
            public int creator_id { get; set; }
            public string url { get; set; }
            public int alliance_id { get; set; }
            public DateTime creation_date { get; set; }
        }

        public class ZKill
        {
            public Kill[] kill { get; set; }
        }

        public class Kill
        {
            public int killID { get; set; }
            public int solarSystemID { get; set; }
            public string killTime { get; set; }
            public int moonID { get; set; }
            public Victim victim { get; set; }
            public Attacker[] attackers { get; set; }
            public Item[] items { get; set; }
            public PositionData position { get; set; }
            public Zkb zkb { get; set; }
        }

        public class Victim
        {
            public int shipTypeID { get; set; }
            public int characterID { get; set; }
            public string characterName { get; set; }
            public int corporationID { get; set; }
            public string corporationName { get; set; }
            public int allianceID { get; set; }
            public string allianceName { get; set; }
            public int factionID { get; set; }
            public string factionName { get; set; }
            public int damageTaken { get; set; }
        }

        public class Zkb
        {
            public int locationID { get; set; }
            public string hash { get; set; }
            public float fittedValue { get; set; }
            public float totalValue { get; set; }
            public int points { get; set; }
            public bool npc { get; set; }
        }

        public class Attacker
        {
            public int characterID { get; set; }
            public string characterName { get; set; }
            public int corporationID { get; set; }
            public string corporationName { get; set; }
            public int allianceID { get; set; }
            public string allianceName { get; set; }
            public int factionID { get; set; }
            public string factionName { get; set; }
            public float securityStatus { get; set; }
            public int damageDone { get; set; }
            public int finalBlow { get; set; }
            public int weaponTypeID { get; set; }
            public int shipTypeID { get; set; }
        }

        public class Item
        {
            public int typeID { get; set; }
            public int flag { get; set; }
            public int qtyDropped { get; set; }
            public int qtyDestroyed { get; set; }
            public int singleton { get; set; }
            public Item1[] items { get; set; }
        }

        public class Item1
        {
            public int typeID { get; set; }
            public int flag { get; set; }
            public int qtyDropped { get; set; }
            public int qtyDestroyed { get; set; }
            public int singleton { get; set; }
        }


        public class Ship
        {
            public int type_id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public bool published { get; set; }
            public int group_id { get; set; }
            public float radius { get; set; }
            public float volume { get; set; }
            public float capacity { get; set; }
            public int portion_size { get; set; }
            public float mass { get; set; }
            public int graphic_id { get; set; }
            public Dogma_Attributes[] dogma_attributes { get; set; }
            public Dogma_Effects[] dogma_effects { get; set; }
        }

        public class Dogma_Attributes
        {
            public int attribute_id { get; set; }
            public float value { get; set; }
        }

        public class Dogma_Effects
        {
            public int effect_id { get; set; }
            public bool is_default { get; set; }
        }


        public class AllianceData
        {
            public string alliance_name { get; set; }
            public string ticker { get; set; }
            public DateTime date_founded { get; set; }
            public int executor_corp { get; set; }
        }

        public class SystemData
        {
            public int system_id { get; set; }
            public string name { get; set; }
            public PositionData position { get; set; }
            public float security_status { get; set; }
            public int constellation_id { get; set; }
            public PlanetData[] planets { get; set; }
            public int[] stargates { get; set; }
            public string security_class { get; set; }
        }

        public class PositionData
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }

        public class PlanetData
        {
            public int planet_id { get; set; }
            public int[] moons { get; set; }
        }
    }
}
