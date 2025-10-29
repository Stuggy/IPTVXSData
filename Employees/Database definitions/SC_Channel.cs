namespace IPTV.data
{
    public partial class SC_Channel
    {
        public long ID { get; set; }
        public string? uniqueID { get; set; }
        public string? number { get; set; }
        public string? name { get; set; }
        public string? streamURL { get; set; }
        public string? iconPath { get; set; }
        public int channelID { get; set; }
        public string? cmd { get; set; }
        public string? tvGenreID { get; set; }  // this is used as the group identifier
        public int? useHttpTempLink { get; set; }
        public int? useLoadBalancing { get; set; }
        public int? portalID { get; set; }
		public int? shortEPGFails { get; set; }
	}
}

