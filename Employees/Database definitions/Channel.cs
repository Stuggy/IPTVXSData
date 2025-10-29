namespace IPTV.data
{
    public partial class Channel
    {
        public long ID { get; set; }
        public int IsRadio { get; set; }
		public int? ChannelNumber { get; set; }
		public int? SubChannelNumber { get; set; }
		public string? Name { get; set; }
		public string? MimeType { get; set; }
		public string? IconPath { get; set; }
		public int? IsHidden { get; set; }
		public int? HasArchive { get; set; }
		public int? Order { get; set; }
		public int? ClientProvidedUniqueID { get; set; }


	}
}

