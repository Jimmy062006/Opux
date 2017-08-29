using System;
using System.Collections.Generic;

namespace Opux
{
    class JsonClasses
    {
        public class CharacterID
        {
            public int[] character { get; set; }
        }

        public class SearchInventoryType
        {
            public int[] inventorytype { get; set; }
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

        //zKill Classes

        public class zKillboardRedisq
        {
            public Package package { get; set; }
        }

        public class Package
        {
            public int killID { get; set; }
            public KillmailRedisq killmail { get; set; }
            public ZkbRedisq zkb { get; set; }
        }

        public class KillmailRedisq
        {
            public Solarsystem solarSystem { get; set; }
            public int killID { get; set; }
            public string killTime { get; set; }
            public AttackerRedisq[] attackers { get; set; }
            public int attackerCount { get; set; }
            public VictimRedisq victim { get; set; }
            public string killID_str { get; set; }
            public string attackerCount_str { get; set; }
            public War war { get; set; }
        }


        public class SolarSystemSearch
        {
            public int[] solarsystem { get; set; }
        }


        public class Solarsystem
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
        }

        public class VictimRedisq
        {
            public AllianceRedisq alliance { get; set; }
            public int damageTaken { get; set; }
            public ItemRedisq[] items { get; set; }
            public string damageTaken_str { get; set; }
            public CharacterRedisq character { get; set; }
            public Shiptype shipType { get; set; }
            public Corporation corporation { get; set; }
            public Position position { get; set; }
        }

        public class AllianceRedisq
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon icon { get; set; }
        }

        public class Icon
        {
            public string href { get; set; }
        }

        public class CharacterRedisq
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon1 icon { get; set; }
        }

        public class Icon1
        {
            public string href { get; set; }
        }

        public class Shiptype
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon2 icon { get; set; }
        }

        public class Icon2
        {
            public string href { get; set; }
        }

        public class Corporation
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon3 icon { get; set; }
        }


        public class CorporationSearch
        {
            public int[] corporation { get; set; }
        }

        public class Icon3
        {
            public string href { get; set; }
        }

        public class Position
        {
            public float y { get; set; }
            public float x { get; set; }
            public float z { get; set; }
        }

        public class ItemRedisq
        {
            public int singleton { get; set; }
            public Itemtype itemType { get; set; }
            public string quantityDestroyed_str { get; set; }
            public int flag { get; set; }
            public string flag_str { get; set; }
            public string singleton_str { get; set; }
            public int quantityDestroyed { get; set; }
            public string quantityDropped_str { get; set; }
            public int quantityDropped { get; set; }
        }

        public class Itemtype
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon4 icon { get; set; }
        }

        public class Icon4
        {
            public string href { get; set; }
        }

        public class War
        {
            public string href { get; set; }
            public int id { get; set; }
            public string id_str { get; set; }
        }

        public class AttackerRedisq
        {
            public Alliance1 alliance { get; set; }
            public Shiptype1 shipType { get; set; }
            public Corporation1 corporation { get; set; }
            public Character1 character { get; set; }
            public string damageDone_str { get; set; }
            public Weapontype weaponType { get; set; }
            public bool finalBlow { get; set; }
            public float securityStatus { get; set; }
            public int damageDone { get; set; }
        }

        public class Alliance1
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon5 icon { get; set; }
        }

        public class Icon5
        {
            public string href { get; set; }
        }

        public class Shiptype1
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon6 icon { get; set; }
        }

        public class Icon6
        {
            public string href { get; set; }
        }

        public class Corporation1
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon7 icon { get; set; }
        }

        public class Icon7
        {
            public string href { get; set; }
        }

        public class Character1
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon8 icon { get; set; }
        }

        public class Icon8
        {
            public string href { get; set; }
        }

        public class Weapontype
        {
            public string id_str { get; set; }
            public string href { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Icon9 icon { get; set; }
        }

        public class Icon9
        {
            public string href { get; set; }
        }

        public class ZkbRedisq
        {
            public int locationID { get; set; }
            public string hash { get; set; }
            public float fittedValue { get; set; }
            public float totalValue { get; set; }
            public int points { get; set; }
            public bool npc { get; set; }
            public string href { get; set; }
        }

        //EVE Central

        public class EveCentralApi
        {
            public Items[] Property1 { get; set; }
        }

        public class Items
        {
            public Buy buy { get; set; }
            public All all { get; set; }
            public Sell sell { get; set; }
        }

        public class Buy
        {
            public Forquery forQuery { get; set; }
            public int volume { get; set; }
            public float wavg { get; set; }
            public float avg { get; set; }
            public float variance { get; set; }
            public float stdDev { get; set; }
            public float median { get; set; }
            public float fivePercent { get; set; }
            public float max { get; set; }
            public float min { get; set; }
            public bool highToLow { get; set; }
            public long generated { get; set; }
        }

        public class Forquery
        {
            public bool bid { get; set; }
            public int[] types { get; set; }
            public object[] regions { get; set; }
            public object[] systems { get; set; }
            public int hours { get; set; }
            public int minq { get; set; }
        }

        public class All
        {
            public Forquery1 forQuery { get; set; }
            public int volume { get; set; }
            public float wavg { get; set; }
            public float avg { get; set; }
            public float variance { get; set; }
            public float stdDev { get; set; }
            public float median { get; set; }
            public float fivePercent { get; set; }
            public float max { get; set; }
            public float min { get; set; }
            public bool highToLow { get; set; }
            public long generated { get; set; }
        }

        public class Forquery1
        {
            public object bid { get; set; }
            public int[] types { get; set; }
            public object[] regions { get; set; }
            public object[] systems { get; set; }
            public int hours { get; set; }
            public int minq { get; set; }
        }

        public class Sell
        {
            public Forquery2 forQuery { get; set; }
            public int volume { get; set; }
            public float wavg { get; set; }
            public float avg { get; set; }
            public float variance { get; set; }
            public float stdDev { get; set; }
            public float median { get; set; }
            public float fivePercent { get; set; }
            public float max { get; set; }
            public float min { get; set; }
            public bool highToLow { get; set; }
            public long generated { get; set; }
        }

        public class Forquery2
        {
            public bool bid { get; set; }
            public int[] types { get; set; }
            public object[] regions { get; set; }
            public object[] systems { get; set; }
            public int hours { get; set; }
            public int minq { get; set; }
        }

        public class SystemList
        {
            public int[] systemList { get; set; }
        }

        //Fleetup

        public class Fleetupapi
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public DateTime CachedUntilUTC { get; set; }
            public string CachedUntilString { get; set; }
            public int Code { get; set; }
            public Datum[] Data { get; set; }
        }

        public class Datum
        {
            public int Id { get; set; }
            public int OperationId { get; set; }
            public string Subject { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string StartString { get; set; }
            public string EndString { get; set; }
            public string Location { get; set; }
            public int LocationId { get; set; }
            public string LocationInfo { get; set; }
            public string Details { get; set; }
            public string Url { get; set; }
            public string Organizer { get; set; }
            public string Category { get; set; }
            public string Group { get; set; }
            public int GroupId { get; set; }
            public object[] Doctrines { get; set; }
        }
    }
}