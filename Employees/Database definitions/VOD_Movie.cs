namespace IPTV.data
{
	public partial class SC_VOD_MOVIE
	{
		public int ID { get; set; } 
		public string actors { get; set; }
		public string category_ID { get; set; }
		public string cmd { get; set; }
		public string playurl { get; set; }
		public string description { get; set; }
		public string director { get; set; }
		public string genres_str { get; set; }
		public string vod_ID { get; set; }
		public int is_movie { get; set; }
		public int is_series { get; set; }
		public string name { get; set; }
		public string rating_imdb { get; set; }
		public string screenshot_url { get; set; }
		public string tmdb { get; set; }
		public string tmdb_id { get; set; }
		public string year { get; set; }
		public int portalID { get; set; }   // the portal that this VOD item belongs to
	}
}

