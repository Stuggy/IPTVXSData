using FuzzySharp;
using IPTV.data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.ProgressBar;
using Syncfusion.Blazor.Buttons;
using System.Dynamic;
using Serilog;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace IPTVData.Pages
{
	public partial class Fuzzy
	{
		struct XMLFields
		{
			public int XMLID;           // the table ID number
			public string XMLIDName;    // this is for example "Live.360.uk"
		}

		public class XMLMatchInfo
		{
			public string? channelName;
			public string? XMLName;
			public long? channelID;
			public string? XMLIDName;
			public int? score;
			public string? TimeZone;
			public string? XMLSource;
		}
		//private IptvDataContext? _IPTVcontext;

		//General
		public string? ActivePortal;                                    // the active
		SfGrid<ExpandoObject> Grid { get; set; }
		public bool ShowProgress = false;
		//         public double progressValue;
		private double progressValue { get; set; }
		public double progressMax;
		SfProgressBar pBar;
		Utils _utils;
		int unmatchedCount = 0;          // count of channels not matched to an EPG
		bool bFuzzy = false;
		bool bShowUnmatched = false;	// to show or not the list of unmatched

        //Channels
        public List<SC_Channel>? OurChannels { get; set; }
		public List<SC_Channel>? MatchChannels { get; set; }            // channels filtered by the passed in timezone
		public List<SC_Channel>? UnmatchedChannels { get; set; }
		public string? selectedChannelID;                               // channel text to display at the top

		//Channel data - my solution is to save my related data in a separate table so that the channel data can just be reloaded.
		// The portals sometimes change their channel lineup.
		public List<SC_Channel_Data>? ChannelData { get; set; }
		public List<ExpandoObject> XMLObject { get; set; } = new List<ExpandoObject>(); // this is what is displayed on the Razor page
																						//public List<ExpandoObject> XMLObjectEmpty { get; set; } = new List<ExpandoObject>(); // this is what is displayed on the Razor page
		public List<XMLMatchInfo> XMLMatchList = new List<XMLMatchInfo>();

		//XML
		public List<XMLChannel>? OurXML { get; set; }

		//Toasts
		SfToast? ToastObj;
		private string ToastPosition = "Center";

		////////////////////////////////////////////////////////////////////////////////////
		// General
		protected override async Task OnInitializedAsync()
		{
			_utils = new Utils();
			// I need to find out the active portal
			ActivePortal = _utils.getPortalNumber();

			bFuzzy = false;
			await GetChannels("");
			await GetXML("");
			GetUnmatchedChannels();
			ShowMatchesByXML();
		}

		private async void resetSC_Channel_Data()    // try to force refresh - this seems to mostly work
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			_IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_Channel_Data WHERE PortalID = " + ActivePortal);
		}
		//
		////////////////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////////////////////
		//  XML
		public async Task GetXML(string filter) // this filters the XML Names and shows the related channels

		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			IEnumerable<string> LoadedXMLIDs = new List<string>();
			if (OurXML != null)  // this solves the first refresh that doesn't otherwise work
			{
				OurXML.Clear();  // trying to force refresh
			}

			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			IQueryable<XMLChannel> query;   // default empty query

			if (_IPTVcontext is not null)
			{
				if (filter != null && filter != "")     // there is some filter text
				{
					// get the xml channels that match the filter text
					query = from xmlchan in _IPTVcontext.XMLChannels
							where xmlchan.Name.ToUpper().Contains(filter)
							select xmlchan;
					OurXML = query.ToList<XMLChannel>();
				}
				else // no filter text
				{
					query = from xmlchan in _IPTVcontext.XMLChannels
							select xmlchan;
					OurXML = query.ToList<XMLChannel>();
					OurXML = OurXML.OrderBy(x => x.Name).ToList();
				}
			}
			// I have the filtered XML list

		}

		public void ClearList()     // this forces a refresh of the grid
		{
			XMLObject.Clear();
			XMLObject = new List<ExpandoObject>();
		}

		public async Task ShowMatchesByXML()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			GetChannels("");                            // reload because scores might need refreshing
			XMLObject = new List<ExpandoObject>();      // this is the data that is presented in the grid
			XMLMatchList = new List<XMLMatchInfo>();    // reset to help with refresh
			ChannelData = new List<SC_Channel_Data>();  // reset to help with refresh
			IQueryable<SC_Channel_Data> ChanDataQuery;   // default empty query
														 // Now I have the XML or the filtered subset and I need to get the related channel info
														 // out of the SC_Channel_Data which provides matched EPG XML rows to channels
			foreach (var xml in OurXML)
			{
				XMLMatchInfo x = new XMLMatchInfo();
				x.channelName = "";
				x.channelID = 0;
				x.XMLName = xml.Name;   // this isn't the friendly name but it would be better if it was
				x.XMLIDName = xml.IDName;
				x.TimeZone = xml.TimeZone;
				x.XMLSource = xml.Source;
				// Get the related data from SC_Channel_Data if it exists
				ChanDataQuery = from scd in _IPTVcontext.SC_Channel_Data
								where scd.PortalID == int.Parse(@ActivePortal) &&
								scd.XMLID == xml.ID     // this means that a channel has been matched to an EPG XML row
								select scd;
				ChannelData = ChanDataQuery.ToList<SC_Channel_Data>();

				if (ChannelData.Count > 0)  // if this channel has a created link in the SC_Channel_Data
				{
					x.channelName = ChannelData.FirstOrDefault().channelName;
					x.channelID = ChannelData.FirstOrDefault().channelID;
					x.score = ChannelData.FirstOrDefault().Score;
				}
				XMLMatchList.Add(x);
			}
			// reload data
			//var blog2 = _IPTVcontext.SC_Channel_Data.Any();
			//var entityEntry = _IPTVcontext.Entry(blog2);
			//entityEntry.Reload();   // finally this caused the grid data to refresh *******************************************

			XMLObject = Enumerable.Range(0, XMLMatchList.Count).Select((x) =>
			{
				dynamic d = new ExpandoObject();
				d.channelID = XMLMatchList[x].channelID;
				d.Name = XMLMatchList[x].channelName;
				d.XMLName = XMLMatchList[x].XMLName;
				d.XMLIDName = XMLMatchList[x].XMLIDName;
				d.Score = XMLMatchList[x].score;
				d.TimeZone = XMLMatchList[x].TimeZone;
				d.Source = XMLMatchList[x].XMLSource;
				return d;
			}).Cast<ExpandoObject>().ToList<ExpandoObject>();
		}
		// end of XML
		////////////////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////////////////////
		//  SC_Channels
		public async Task GetChannels(string filter)
		{   // This version gets all channels whether selected as active or not (that is done by the groups)
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			filter = filter.ToUpper();      // change to filter to upper case to help with matching

			XMLMatchList.Clear();
			IQueryable<SC_Channel> query;   // default empty query
			if (_IPTVcontext is not null)
			{
				// Filter channels if required
				if (filter != null && filter != "")     // there is some filter text
				{
					// get the channels that match the filter text
					query = from ch in _IPTVcontext.SC_Channels
							where ch.name.ToUpper().Contains(filter) &&
							ch.portalID == int.Parse(@ActivePortal)         // string to int conversion - only channels for the current portal
							select ch;
					OurChannels = query.ToList<SC_Channel>();
				}
				else // no filter text
				{
					query = from ch in _IPTVcontext.SC_Channels
							where ch.portalID == int.Parse(@ActivePortal)
							select ch;
					OurChannels = query.ToList<SC_Channel>();
				}
				// End of filtering channels
				///////////////////////////////////////////////////////////
				// Now I have the channels or the filtered subset and I need to get the related XML info
			}
		}

		public async Task ShowMatchesByChannels()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			GetChannels("");                                // reload because scores might need refreshing
			XMLObject = new List<ExpandoObject>();          // reset to help with refresh
			XMLMatchList = new List<XMLMatchInfo>();        // reset to help with refresh
			ChannelData = new List<SC_Channel_Data>();      // reset to help with refresh
			IQueryable<SC_Channel_Data> ChanDataQuery;      // default empty query
			foreach (var chan in OurChannels)
			{
				XMLMatchInfo x = new XMLMatchInfo();
				x.channelName = chan.name;
				x.channelID = chan.channelID;
				x.XMLName = "";
				x.XMLIDName = "";
				x.TimeZone = "";
				x.XMLSource = "";
				// Get the related data from SC_Channel_Data if it exists
				ChanDataQuery = from scd in _IPTVcontext.SC_Channel_Data
								where scd.PortalID == int.Parse(@ActivePortal) &&
								scd.channelID == chan.channelID
								select scd;
				//_IPTVcontext.SC_Channel_Data.Entry(ChanDataQuery).Reload();
				ChannelData = ChanDataQuery.ToList<SC_Channel_Data>();

				//var XMLP = _IPTVcontext.SC_Channel_Data.FromSqlRaw("SELECT * FROM SC_Channel_Data WHERE PortalID = 19 AND channelID = 45439");

				if (ChannelData.Count > 0)  // if this channel has a created link in the SC_Channel_Data
				{   // this is not populating after first match run even though the database fields are updated
					x.XMLName = ChannelData.FirstOrDefault().XMLChanName;
					x.XMLIDName = ChannelData.FirstOrDefault().XMLIDName;  //need the ID field
					x.score = ChannelData.FirstOrDefault().Score;
					x.TimeZone = ChannelData.FirstOrDefault().XMLTimeZone;
				}
				XMLMatchList.Add(x);
			}

			XMLObject = Enumerable.Range(0, XMLMatchList.Count).Select((x) =>
			{
				dynamic d = new ExpandoObject();
				d.channelID = XMLMatchList[x].channelID;
				d.Name = XMLMatchList[x].channelName;
				d.XMLName = XMLMatchList[x].XMLName;
				d.XMLIDName = XMLMatchList[x].XMLIDName;
				d.Score = XMLMatchList[x].score;
				d.TimeZone = XMLMatchList[x].TimeZone;
				d.Source = XMLMatchList[x].XMLSource;
				return d;
			}).Cast<ExpandoObject>().ToList<ExpandoObject>();
		}

		public void RowSelectHandler(RowSelectEventArgs<XMLMatchInfo> args)   // a row has been selected
		{
			//refresh();
			int x = 1;
			//selectedChannelID = args.Data.channelID.ToString();
		}

		// end of channels
		////////////////////////////////////////////////////////////////////////////////////
		private async Task GetUnmatchedChannels() // this is incorrect #TODO
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			unmatchedCount = 0;
			UnmatchedChannels = new List<SC_Channel> { };
			IQueryable<SC_Channel_Data> ChanDataQuery;   // default empty query
			foreach (var chan in OurChannels)
			{
				ChanDataQuery = from scd in _IPTVcontext.SC_Channel_Data
								where scd.PortalID == int.Parse(@ActivePortal) &&
								scd.channelID == chan.channelID
								select scd;
				ChannelData = ChanDataQuery.ToList<SC_Channel_Data>();

				if (ChannelData.Count == 0)  // if this channel does not have a created link in the SC_Channel_Data
				{
					unmatchedCount++;
					UnmatchedChannels.Add(chan);
				}
			}
		}

			////////////////////////////////////////////////////////////////////////////////////
			// Fuzzy matching - channels to XML guide channels
			private async Task DoFuzzyMatches()
		{
			bFuzzy = true;				// this doesn't work because of the await functions I think
			await GetXML("");           // reload this in case of changes made by me to the better names
										// These matches are not really countries because I have added other XML files
			await FuzzyMatch("SA");
			await FuzzyMatch("UK");
			await FuzzyMatch("US");

			//await FuzzyMatch("BEINUS");
			//await FuzzyMatch("BallyUS");
			//await FuzzyMatch("PE");

			string msg = "Fuzzy matching done";
			ShowToast(msg);
			await GetChannels("");              // I need to reload the data to see the refresh
			await ShowMatchesByChannels();      // this does cause a refresh because the XMLObject is changed
			await GetUnmatchedChannels();
			bFuzzy = false;
		}

		/*
         *  I might have to combine the xml for news and general to get a good match for ABC for example
         *  TSN is from Canada so might need that guide
         *  Sony movies UK no XML
         *  TSN - I need to manually set them to CA because my channel groups replace all linked channels by for example US
        */
		private async Task FuzzyMatch(string TimeZone)
		{
			// Plan:
			// Match XML channels from the XMLEPG file (XMLChannels table which is just the channels no EPG) to the channels in the portal database
			// using the pretty name as a fuzzy match and the XMLIDName in SC_Channel_data as the key that links the channel to the EPG channel
			// The SC_Channel_data table has all the channels but not all the XML rows (example 2230 SC_Channel_Data 2434 - not sure difference)
			if (OurXML.Count == 0)
			{
				ShowToast("No data in XMLChannels table");
				return;
			}
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			// reset previous matches first so it is a clean run
			string qry = "UPDATE SC_Channel_Data SET XMLIDName = '', " +
										"XMLID = 0, " +
										"XMLChanName = '', " +
										"Score = 0 " +
										"WHERE XMLTimeZone = '" + TimeZone + "' AND PortalID = " + ActivePortal;
			int rows = await _IPTVcontext.Database.ExecuteSqlRawAsync(qry);

			// this is the lookup between the prettier name and the XML full channel name 
			// I can't use dictionary anymore because I can now have multiples entries with the same idname like Film4+1.uk
			// 
			var XMLKeyList = new List<KeyValuePair<string, XMLFields>>();
			// now iterate the XML channel list and create an array of channel names

			// XML CHANNELS from the XMLChannel table
			// Pretty names from the XML EPG file
			// My new method uses the XMLChannels_Map table that enables an XML EPG source to be mapped to different time zones
			// So I need to extract all XML channels that are linked to a timezone even if they come from a XML source that is not that time zone

			//var queryX = "";
			if (TimeZone == "SA")
			{
			var	queryX =
			   from XMLChans in _IPTVcontext.XMLChannels
			   join XMLMap in _IPTVcontext.XMLChannels_Map on XMLChans.ID equals XMLMap.XMLChan_ID
			   where XMLMap.TimeZoneSA == 1
			   select new { XMLChans.ID, XMLChans.IDName, XMLChans.Name };
				var resultX = queryX.ToList();
				foreach (var x in resultX)
				{
					XMLFields info = new XMLFields();
					info.XMLIDName = x.IDName;              // the IDname is for example "Live.360.uk"
															// when I update the name field using my XMLrename data the better name gets saved in the XML table under the name field
															// to support multiple different names for one XML channels I might do this differently in the future
					info.XMLID = x.ID;                      // numeric ID number
					KeyValuePair<string, XMLFields> newitem = new KeyValuePair<string, XMLFields>(x.Name, info);
					// here I match the slightly prettier name extracted from the EPG xml (XML.Name) as the name to match against 
					XMLKeyList.Add(newitem); // add both the key (IDName) and pretty name for the channel to be used when adding to SC_Channels
											 // So XML dictionary has the key that is Name field from the XMLChannels table and the pair is the XMLFields which consists
											 // of the ID from the XMLchannels table and the IDName from that table e.g. "Live.360.uk"
				}
			}
			if (TimeZone == "UK")
			{
				var queryX =
				   from XMLChans in _IPTVcontext.XMLChannels
				   join XMLMap in _IPTVcontext.XMLChannels_Map on XMLChans.ID equals XMLMap.XMLChan_ID
				   where XMLMap.TimeZoneUK == 1
				   select new { XMLChans.ID, XMLChans.IDName, XMLChans.Name };
				var resultX = queryX.ToList();
				foreach (var x in resultX)
				{
					XMLFields info = new XMLFields();
					info.XMLIDName = x.IDName;                // the IDname is for example "Live.360.uk"
															  // when I update the name field using my XMLrename data the better name gets saved in the XML table under the name field
															  // to support multiple different names for one XML channels I might do this differently in the future
					info.XMLID = x.ID;                        // numeric ID number
					KeyValuePair<string, XMLFields> newitem = new KeyValuePair<string, XMLFields>(x.Name, info);
					// here I match the slightly prettier name extracted from the EPG xml (XML.Name) as the name to match against 
					XMLKeyList.Add(newitem); // add both the key (IDName) and pretty name for the channel to be used when adding to SC_Channels
											 // So XML dictionary has the key that is Name field from the XMLChannels table and the pair is the XMLFields which consists
											 // of the ID from the XMLchannels table and the IDName from that table e.g. "Live.360.uk"
				}
			}
			if (TimeZone == "US")
			{
				var queryX =
				   from XMLChans in _IPTVcontext.XMLChannels
				   join XMLMap in _IPTVcontext.XMLChannels_Map on XMLChans.ID equals XMLMap.XMLChan_ID
				   where XMLMap.TimeZoneUS == 1
				   select new { XMLChans.ID, XMLChans.IDName, XMLChans.Name };
				var resultX = queryX.ToList();
				foreach (var x in resultX)
				{
					XMLFields info = new XMLFields();
					info.XMLIDName = x.IDName;                // the IDname is for example "Live.360.uk"
															  // when I update the name field using my XMLrename data the better name gets saved in the XML table under the name field
															  // to support multiple different names for one XML channels I might do this differently in the future
					info.XMLID = x.ID;                        // numeric ID number
					KeyValuePair<string, XMLFields> newitem = new KeyValuePair<string, XMLFields>(x.Name, info);
					// here I match the slightly prettier name extracted from the EPG xml (XML.Name) as the name to match against 
					XMLKeyList.Add(newitem); // add both the key (IDName) and pretty name for the channel to be used when adding to SC_Channels
											 // So XML dictionary has the key that is Name field from the XMLChannels table and the pair is the XMLFields which consists
											 // of the ID from the XMLchannels table and the IDName from that table e.g. "Live.360.uk"
				}
			}
			// I now have the XMLChannels just for the selected timezone

			////////////////////////////////////////////////////////////////////////////////////////////////
			// BETTER RENAMES
			// But now a new idea where I just add more entries to the dictionary and XMLChanNames using the XMLrenames instead of
			// XMLrenames overwriting the name field in the XMLChannel table
			// Later I discovered a problem where BBC.One.UK has a pretty name of BBC One but it doesn't have EPG data so I wanted
			// BBC One to be something else that wouldn't get picked

			// So first I will remove existing match data if a better rename has been defined.  The better renames will then be added below.
			List<XMLRename>? PreRenames;
			PreRenames = _IPTVcontext.XMLRename.FromSql($"SELECT * FROM XMLRename").ToList();
			foreach (var rename in PreRenames)
			{
				if (rename.XMLType == TimeZone)
				{
					// Now remove the existing item due to at least one rename coming in
					var resultkey = XMLKeyList.Where(kvp => kvp.Value.XMLIDName == rename.NameFromXML);
					if (resultkey != null)
					{
						string oops1 = "About to remove channel that I don't need due to better rename : " + rename.NameFromXML + " ID: " + rename.ID;
						Log.Information(oops1);
						// so I have found an existing item that I will now delete so that only my better rename is used
						XMLKeyList.Remove(resultkey.SingleOrDefault());     // delete the XMLKeyList item
						string oops = "Removed channel that I don't need due to better rename : " + rename.NameFromXML + " ID: " + rename.ID;
						Log.Information(oops);
					}
				}
			}

			List<XMLRename>? Renames;
			Renames = _IPTVcontext.XMLRename.FromSql($"SELECT * FROM XMLRename").ToList();
			foreach (var rename in Renames)
			{
				if (rename.XMLType == TimeZone)
				{
					XMLFields info = new XMLFields();
					// Find the related XMLChannels table data based upon the IDName which is the full XML name (not pretty)
					XMLChannel XMLChan = OurXML.Find(x => x.IDName == rename.NameFromXML); //NameFromXML is the IDName such as BBC.R.Ulster.uk 
					if (XMLChan != null)    // so this is a table data item from XMLChannels
					{
						info.XMLID = XMLChan.ID;
						info.XMLIDName = rename.NameFromXML;

						KeyValuePair<string, XMLFields> newitem = new KeyValuePair<string, XMLFields>(rename.BetterRename, info);
						XMLKeyList.Add(newitem);
						string oops2 = "XMLRaname add better rename : " + newitem.Key;
						Log.Information(oops2);
					}
					// I now have the unique XML name and a pretty name so just add the pretty name
				}
			}
			// So now all my XMLKeyList have been added as extra entries in the match list XMLKeyList - so I can have one XML channel refer to multiple pretty names
			// this would be for example BBC 1 and BBC One
			////////////////////////////////////////////////////////////////////////////////////////////////

			//Create the list of channel names for fuzzy matching
			string[] XMLChanMatchNames = new string[] { };   // a list of the XML channel names which is the pretty name from the XML file
			foreach (KeyValuePair<string, XMLFields> chankey in XMLKeyList) // KeyValuePair<string, XMLFields>
			{
				XMLChanMatchNames = XMLChanMatchNames.Append(chankey.Key).ToArray();
			}

			// new format for query to try
			// The SC_Channel_Data stores channel info outside of the core SC_Channel table related to which XML
			// guide to use, match scores etc
			var queryL =
			   from chData in _IPTVcontext.SC_Channel_Data
			   join channels in _IPTVcontext.SC_Channels on chData.channelID equals channels.channelID
			   where chData.PortalID == int.Parse(ActivePortal)
			   select new { channels.channelID, chData.XMLTimeZone, channels.name, channels.portalID };

			// Execute the query and output the results
			var result = queryL.ToList();

			// The channels are matched to the XMLChanNames and then the index of that best matching item is matched
			// to the XMLKeyList list which is the exact same length so the index is valid between them
			if (XMLChanMatchNames.Length > 0)    // in case one country doesn't have an XML file
			{
				//                 ShowProgress = true;     // I can't get the progress to work in the loop
				//                 progressMax = OurXML.Count;
				int count = 0;
				foreach (var chan in result)
				{
					count++;
					//                     UpdateProgress(count);
					//                     pBar.Value = count;
					//                     progressValue = count;
					//                     await pBar.RefreshAsync();
					//                     pBar.Maximum = 100;

					if (chan.XMLTimeZone == TimeZone)  // only do channels that match the country
					{
						string thechan = chan.name;

						// replace parts of the channel name to enhance the match - looks like it is case sensitive
						if (thechan.Contains("#####")) thechan = "#####";   // I don't want to match the dividers
						thechan = thechan.Replace("[UK]", "");
						thechan = thechan.Replace("[US]", "");
						thechan = thechan.Replace("[NA]", "");
						//thechan = thechan.Replace(" UK ", "");
						//thechan = thechan.Replace(" USA ", "");
						thechan = thechan.Replace("US-", "");
						thechan = thechan.Replace("UK-", "");
						thechan = thechan.Replace("UK -", "");
						thechan = thechan.Replace("USA -", "");
						thechan = thechan.Replace("HDTV", "");
						thechan = thechan.Replace("HEVC", "");
						//thechan = thechan.Replace("EAST", "");
						//thechan = thechan.Replace("WEST", "");
						thechan = thechan.Replace("4K+", "");
						thechan = thechan.Replace("4K", "");
						thechan = thechan.Replace("FHD", "");
						thechan = thechan.Replace("UHD", "");
						thechan = thechan.Replace("HD+", "");
						thechan = thechan.Replace("HD", "");
						thechan = thechan.Replace("NBC GOLF", "GOLF CHANNEL");      // NBC Golf and the Golf Channel are the same
						thechan = thechan.Replace("NBCS", "NBC SPORTS");      // expand the abbreviation
						thechan.Trim();   // trim leading and trailing spaces
										  // This is the matching part 
						var bestmatch = Process.ExtractOne(thechan, XMLChanMatchNames);
						//var bestmatch = Process.ExtractOne(query, XMLChanNames, s => s, ScorerCache.Get<DefaultRatioScorer>());    // Get the best fuzzy match based upon the XML pretty name and the channel name
						//var bestmatch = Process.ExtractOne(query, XMLChanNames, s => s, ScorerCache.Get<PartialRatioScorer>());
						int x = bestmatch.Index;
						string fuzzy = $"Fuzzy scoring. Channel: >{thechan}< \t\tScore: {bestmatch.Score} \t\tXML Match: >{bestmatch.Value}<";
						//Log.Information(fuzzy);
						if (bestmatch.Score > 80)   //  the minimum score to declare a good match
													// After I implemented saving the score I decided to bump the min score to 80
						{
							if (_IPTVcontext is not null)
							{
								string XMLIDName;   // this must be for example "Live.360.uk" and not any of the prettier names
								XMLIDName = XMLKeyList.ElementAt(x).Key;
								XMLFields XF = new XMLFields();
								//XMLDictionary.ElementAt(x).
								//XF = XMLDictionary.ElementAt(x);
								XF = XMLKeyList.ElementAt(x).Value;

								bool skip = false;

								if (XF.XMLIDName == "E!" && !thechan.Contains("E!")) skip = true;  // prevent E! matching too many channels
																								   // Here I update the SC_Channel_Data row because a good match has been made between the XML channel name and the channel name
																								   // Maybe I should actually create the new rows on the groups page when I assign countries to the channels and then just update here - I think I do this now
								if (skip == false)
								{
									string sqlquery = "UPDATE SC_Channel_Data SET XMLIDName = '" + XF.XMLIDName + "', " +
										"XMLID = " + XF.XMLID + ", " +
										"XMLChanName = '" + XMLIDName + "', " +
										"Score = " + bestmatch.Score.ToString() + " " +
										"WHERE channelID = " + chan.channelID + " AND PortalID = " + chan.portalID;
									int rowsins = await _IPTVcontext.Database.ExecuteSqlRawAsync(sqlquery);
								}
							}
						}
					}
				}
			}
			//             ShowProgress = false;
		}

		//
		////////////////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////////////////////
		// Utilities
		private async Task ShowToast(string msg)
		{
			ToastObj.Content = msg;
			await this.ToastObj.ShowAsync();
		}

		private async Task UpdateProgress(int Value)    // never works
		{
			pBar.Value = Value;
			await pBar.RefreshAsync();
		}
	}
}