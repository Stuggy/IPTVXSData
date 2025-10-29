namespace IPTV.data
{
    public partial class Portal
    {
        public long ID { get; set; }
        public string? MacAddress { get; set; }
        public string? ServerAddress { get; set; }
        public string? TimeZone { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public int? CacheGuideData { get; set; }
        public int? GuideCacheTime { get; set; }
        public string? Token { get; set; }
        public string? SerialNumber { get; set; }
        public string? DeviceID { get; set; }
        public string? DeviceID2 { get; set; }
        public string? Signature { get; set; }
		public string? Comments { get; set; }
		public string? SourceURL { get; set; }
        public int? EPGTimeShift { get; set; }
		public int? Active { get; set; }
		public int? RequiresFreshToken { get; set; }    // some portals require a fresh token on each play
	}
}

