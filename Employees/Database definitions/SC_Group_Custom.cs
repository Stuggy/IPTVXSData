namespace IPTV.data
{
    public partial class SC_Groups_Custom
    {
        public int ID { get; set; } // changing this to int from long solved the listbox problem
		public string groupName { get; set; }
		public int groupSC_ID { get; set; }
		public int ChanUniqueID { get; set; }
		public int PortalID { get; set; }   // the portal that this group belongs to
    }
}

