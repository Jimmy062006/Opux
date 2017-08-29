using System;

namespace Opux
{
    class JsonClasses
    {
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
            public int Alliance_id { get; set; }
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
            public int Alliance_id { get; set; }
            public DateTime Creation_date { get; set; }
        }

        public class ZKill
        {
            public Kill[] Kill { get; set; }
        }

        public class Kill
        {
            public int KillID { get; set; }
            public int SolarSystemID { get; set; }
            public string KillTime { get; set; }
            public int MoonID { get; set; }
            public Victim Victim { get; set; }
            public Attacker[] Attackers { get; set; }
            public Item[] Items { get; set; }
            public PositionData Position { get; set; }
            public Zkb Zkb { get; set; }
        }

        public class Victim
        {
            public int ShipTypeID { get; set; }
            public int CharacterID { get; set; }
            public string CharacterName { get; set; }
            public int CorporationID { get; set; }
            public string CorporationName { get; set; }
            public int AllianceID { get; set; }
            public string AllianceName { get; set; }
            public int FactionID { get; set; }
            public string FactionName { get; set; }
            public int DamageTaken { get; set; }
        }

        public class Zkb
        {
            public int LocationID { get; set; }
            public string Hash { get; set; }
            public float FittedValue { get; set; }
            public float TotalValue { get; set; }
            public int Points { get; set; }
            public bool Npc { get; set; }
        }

        public class Attacker
        {
            public int CharacterID { get; set; }
            public string CharacterName { get; set; }
            public int CorporationID { get; set; }
            public string CorporationName { get; set; }
            public int AllianceID { get; set; }
            public string AllianceName { get; set; }
            public int FactionID { get; set; }
            public string FactionName { get; set; }
            public float SecurityStatus { get; set; }
            public int DamageDone { get; set; }
            public int FinalBlow { get; set; }
            public int WeaponTypeID { get; set; }
            public int ShipTypeID { get; set; }
        }

        public class Item
        {
            public int TypeID { get; set; }
            public int Flag { get; set; }
            public int QtyDropped { get; set; }
            public int QtyDestroyed { get; set; }
            public int Singleton { get; set; }
            public Item1[] Items { get; set; }
        }

        public class Item1
        {
            public int TypeID { get; set; }
            public int Flag { get; set; }
            public int QtyDropped { get; set; }
            public int QtyDestroyed { get; set; }
            public int Singleton { get; set; }
        }


        public class Ship
        {
            public int Type_id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public bool Published { get; set; }
            public int Group_id { get; set; }
            public float Radius { get; set; }
            public float Volume { get; set; }
            public float Capacity { get; set; }
            public int Portion_size { get; set; }
            public float Mass { get; set; }
            public int Graphic_id { get; set; }
            public Dogma_Attributes[] Dogma_attributes { get; set; }
            public Dogma_Effects[] Dogma_effects { get; set; }
        }

        public class Dogma_Attributes
        {
            public int Attribute_id { get; set; }
            public float Value { get; set; }
        }

        public class Dogma_Effects
        {
            public int Effect_id { get; set; }
            public bool Is_default { get; set; }
        }


        public class AllianceData
        {
            public string Alliance_name { get; set; }
            public string Ticker { get; set; }
            public DateTime Date_founded { get; set; }
            public int Executor_corp { get; set; }
        }

        public class SystemData
        {
            public int System_id { get; set; }
            public string Name { get; set; }
            public PositionData Position { get; set; }
            public float Security_status { get; set; }
            public int Constellation_id { get; set; }
            public PlanetData[] Planets { get; set; }
            public int[] Stargates { get; set; }
            public string Security_class { get; set; }
        }

        public class PositionData
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        public class PlanetData
        {
            public int Planet_id { get; set; }
            public int[] Moons { get; set; }
        }

        //zKill Classes

        public class ZKillboardRedisq
        {
            public Package Package { get; set; }
        }

