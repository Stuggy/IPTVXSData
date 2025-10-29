namespace IPTV.data
{
	public class XSChannelCategories
	{
		public long ID { get; set; }
		public long ServerID { get; set; }
		public int category_id { get; set; }
		public string? category_name { get; set; }
		public int parent_id { get; set; }
		public int Enabled { get; set; }


	}
}
