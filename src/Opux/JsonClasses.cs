using System;

namespace Opux
{
    class JsonClasses
    {
        //ESI Classes
        public class CharacterID
        {
            public int[] Character { get; set; }
        }

        public class SearchInventoryType
        {
            public int[] Inventorytype { get; set; }
        }

        public class CharacterData
        {
            public int Corporation_id { get; set; }
            public DateTime Birthday { get; set; }
            public string Name { get; set; }
            public string Gender { get; set; }
            public int Race_id { get; set; }
            public int Bloodline_id { get; set; }
            public string Description { get; set; }
            public int Alliance_id { get; set; } = -1;
            public int Ancestry_id { get; set; }
            public float Security_status { get; set; }
        }

        public class CorporationData
        {
            public string Corporation_name { get; set; }
            public string Ticker { get; set; }
            public int Member_count { get; set; }
            public int Ceo_id { get; set; }
            public string Corporation_description { get; set; }
            public float Tax_rate { get; set; }
            public int Creator_id { get; set; }
            public string Url { get; set; }
            public int Alliance_id { get; set; } = -1;
            public DateTime Creation_date { get; set; }
        }

        public class AllianceData
        {
            public string alliance_name { get; set; }
            public string ticker { get; set; }
            public DateTime date_founded { get; set; }
            public int executor_corp { get; set; }
        }

        public class SystemIDSearch
        {
            public int[] solarsystem { get; set; }
        }

        public class CorporationSearch
        {
            public string corporation_name { get; set; }
            public string ticker { get; set; }
            public int member_count { get; set; }
            public int ceo_id { get; set; }
            public string corporation_description { get; set; }
            public float tax_rate { get; set; }
            public int creator_id { get; set; }
            public string url { get; set; }
            public DateTime creation_date { get; set; }
            public string faction { get; set; }
        }

        public class AllianceSearch
        {
            public string alliance_name { get; set; }
            public string ticker { get; set; }
            public DateTime date_founded { get; set; }
            public int executor_corp { get; set; }
        }

        public class SystemName
        {
            public int star_id { get; set; }
            public int system_id { get; set; }
            public string name { get; set; }
            public Position position { get; set; }
            public float security_status { get; set; }
            public int constellation_id { get; set; }
            public Planet[] planets { get; set; }
            public int[] stargates { get; set; }
            public int[] stations { get; set; }
        }

        public class Position
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }

        public class Planet
        {
            public int planet_id { get; set; }
            public int[] moons { get; set; }
        }

        public class Type_id
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

        //EVE Central

        public class EveCentralApi
        {
            public Items[] Property1 { get; set; }
        }

        public class Items
        {
            public Buy Buy { get; set; }
            public All All { get; set; }
            public Sell Sell { get; set; }
        }

        public class Buy
        {
            public Forquery ForQuery { get; set; }
            public int Volume { get; set; }
            public float Wavg { get; set; }
            public float Avg { get; set; }
            public float Variance { get; set; }
            public float StdDev { get; set; }
            public float Median { get; set; }
            public float FivePercent { get; set; }
            public float Max { get; set; }
            public float Min { get; set; }
            public bool HighToLow { get; set; }
            public long Generated { get; set; }
        }

        public class Forquery
        {
            public bool Bid { get; set; }
            public int[] Types { get; set; }
            public object[] Regions { get; set; }
            public object[] Systems { get; set; }
            public int Hours { get; set; }
            public int Minq { get; set; }
        }

        public class All
        {
            public Forquery1 ForQuery { get; set; }
            public int Volume { get; set; }
            public float Wavg { get; set; }
            public float Avg { get; set; }
            public float Variance { get; set; }
            public float StdDev { get; set; }
            public float Median { get; set; }
            public float FivePercent { get; set; }
            public float Max { get; set; }
            public float Min { get; set; }
            public bool HighToLow { get; set; }
            public long Generated { get; set; }
        }

        public class Forquery1
        {
            public object Bid { get; set; }
            public int[] Types { get; set; }
            public object[] Regions { get; set; }
            public object[] Systems { get; set; }
            public int Hours { get; set; }
            public int Minq { get; set; }
        }

