namespace IPTV.data
{
    public partial class SC_EPG
    {
        public int ID { get; set; }
		public int? uniqueBroadcastId { get; set; }
		public string? title { get; set; }
		public int channelNumber { get; set; }
		public int startTime { get; set; }
		public int endTime { get; set; }
		public string? plot { get; set; }
		public int portalID { get; set; }
		public int? year { get; set; }  
		public string? category { get; set; }
		public int? categoryKodi { get; set; }
        public int? episodeNumber { get; set; }
		public string? episodeName { get; set; }
    }
}

