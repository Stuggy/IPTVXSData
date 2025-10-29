namespace IPTV.data
{
	public class XSChannels
	{
		public long ID { get; set; }
		public long ServerID { get; set; }
		public string? name { get; set; }
		public string? stream_type { get; set; }
		public int stream_id { get; set; }
		public string? stream_icon { get; set; }
		public string? epg_channel_id { get; set; }
		public int added { get; set; }
		public int category_id { get; set; }
		public string? custom_sid { get; set; }
		public int tv_archive { get; set; }
		public string? direct_source { get; set; }
		public int tv_archive_duration { get; set; }

	}
}
