namespace IPTV.data
{
	// This is a table of channels and their related XML data for EPG
	public class SC_Channel_Data
	{
		public long ID { get; set; }
		public long channelID { get; set; }
        public string channelName { get; set; }
        public long XMLID { get; set; }				// index in the XMLChannels table
        public string? XMLIDName { get; set; }
        public string? XMLChanName { get; set; }
        public string? XMLTimeZone{ get; set; }
		public int? Genre { get; set; }
        public int? Score { get; set; }				// channel match score to XML name
        public int? PortalID { get; set; }
	}
}
