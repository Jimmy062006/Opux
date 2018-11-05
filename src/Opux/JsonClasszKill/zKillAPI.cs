namespace JsonClasszAPIKill
{
    public class ZKillAPI
    {
        public int killmail_id { get; set; }
        public Zkb zkb { get; set; }
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
    }

}
