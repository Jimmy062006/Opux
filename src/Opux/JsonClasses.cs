using System;

namespace Opux
{
	class JsonClasses
	{
		//ESI Classes
		public class CharacterID
		{
			public int[] character { get; set; }
		}

		public class SearchInventoryType
		{
			public int[] inventory_type { get; set; }
		}


		public class CorpIDLookup
		{
			public int[] corporation { get; set; }
		}

		public class CharacterData
		{
			public string name { get; set; }
			public string description { get; set; }
			public int corporation_id { get; set; }
			public int? alliance_id { get; set; }
			public DateTime birthday { get; set; }
			public string gender { get; set; }
			public int race_id { get; set; }
			public int bloodline_id { get; set; }
			public int? ancestry_id { get; set; }
			public float? security_status { get; set; }
			public int? faction_id { get; set; }
		}

		public class CorporationData
		{
			public string name { get; set; }
			public string ticker { get; set; }
			public int member_count { get; set; }
			public int ceo_id { get; set; }
			public int? alliance_id { get; set; }
			public string description { get; set; }
			public float tax_rate { get; set; }
			public DateTime date_founded { get; set; }
			public int creator_id { get; set; }
			public string url { get; set; }
		}

		public class AllianceData
		{
			public string name { get; set; }
			public string ticker { get; set; }
			public int creator_id { get; set; }
			public int creator_corporation_id { get; set; }
			public int executor_corporation_id { get; set; }
			public DateTime date_founded { get; set; }
			public int faction_id { get; set; }
		}


		public class SystemIDSearch
		{
			public int[] solar_system { get; set; }
		}

		public class SearchName
		{
			public int id { get; set; }
			public string name { get; set; }
			public string category { get; set; }
		}


		public class CorporationSearch
		{
			public string name { get; set; }
			public string ticker { get; set; }
			public int member_count { get; set; }
			public int ceo_id { get; set; }
			public float tax_rate { get; set; }
			public int creator_id { get; set; }
			public int? alliance_id { get; set; }
			public string description { get; set; }
			public DateTime date_founded { get; set; }
			public string url { get; set; }
			public int? home_station_id { get; set; }
			public Int64? shares { get; set; }
			public int? faction_id { get; set; }
		}

		public class AllianceSearch
		{
			public string name { get; set; }
			public string ticker { get; set; }
			public int creator_id { get; set; }
			public int creator_corporation_id { get; set; }
			public int executor_corporation_id { get; set; }
			public int? faction_id { get; set; }
			public DateTime date_founded { get; set; }
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
			public Items[] property1 { get; set; }
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
			public Int64 volume { get; set; }
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
			public bool Bid { get; set; }
			public int[] Types { get; set; }
			public object[] Regions { get; set; }
			public object[] Systems { get; set; }
			public int Hours { get; set; }
			public int Minq { get; set; }
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
			public Int64 volume { get; set; }
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
			public int[] system { get; set; }
		}


		public class SystemData
		{
			public int star_id { get; set; }
			public int system_id { get; set; }
			public string name { get; set; }
			public Position position { get; set; }
			public float security_status { get; set; }
			public int constellation_id { get; set; }
			public Planet[] planets { get; set; }
			public string security_class { get; set; }
			public int[] stargates { get; set; }
			public int[] stations { get; set; }
		}

		public class ShipType
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

		//Fleetup

		public class Opperations
		{
			public bool Success { get; set; }
			public string Message { get; set; }
			public DateTime CachedUntilUTC { get; set; }
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
			public string Location { get; set; }
			public int LocationId { get; set; }
			public string Details { get; set; }
			public string Url { get; set; }
			public string Organizer { get; set; }
			public string Category { get; set; }
			public string Group { get; set; }
			public int GroupId { get; set; }
			public Doctrine[] Doctrines { get; set; }
		}

		public class Doctrine
		{
			public int Id { get; set; }
			public string Name { get; set; }
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
			public bool solo { get; set; }
			public bool awox { get; set; }
			public string href { get; set; }
		}


		public class Kill
		{
			public int killmail_id { get; set; }
			public DateTime killmail_time { get; set; }
			public Victim victim { get; set; }
			public Attacker[] attackers { get; set; }
			public int solar_system_id { get; set; }
			public Zkb zkb { get; set; }
		}

