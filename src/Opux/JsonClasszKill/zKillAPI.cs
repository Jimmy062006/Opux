using System;

namespace JsonClasszAPIKill
{

	public class ZKillAPI
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
		public Attacker[] attackers { get; set; }
		public int killmail_id { get; set; }
		public DateTime killmail_time { get; set; }
		public int solar_system_id { get; set; }
		public Victim victim { get; set; }
	}

	public class Victim
	{
		public int alliance_id { get; set; }
		public int character_id { get; set; }
		public int corporation_id { get; set; }
		public int damage_taken { get; set; }
		public int faction_id { get; set; }
		public Item[] items { get; set; }
		public Position position { get; set; }
		public int ship_type_id { get; set; }
	}

	public class Position
	{
		public float x { get; set; }
		public float y { get; set; }
		public float z { get; set; }
	}

	public class Item
	{
		public int flag { get; set; }
		public int item_type_id { get; set; }
		public int quantity_dropped { get; set; }
		public int singleton { get; set; }
		public int quantity_destroyed { get; set; }
	}

	public class Attacker
	{
		public int alliance_id { get; set; }
		public int character_id { get; set; }
		public int corporation_id { get; set; }
		public int damage_done { get; set; }
		public bool final_blow { get; set; }
		public float security_status { get; set; }
		public int ship_type_id { get; set; }
		public int weapon_type_id { get; set; }
	}

	public class Zkb
	{
		public int locationID { get; set; }
		public string hash { get; set; }
		public float fittedValue { get; set; }
		public float droppedValue { get; set; }
		public float destroyedValue { get; set; }
		public float totalValue { get; set; }
		public int points { get; set; }
		public bool npc { get; set; }
		public bool solo { get; set; }
		public bool awox { get; set; }
		public string[] labels { get; set; }
		public string href { get; set; }
	}

}