        public class Sell
        {
            public Forquery2 ForQuery { get; set; }
            public int Volume { get; set; }
            public float Wavg { get; set; }
            public float Avg { get; set; }
            public float Variance { get; set; }
            public float StdDev { get; set; }
            public float Median { get; set; }
            public float FivePercent { get; set; }
            public float Max { get; set; }
            public float Min { get; set; }
            public bool HighToLow { get; set; }
            public long Generated { get; set; }
        }

        public class Forquery2
        {
            public bool Bid { get; set; }
            public int[] Types { get; set; }
            public object[] Regions { get; set; }
            public object[] Systems { get; set; }
            public int Hours { get; set; }
            public int Minq { get; set; }
        }

        public class SystemList
        {
            public int[] System { get; set; }
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

        //zKill Classes

        public class ZKillboard
        {
            public Package package { get; set; }
        }

        public class Package
        {
            public int killID { get; set; }
            public Killmail killmail { get; set; }
            public Zkb zkb { get; set; }
        }

        public class Killmail
        {
            public int killmail_id { get; set; }
            public DateTime killmail_time { get; set; }
            public Victim victim { get; set; }
            public Attacker[] attackers { get; set; }
            public int solar_system_id { get; set; }
        }

        public class Victim
        {
            public int damage_taken { get; set; }
            public int ship_type_id { get; set; }
            public int character_id { get; set; }
            public int corporation_id { get; set; }
            public int alliance_id { get; set; }
            public int faction_id { get; set; }
            public Item[] items { get; set; }
            public Position position { get; set; }
        }

        public class Item
        {
            public int item_type_id { get; set; }
            public int singleton { get; set; }
            public int flag { get; set; }
            public int quantity_dropped { get; set; }
            public int quantity_destroyed { get; set; }
        }

        public class Attacker
        {
            public float security_status { get; set; }
            public bool final_blow { get; set; }
            public int damage_done { get; set; }
            public int character_id { get; set; }
            public int corporation_id { get; set; }
            public int alliance_id { get; set; }
            public int ship_type_id { get; set; }
            public int weapon_type_id { get; set; }
        }

        public class Zkb
        {
            public int locationID { get; set; }
            public string hash { get; set; }
            public float fittedValue { get; set; }
            public float totalValue { get; set; }
            public int points { get; set; }
            public bool npc { get; set; }
            public string href { get; set; }
        }

        public class Kill
        {
            public Class1[] Property1 { get; set; }
        }

        public class Class1
        {
            public int killmail_id { get; set; }
            public DateTime killmail_time { get; set; }
            public Victim victim { get; set; }
            public Attacker[] attackers { get; set; }
            public int solar_system_id { get; set; }
            public Zkb zkb { get; set; }
        }

        //public class Victim
        //{
        //    public int damage_taken { get; set; }
        //    public int ship_type_id { get; set; }
        //    public int character_id { get; set; }
        //    public int corporation_id { get; set; }
        //    public int faction_id { get; set; }
        //    public Item[] items { get; set; }
        //    public Position position { get; set; }
        //    public int alliance_id { get; set; }
        //}

        //public class Position
        //{
        //    public float x { get; set; }
        //    public float y { get; set; }
        //    public float z { get; set; }
        //}

        //public class Item
        //{
        //    public int item_type_id { get; set; }
        //    public int singleton { get; set; }
        //    public int flag { get; set; }
        //    public int quantity_destroyed { get; set; }
        //    public int quantity_dropped { get; set; }
        //}

        //public class Zkb
        //{
        //    public int locationID { get; set; }
        //    public string hash { get; set; }
        //    public float fittedValue { get; set; }
        //    public float totalValue { get; set; }
        //    public int points { get; set; }
        //}

        //public class Attacker
        //{
        //    public float security_status { get; set; }
        //    public bool final_blow { get; set; }
        //    public int damage_done { get; set; }
        //    public int character_id { get; set; }
        //    public int corporation_id { get; set; }
        //    public int ship_type_id { get; set; }
        //    public int weapon_type_id { get; set; }
        //    public int faction_id { get; set; }
        //    public int alliance_id { get; set; }
        //}

    }
}