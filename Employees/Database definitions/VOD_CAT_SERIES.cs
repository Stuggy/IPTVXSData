namespace IPTV.data
{
    public partial class SC_VOD_CAT_SERIES
    {
        public int ID { get; set; } // changing this to int from long solved the listbox problem
		public string SC_ID { get; set; }
		public string Name { get; set; }
		public int Enabled { get; set; }
        public int PortalID { get; set; }   // the portal that this group belongs to
    }
}