		public class CharacterStats
		{
			public int allTimeSum { get; set; }
			public bool calcTrophies { get; set; }
			public int dangerRatio { get; set; }
			public int gangRatio { get; set; }
			public int id { get; set; }
			public long iskDestroyed { get; set; }
			public long iskLost { get; set; }
			public int nextTopRecalc { get; set; }
			public int pointsDestroyed { get; set; }
			public int pointsLost { get; set; }
			public int sequence { get; set; }
			public int shipsDestroyed { get; set; }
			public int shipsLost { get; set; }
			public int soloKills { get; set; }
			public int soloLosses { get; set; }
			public Topalltime[] topAllTime { get; set; }
			public Trophies trophies { get; set; }
			public string type { get; set; }
			public Activepvp activepvp { get; set; }
			public Info info { get; set; }
			public Toplist[] topLists { get; set; }
			public int[] topIskKillIDs { get; set; }
		}

		public class Trophies
		{
			public int levels { get; set; }
			public int max { get; set; }
		}

		public class Activepvp
		{
			public Ships ships { get; set; }
			public Systems systems { get; set; }
			public Regions regions { get; set; }
			public Kills kills { get; set; }
		}

		public class Ships
		{
			public string type { get; set; }
			public int count { get; set; }
		}

		public class Systems
		{
			public string type { get; set; }
			public int count { get; set; }
		}

		public class Regions
		{
			public string type { get; set; }
			public int count { get; set; }
		}

		public class Kills
		{
			public string type { get; set; }
			public int count { get; set; }
		}

		public class Info
		{
			public _1 _1 { get; set; }
			public int allianceID { get; set; }
			public int corporationID { get; set; }
			public int factionID { get; set; }
			public int id { get; set; }
			public int killID { get; set; }
			public Lastapiupdate lastApiUpdate { get; set; }
			public string name { get; set; }
			public float secStatus { get; set; }
			public int skip { get; set; }
			public string type { get; set; }
		}

		public class _1
		{
			public int allianceID { get; set; }
		}

		public class Lastapiupdate
		{
			public int sec { get; set; }
			public int usec { get; set; }
		}

		public class Topalltime
		{
			public string type { get; set; }
			public Datum2[] data { get; set; }
		}

		public class Datum2
		{
			public int kills { get; set; }
			public int characterID { get; set; }
			public string characterName { get; set; }
			public int corporationID { get; set; }
			public string corporationName { get; set; }
			public string cticker { get; set; }
			public int allianceID { get; set; }
			public string allianceName { get; set; }
			public string aticker { get; set; }
			public int factionID { get; set; }
			public string factionName { get; set; }
			public int shipTypeID { get; set; }
			public string shipName { get; set; }
			public string groupID { get; set; }
			public string groupName { get; set; }
			public int solarSystemID { get; set; }
			public string solarSystemName { get; set; }
			public object sunTypeID { get; set; }
			public string solarSystemSecurity { get; set; }
			public string systemColorCode { get; set; }
			public int regionID { get; set; }
			public string regionName { get; set; }
			public string systemClass { get; set; }
			public string systemEffect { get; set; }
		}

		public class Toplist
		{
			public string type { get; set; }
			public string title { get; set; }
			public Value[] values { get; set; }
		}

		public class Value
		{
			public int kills { get; set; }
			public int characterID { get; set; }
			public string characterName { get; set; }
			public int id { get; set; }
			public string typeID { get; set; }
			public string name { get; set; }
			public int corporationID { get; set; }
			public string corporationName { get; set; }
			public string cticker { get; set; }
			public int allianceID { get; set; }
			public string allianceName { get; set; }
			public string aticker { get; set; }
			public int shipTypeID { get; set; }
			public string shipName { get; set; }
			public string groupID { get; set; }
			public string groupName { get; set; }
			public int solarSystemID { get; set; }
			public string solarSystemName { get; set; }
			public string sunTypeID { get; set; }
			public string solarSystemSecurity { get; set; }
			public string systemColorCode { get; set; }
			public int regionID { get; set; }
			public string regionName { get; set; }
			public int locationID { get; set; }
			public string itemName { get; set; }
			public string locationName { get; set; }
		}

	}
}