        public class Package
        {
            public int KillID { get; set; }
            public KillmailRedisq Killmail { get; set; }
            public ZkbRedisq Zkb { get; set; }
        }

        public class KillmailRedisq
        {
            public Solarsystem SolarSystem { get; set; }
            public int KillID { get; set; }
            public string KillTime { get; set; }
            public AttackerRedisq[] Attackers { get; set; }
            public int AttackerCount { get; set; }
            public VictimRedisq Victim { get; set; }
            public string KillID_str { get; set; }
            public string AttackerCount_str { get; set; }
            public War War { get; set; }
        }


        public class SolarSystemSearch
        {
            public int[] Solarsystem { get; set; }
        }


        public class Solarsystem
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class VictimRedisq
        {
            public AllianceRedisq Alliance { get; set; }
            public int DamageTaken { get; set; }
            public ItemRedisq[] Items { get; set; }
            public string DamageTaken_str { get; set; }
            public CharacterRedisq Character { get; set; }
            public Shiptype ShipType { get; set; }
            public Corporation Corporation { get; set; }
            public Position Position { get; set; }
        }

        public class AllianceRedisq
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon Icon { get; set; }
        }

        public class Icon
        {
            public string Href { get; set; }
        }

        public class CharacterRedisq
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon1 Icon { get; set; }
        }

        public class Icon1
        {
            public string Href { get; set; }
        }

        public class Shiptype
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon2 Icon { get; set; }
        }

        public class Icon2
        {
            public string Href { get; set; }
        }

        public class Corporation
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon3 Icon { get; set; }
        }


        public class CorporationSearch
        {
            public int[] Corporation { get; set; }
        }

        public class Icon3
        {
            public string Href { get; set; }
        }

        public class Position
        {
            public float Y { get; set; }
            public float X { get; set; }
            public float Z { get; set; }
        }

        public class ItemRedisq
        {
            public int Singleton { get; set; }
            public Itemtype ItemType { get; set; }
            public string QuantityDestroyed_str { get; set; }
            public int Flag { get; set; }
            public string Flag_str { get; set; }
            public string Singleton_str { get; set; }
            public int QuantityDestroyed { get; set; }
            public string QuantityDropped_str { get; set; }
            public int QuantityDropped { get; set; }
        }

        public class Itemtype
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon4 Icon { get; set; }
        }

        public class Icon4
        {
            public string Href { get; set; }
        }

        public class War
        {
            public string Href { get; set; }
            public int Id { get; set; }
            public string Id_str { get; set; }
        }

        public class AttackerRedisq
        {
            public Alliance1 Alliance { get; set; }
            public Shiptype1 ShipType { get; set; }
            public Corporation1 Corporation { get; set; }
            public Character1 Character { get; set; }
            public string DamageDone_str { get; set; }
            public Weapontype WeaponType { get; set; }
            public bool FinalBlow { get; set; }
            public float SecurityStatus { get; set; }
            public int DamageDone { get; set; }
        }

        public class Alliance1
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon5 Icon { get; set; }
        }

        public class Icon5
        {
            public string Href { get; set; }
        }

        public class Shiptype1
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon6 Icon { get; set; }
        }

        public class Icon6
        {
            public string Href { get; set; }
        }

        public class Corporation1
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon7 Icon { get; set; }
        }

        public class Icon7
        {
            public string Href { get; set; }
        }

        public class Character1
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon8 Icon { get; set; }
        }

        public class Icon8
        {
            public string Href { get; set; }
        }

        public class Weapontype
        {
            public string Id_str { get; set; }
            public string Href { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public Icon9 Icon { get; set; }
        }

        public class Icon9
        {
            public string Href { get; set; }
        }

        public class ZkbRedisq
        {
            public int LocationID { get; set; }
            public string Hash { get; set; }
            public float FittedValue { get; set; }
            public float TotalValue { get; set; }
            public int Points { get; set; }
            public bool Npc { get; set; }
            public string Href { get; set; }
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
    }
}