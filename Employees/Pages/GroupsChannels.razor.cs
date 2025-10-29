using IPTV.data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Kanban.Internal;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.RichTextEditor;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

/*
	The tvGenreID is used to link channels to groups.  I no longer use commas in the tvGenreID field to allow a channel to appear in multiple groups.
	SC_ChannelData has a country field to reference the XML EPG
	SC_Groups_Custom is a table that stores a list of custom groups and rows for each ChanUniqueID that forms part of the custom group
*/


namespace IPTVData.Pages
{
	public partial class GroupsChannels
	{
		public bool ShowCreate { get; set; }
		public bool ShowEdit { get; set; }
		public long GroupEditingId { get; set; }

		//Overall
		private string editMode = "groups";                             // the toggle to define whether I am working on groups or channels in groups
		private string TimeZone = "";                                   // the toggle to define the timezone which I use for the XML EPG guide later
		private string sizing = "25%";
		Utils? _utils;
		private bool channelmode;
		private bool groupmode;
		private bool countrymode;

		// Groups
		// 		private IptvDataContext? _IPTVcontext;					// Don't declare this here or data is cached and it cause problems - stale
		public SC_Groups? NewGroup { get; set; }
		public SC_Groups? GroupToUpdate { get; set; }
		public List<SC_Groups>? OurGroups { get; set; }
		public List<SC_Groups>? OurChannels { get; set; }
		SfListBox<string[], SC_Groups>? GroupListBox;                   // this enables calls to the listbox.  It is referenced in the Blazor file.  I'm not really using this now.
		private string[] groupCBvalues = new string[] { };              //  "89, 1025" example data.  This drives the checkbox to be checked or not.  First data is added on read from the database.
		private string[] groupCBEmpty = new string[] { };               // An empty list to use to not have anything selected
		private string[] channelCBvalues = Array.Empty<string>();       //  from the full list of channels.  These are channels attached to the selected group
		private string[] midgroupCBvalues = Array.Empty<string>();      // the middle splitter
		public bool showgroupboxes;                                     // show checkboxes or not
		public bool OnlyShowEnabledGroups;                             // show enabled groups or not
		public bool onlyshowselectedchannels;                           // show selected channels only - this is in the all channels list box
		public string? selectedGroup;                                   // this is the group ID and is used to show related channels
		SfListBox<string[], SC_Channel>? GroupChanListBox;

		//Channels
		public List<SC_Channel>? GroupChannels { get; set; }
		public List<SC_Channel>? AllChannels { get; set; }
		public List<SC_Channel>? ResultChannels { get; set; }           // used for interim work on channels
		public string? selectedChannel;                                 // this is the channel ID and is used to ... not sure yet
		public string? ActivePortal;                                    // the active portal
		private string[] allchannelvalues = Array.Empty<string>();

		//Toasts
		SfToast ToastObj;
		private string ToastPosition = "Center";
		private string ToastContent = "";

		// channel data
		public List<SC_Channel_Data>? ChannelData { get; set; }

		protected override async Task OnInitializedAsync()
		{
			ShowCreate = false;
			_utils = new Utils();
			// I need to find out the active portal
			ActivePortal = _utils.getPortalNumber();

			showgroupboxes = true;
			await LoadGroups("");
			await LoadGroupChannels("");
		}

		private async Task ShowToast(string msg)
		{
			ToastObj.Content = msg;
			await this.ToastObj.ShowAsync();
		}

		private async Task onToggledEnabled(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{
			await LoadGroups("");
			await LoadAllChannels("");
		}

		private async Task onToggledAllEnabled(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{
			// 			await LoadGroups("");	// don't do this it loses the focus of the current group and selects all enabled
			await LoadAllChannels("");
			await LoadGroupChannels("");
		}

		private async void OnchangeGroup(ListBoxChangeEventArgs<string[], SC_Groups> args)    // Syncfusion version
		{   // this event fires even when you click on the name.  The checkbox gets checked or unchecked.
			// When I want to assign groups to EPG zones
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (editMode == "channels")         // I'm working on channels attached to groups or assigning EPG zones
			{
				if (args.Value is not null)
				{
					selectedGroup = args.Value[0];          // assign the first and only
					groupCBvalues = args.Value;				// save the selected group(s) into the checkbox array
				}

				await LoadGroupChannels("");          // show all channels that match the selectedCroup
													  //country = "";                   // reset
			}
			else // Working on whether a group is enabled or not
			{
				// Here I reset all groups to disabled and then add in the selected groups below
				string query = "UPDATE SC_Groups SET Enabled = 0 WHERE PortalID = " + ActivePortal;
				await _IPTVcontext.Database.ExecuteSqlRawAsync(query);

				// Now set the flag for my app to reset the groups and channel map tables on the next start to make sure I get a clean groups and channel list
				string qry = "UPDATE Settings SET value = '1' WHERE name = 'DeleteKodiGroupsAndMaps'";
				await _IPTVcontext.Database.ExecuteSqlRawAsync(query);

				if (args.Value != null && OurGroups != null) // make sure that it isn't null.  This happens when there are no groups selected
				{
					groupCBvalues = args.Value;     // save the selected groups into the checkbox array
					foreach (var item in args.Value)
					{
						foreach (var txt in groupCBvalues)
						{
							SC_Groups result = OurGroups.Find(x => x.SC_ID == txt);
							if (result != null)
							{
								_IPTVcontext.Attach(result);     // from https://www.learnentityframeworkcore.com/dbcontext/modifying-data
								result.Enabled = 1;
								_IPTVcontext.Entry(result).Property("Enabled").IsModified = true;
								_IPTVcontext.Update(result);
							}
						}
					}
				}
				else    // no items selected
				{
					groupCBvalues = groupCBEmpty;
				}


				// now reset the groups by triggering a groups update from the portal - this will trigger my addon to reload groups from the portal
				qry = "UPDATE Settings SET value = '1' WHERE name = 'DeleteKodiGroupsAndMaps'";
				_IPTVcontext.Database.ExecuteSqlRaw(qry);   // don't do async to make sure changes are reflected before moving on
															// refresh the group list - this works
				_IPTVcontext.SaveChanges();     // Now it updates all fields that changed and is way faster than doing it in the foreach loop
				await LoadGroups("");
				await LoadGroupChannels("");
			}
			LoadAllChannels("");
		}

		private async Task OnChangeGroupMode()
		{   // this works but the checkbox above doesn't force a refresh...
			if (editMode == "groups")
			{
				showgroupboxes = true;
				await LoadGroups("");
				await LoadGroupChannels("");
				await LoadAllChannels("");
				sizing = "25%";
			}

			if (editMode == "channels")
			{
				await LoadGroups("");
				await LoadGroupChannels("");
				await LoadAllChannels("");
				sizing = "100%";
			}
		}

		//------------------ Create! ----------------///
		public void ShowCreateForm()
		{
			ShowCreate = true;
			NewGroup = new SC_Groups();
		}

		public async Task CreateNewGroup()
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (NewGroup is not null)
			{   // if the Id is not unique to the database I get an error.  It is supposed to auto increment.
				_IPTVcontext?.SC_Groups.Add(NewGroup);
				_IPTVcontext?.SaveChangesAsync();
			}

			ShowCreate = false;
			await LoadGroups("");
		}

		//------------------ Delete! ----------------///

		public async Task DeleteGroup(SC_Groups ourGroup)
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (_IPTVcontext is not null)
			{
				if (ourGroup is not null) _IPTVcontext.SC_Groups.Remove(ourGroup);
				await _IPTVcontext.SaveChangesAsync();
			}
			await LoadGroups("");
		}

		//------------------ Read! ----------------///

		public async Task LoadGroups(string filter)
		{
			groupCBvalues = new string[] { };    // clear the list for a fresh start
			OurGroups = new List<SC_Groups>();  // doesn't refresh checkboxes

			IEnumerable<string> LoadedGroupIDs2 = new List<string>();
			if (OurGroups != null)  // this solves the first refresh that doesn't otherwise work
			{
				OurGroups.Clear();  // trying to force refresh
				LoadedGroupIDs2 = new string[] { };
			}

			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			IQueryable<SC_Groups> query;             // default empty query
													 //var query2;
			if (_IPTVcontext is not null)
			{
				if (filter != null && filter != "")
				{
					// get the groups that contain the filter text
					if (OnlyShowEnabledGroups)
					{
						query = from gr in _IPTVcontext.SC_Groups
								where gr.Name.ToUpper().Contains(filter) &&
								gr.Enabled == 1 &&
								gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only groups for the current portal
								select gr;
					}
					else
					{
						query = from gr in _IPTVcontext.SC_Groups
								where gr.Name.ToUpper().Contains(filter) &&
								gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only groups for the current portal
								select gr;
					}
					OurGroups = query.ToList<SC_Groups>();
					foreach (var Group in OurGroups)    // If a group is marked as enabled then set the checkbox
					{
						if (Group is not null)
						{
							if (Group.Enabled == 1)
							{
								LoadedGroupIDs2 = LoadedGroupIDs2.Concat(new string[] { Group.SC_ID });
							}
						}
					}
					groupCBvalues = LoadedGroupIDs2.ToArray(); // this is needed or the checkboxes aren't checked

					if (OurGroups.Count != 0) GroupToUpdate = _IPTVcontext.SC_Groups.First();  // load the first group as the one to update
				}
				else // no filter text
				{
					if (OnlyShowEnabledGroups)
					{
						query = from gr in _IPTVcontext.SC_Groups
								where gr.PortalID == int.Parse(@ActivePortal) &&    // string to int conversion - only groups for the current portal
								gr.Enabled == 1
								select gr;
						var query2 =
						   from Groups2 in _IPTVcontext.SC_Groups
						   where Groups2.Enabled == 1 && Groups2.PortalID == int.Parse(@ActivePortal)
						   select new { Groups2.ID, Groups2.SC_ID, Groups2.Name, Groups2.Enabled, Groups2.PortalID };
						var result2 = query2.ToList();
						int x = 0;
					}
					else
					{
						query = from gr in _IPTVcontext.SC_Groups
								where gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only groups for the current portal
								select gr;
						/*
						 * this worked but at the same time as the above - dunno why I couldn't get it to at first to get the int value
						var query2 =
						   from gr in _IPTVcontext.SC_Groups
						   where gr.PortalID == int.Parse(@ActivePortal)
						   select new { gr.ID, gr.SC_ID, gr.Name, gr.Enabled, gr.PortalID };
						var result2 = query2.ToList();
						int x = 0;
						*/
					}

					var result = query.ToList<SC_Groups>();     // using a local variable seems to cause a refresh
					OurGroups = query.ToList<SC_Groups>();		// this was not refreshing the enabled field in the database but needed to show data
					if (OurGroups.Count != 0) GroupToUpdate = _IPTVcontext.SC_Groups.First();  // load the first group as the one to update

					foreach (var Group in result)    // If a group is marked as enabled then set the checkbox - OurGroups didn't work
					{
						if (Group is not null)
						{
							if (Group.Enabled == 1)
							{
								LoadedGroupIDs2 = LoadedGroupIDs2.Concat(new string[] { Group.SC_ID });
							}
						}
					}
					groupCBvalues = LoadedGroupIDs2.ToArray(); // this is needed or the checkboxes aren't checked
				}
			}
			await _IPTVcontext.SaveChangesAsync();
			if (LoadedGroupIDs2.Count() == 0)   // if there are no groups shown because of the filter or more likely none checked as enabled then remove the active checkbox
			{
				OnlyShowEnabledGroups = false; // make sure all groups can be seen
			}
		}

		//------------------ Update! ----------------///

		public async Task ShowGroupEditForm(SC_Groups ourGroup)
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				GroupToUpdate = _IPTVcontext.SC_Groups.FirstOrDefault(x => x.ID == ourGroup.ID);
				ShowEdit = true;
				GroupEditingId = ourGroup.ID;
			}
		}

		public async Task UpdateGroup()
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				if (GroupToUpdate is not null) _IPTVcontext.SC_Groups.Update(GroupToUpdate);
				await _IPTVcontext.SaveChangesAsync();
			}
			ShowEdit = false;
		}

		private void ChangeChannelToGroup(ListBoxChangeEventArgs<string[], SC_Channel> args)    // Syncfusion version - this is when selecting channels to add or remove to a group
		{   // this event fires even when you click on the name.  The checkbox gets checked or unchecked.
			// The args parameter is a list of the selected channels
			// this is the third splitter
			if (editMode == "channels")             // I'm working on channels attached to groups
			{
				if (args.Value == null)             // no channels are selected
				{
					ApplyGroupLinkToChannel(selectedChannel, "REMOVE"); // remove this channel to the group
					channelCBvalues = new string[] { };// reset the array of channels
					return;                         // if no channels are left selected then exit
				}
				int argscount = args.Value.Length;  // args is not null here so some channels are selected
				if (channelCBvalues == null)          // previously no channels were selected
				{
					selectedChannel = args.Value[0];// assign the first and only
					allchannelvalues = args.Value;  // update the selected channels array
					return;                         // exit now
				}
				if (argscount > channelCBvalues.Length)  // a new item has been checked so the args have the greater items array
				{
					foreach (string item in args.Value)
					{
						bool result = Array.Exists(channelCBvalues, element => element == item);
						if (!result) // the item is not in the existing checked items list so it is the new item
						{
							selectedChannel = item;
							ApplyGroupLinkToChannel(selectedChannel, "ADD"); // add this channel to the group
						}
					}
					channelCBvalues = args.Value;       // update the selected channels array
				}
				else // a previously selected channels has been deselected
				{
					foreach (string item in channelCBvalues)
					{
						bool result = args.Value.Contains(item); // .NET 3.5
						if (!result) // the item is not in the existing checked items list so it is the new item
						{
							selectedChannel = item;
							ApplyGroupLinkToChannel(selectedChannel, "REMOVE"); // remove this channel to the group
						}
					}
					channelCBvalues = args.Value;       // update the selected channels array that belongs to the selected group
				}
			}
			LoadGroupChannels("");
		}

		// This is when text is typed into the filter box
		private void GroupsChannelsInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the channels by the text
			LoadGroupChannels(args.Value);
			// Here you can customize your code
		}

		private void AllChannelsInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the channels by the text
			LoadAllChannels(args.Value);
			// Here you can customize your code
		}

		private void GroupsInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the channels by the text
			args.Value = args.Value.ToUpper();
			LoadGroups(args.Value);
			// Here you can customize your code
		}

		////////////////////////////////////////////////////////////////////////////////////
		//                                      SC_Channels and Groups
		public async Task LoadGroupChannels(string filter)
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			filter = filter.ToUpper();  // change to filter to upper case to help with matching
			GroupChannels = new List<SC_Channel>(); // this works to force a refresh of the list

			IQueryable<SC_Channel> query;   // default empty query
			if (_IPTVcontext is not null)
			{
				// Now with my custom groups I also need to add to channels to the the list if a 
				// custom group is selected - the queries below will return nothing
				if (selectedGroup is not null)
				{
					if (int.Parse(selectedGroup) > 9999 && int.Parse(selectedGroup) < 10050)
					{
						// get the channels that match the group ID
						IQueryable<SC_Groups_Custom> SCquery;   // default empty query
						SCquery = from ch in _IPTVcontext.SC_Groups_Custom
								  where ch.groupSC_ID == int.Parse(selectedGroup) &&
								  ch.PortalID == int.Parse(@ActivePortal)
								  select ch;
						List<SC_Groups_Custom> CustomList = SCquery.ToList<SC_Groups_Custom>();
						// So bow I have a list of SC_Groups_Custom items that match my group number
						// So for each one work out the channel and get the channel info
						foreach (var grCust in CustomList)
						{
							// get the channels that match the group ID
							query = from ch in _IPTVcontext.SC_Channels
									where ch.uniqueID == grCust.ChanUniqueID.ToString() &&
									ch.portalID == int.Parse(@ActivePortal)
									select ch;
							List<SC_Channel> CustomChan = query.ToList<SC_Channel>();
							foreach (var chan in CustomChan)
							{
								GroupChannels.Add(chan);
							}
						}
					}
					else // not a custom group
					{
						if (filter != null && filter != "")
						{
							// get the channels that match the group ID and also contain the filter text
							query = from ch in _IPTVcontext.SC_Channels
									where ch.tvGenreID == selectedGroup &&
									ch.name.ToUpper().Contains(filter) &&
									ch.portalID == int.Parse(@ActivePortal)     // string to int conversion - only groups for the current portal
									select ch;
						}
						else
						{
							// get the channels that match the group ID
							query = from ch in _IPTVcontext.SC_Channels
									where ch.tvGenreID == selectedGroup &&      // #TODO my golf group adds a genre so doesn't get caught
									ch.portalID == int.Parse(@ActivePortal)     // string to int conversion - only groups for the current portal
									select ch;
						}
						GroupChannels = query.ToList<SC_Channel>();
					}
				}
			}
		}

		public async Task LoadAllChannels(string filter)    // the third and furthest right listbox
		{
			// Reset items
			channelCBvalues = new string[] { }; // reset the array of selected channels

			// This first section is for non custom groups
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			filter = filter.ToUpper();  // change to filter to upper case to help with matching

			// Get channels or a list of filtered channels
			IQueryable<SC_Channel> query;   // default empty query
			if (_IPTVcontext is not null)
			{
				if (filter != null && filter != "")     // there is some filter text
				{
					// get the channels that match the filter text
					query = from ch in _IPTVcontext.SC_Channels
							where ch.name.ToUpper().Contains(filter) &&
							ch.portalID == int.Parse(@ActivePortal)         // string to int conversion - only channels for the current portal
							select ch;
					AllChannels = query.ToList<SC_Channel>();
				}
				else // no filter text so get all channels
				{
					query = from ch in _IPTVcontext.SC_Channels
							where ch.portalID == int.Parse(@ActivePortal)
							select ch;
					AllChannels = query.ToList<SC_Channel>();
				}
			}

			List<SC_Channel>? TempChannels;
			TempChannels = AllChannels.ToList(); // save the full list - it is important to do the tolist function or it seems to reference the original
			int index = 0;

			for (int i = AllChannels.Count - 1; i >= 0; i--)    // Go in a reverse direction since I remove some channels
			{
				SC_Channel Channel = AllChannels[i];
				bool channelIsSelected = false;
				if (Channel is not null)
				{
					string[] words = Channel.tvGenreID.Split(',');  // split the tvGenreID into individual groups because it can have multiple
					foreach (var word in words)
					{
						if (word == selectedGroup)      // this channel references the selected group so add it to the checkbox list
						{
							channelCBvalues = channelCBvalues.Append(Channel.uniqueID).ToArray();   // this is the way to append a string to an array - that took a while to work out
							channelIsSelected = true;   // this is a channel that is tied to the selected group
						}
					}

					// now slim down the list if I only want to display the selected channels
					if (onlyshowselectedchannels)
					{
						if (!channelIsSelected) // in this case cut out the channel
						{
							TempChannels.RemoveAt(i);
						}
					}
				}
				index++;
			}
			AllChannels = TempChannels.ToList(); // now copy back what is left

			// Get the list of disabled channels
			IQueryable<SC_Channel_Disabled> SCDquery;   // default empty query
			SCDquery = from ch in _IPTVcontext.SC_Channel_Disabled
					   where ch.Portal == int.Parse(@ActivePortal)
					   select ch;
			List<SC_Channel_Disabled> DisabledChans = SCDquery.ToList<SC_Channel_Disabled>();

			//////////////////////////////////////////////////////////////////////////////////////
			// CUSTOM GROUPS
			// Now I have to check if these are channels that belong to a custom group
			// and then get the channels that match the group ID
			if (selectedGroup != null)
			{
				if (int.Parse(selectedGroup) > 9999 && int.Parse(selectedGroup) < 10050)
				{
					AllChannels = new List<SC_Channel>();   // clear out the list
															// get the group from the SC_Groups_Custom database
					IQueryable<SC_Groups_Custom> SCquery;   // default empty query
					SCquery = from ch in _IPTVcontext.SC_Groups_Custom
							  where ch.groupSC_ID == int.Parse(selectedGroup) &&
							  ch.PortalID == int.Parse(@ActivePortal)
							  select ch;
					List<SC_Groups_Custom> CustGrp = SCquery.ToList<SC_Groups_Custom>();
					// This list will probably just have one group in it 
					// So now I have a list of SC_Groups_Custom items that match my group number
					// So for each one work out the channel and get the channel info

					// Group matching or all channels if the only selected checkbox is not selected
					List<SC_Channel> AllChans = new List<SC_Channel>();
					if (!onlyshowselectedchannels)  // All channels
					{
						query = from ch2 in _IPTVcontext.SC_Channels
								where ch2.portalID == int.Parse(@ActivePortal)
								select ch2;
						AllChans = query.ToList<SC_Channel>();
						// This is all the channels
					}
					else // just selected
					{
						foreach (var grChan in CustGrp)
						{
							query = from ch in _IPTVcontext.SC_Channels
									where ch.uniqueID == grChan.ChanUniqueID.ToString() &&
									ch.portalID == int.Parse(@ActivePortal)
									select ch;
							// 						AllChans = query.ToList<SC_Channel>();
							SC_Channel qChan = query.FirstOrDefault();
							AllChans.Add(qChan);
						}
						// This is the channels that match the selected group
					}

					// now if there is a filter
					if (filter != null && filter != "")     // there is some filter text
					{
						AllChans = AllChans.FindAll(
								delegate (SC_Channel chan)
								{
									return chan.name.Contains(filter);
								}
							);
					}

					// now deal with checkboxes
					// iterate the found channels and set checkboxes because this list is just for channels in the group
					foreach (var chan in AllChans)
					{
						if (chan is not null)
						{
							foreach (var grp in CustGrp)
							{
								if (grp.ChanUniqueID == int.Parse(chan.uniqueID))
								{
									// now before I add the checkbox I need to check if this is a permanently disabled channel
									bool disabled = false;
									foreach (var chan2 in DisabledChans)
									{
										if (chan2.channelID == chan.channelID)  // this channel is flagged as disabled so do not select the checkbox
										{
											disabled = true;
											break;
										}
									}

									if (!disabled) channelCBvalues = channelCBvalues.Append(chan.uniqueID).ToArray();   // this is the way to append a string to an array - that took a while to work out
								}
							}
						}
					}
					AllChannels = AllChans; // this is the final list
				}
			}

		}

		public async Task ApplyGroupLinkToChannel(string channel, string mode)
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				SC_Channel chan = _IPTVcontext.SC_Channels.First(item => item.uniqueID == channel &&
					item.portalID.ToString() == ActivePortal);

				if (mode == "ADD")
				{
					// For my custom groups do this differently.  I no longer edit the tv genre field in channels because that table gets refreshed
					if (int.Parse(selectedGroup) > 9999 && int.Parse(selectedGroup) < 10050)
					{
						// I need to get the name of the selected group
						IQueryable<SC_Groups> query;   // default empty query
						query = from g in _IPTVcontext.SC_Groups
								where g.SC_ID == selectedGroup &&
								g.PortalID == int.Parse(@ActivePortal)
								select g;
						var cGroup = query.FirstOrDefault();
						string name = "";
						if (cGroup != null) // just making sure that the group name was found
						{
							name = cGroup.Name;
						}
						// Now I need to check if this group and channel is already in the SC_Groups_Custom table
						IQueryable<SC_Groups_Custom> query2;   // default empty query
						query2 = from g in _IPTVcontext.SC_Groups_Custom
								 where g.groupSC_ID == Int32.Parse(selectedGroup) &&
								 g.ChanUniqueID == Int32.Parse(selectedChannel) &&
								g.PortalID == int.Parse(@ActivePortal)
								 select g;
						var custGroup = query2.FirstOrDefault();
						if (custGroup == null) // no previous entry so I need to create one
						{
							SC_Groups g = OurGroups.Find(item => item.ID == Int32.Parse(selectedGroup));
							if (g == null)
							{
								SC_Groups_Custom group = new SC_Groups_Custom();
								group.PortalID = Int32.Parse(ActivePortal);
								group.groupName = cGroup.Name;
								group.groupSC_ID = Int32.Parse(selectedGroup);
								group.ChanUniqueID = Int32.Parse(selectedChannel);
								_IPTVcontext.SC_Groups_Custom.Add(group);
							}
						}
					}
					else    // this is not one of my custom created groups
							// I no longer use commas so just replace the current value - this will be overwritten on next channel update
					{
						chan.tvGenreID = selectedGroup;
						// remove the row(s) - should be one - from the disabled channels table
						string query = "DELETE FROM SC_Channel_Disabled WHERE ChannelID = " + chan.channelID + " AND Portal =  " + ActivePortal;
						_IPTVcontext.Database.ExecuteSqlRawAsync(query);
					}
				}
				if (mode == "REMOVE")   // No action for non custom groups - they are just overwritten temporarily
				{
					if (selectedGroup != null)
					{
						if (int.Parse(selectedGroup) > 9999 && int.Parse(selectedGroup) < 10050)
						{
							string query = "DELETE FROM SC_Groups_Custom WHERE GroupSC_ID = " + selectedGroup + " AND ChanUniqueID = " + selectedChannel + " AND PortalID = " + ActivePortal;
							_IPTVcontext.Database.ExecuteSqlRawAsync(query);
						}
						// for non custom groups - the portal provides the group in the tvGenreID field.  So to remove a channel from a group....
						// For now just blank out the tvGenreID field - if there is a complete table refresh from the portal it will be overwritten but good enough for now
						// my new improved version is to add a row to the SC_Channel_Disabled table if I uncheck a channel from being in a group.  This is a permanent
						// solution for when groups and channels update.
						else
						{
							chan.tvGenreID = "";
							// add the row to the disabled channels table
							string query = "INSERT INTO SC_Channel_Disabled VALUES (NULL, " + chan.channelID + ", " + ActivePortal + ")";
							_IPTVcontext.Database.ExecuteSqlRawAsync(query);
						}
					}
				}
				_IPTVcontext.SaveChanges();
			}
		}

		private void OnClickHandler()   // For group button to see if I like using it
		{
			//if(args.GetType == )
			if (groupmode)
			{
				editMode = "groups";
				OnChangeGroupMode();

			}
			if (channelmode)
			{
				editMode = "channels";
				OnChangeGroupMode();
			}
		}

		private async Task OnChangeTimeZone(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{
			// The tvGenreID reflects the group 
			List<SC_Channel>? Results;
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			IQueryable<SC_Channel> query;             // default empty query
			Results = new List<SC_Channel>();
			List<SC_Channel> FinalResults = new List<SC_Channel>();

			// This is the first splitter where I select groups to be added to a timezone - all channels in each group will be assigned to that XMLEPG
			// This is to allow more than one group to be selected when adding channels to a XML category to make it faster to add groups to a XMLEPG
			if (groupCBvalues is not null)
			{ };

			//IEnumerable<SC_Groups> grouplist = GroupListBox.GetDataList();	// nice way to get the list data
			if (GroupListBox.Value != null)     // I could and maybe should use groupCBvalues here to be consistent
			{
				foreach (var groupnoi in GroupListBox.Value)    // iterate each group selected to get all channels in that group
				{
					// first check if this is a custom group
					if (int.Parse(selectedGroup) > 9999 && int.Parse(selectedGroup) < 10050)
					{
						// get the channels that match the group ID
						IQueryable<SC_Groups_Custom> SCquery;   // default empty query
						SCquery = from ch in _IPTVcontext.SC_Groups_Custom
								  where ch.groupSC_ID == int.Parse(selectedGroup) &&
								  ch.PortalID == int.Parse(@ActivePortal)
								  select ch;
						List<SC_Groups_Custom> CustomList = SCquery.ToList<SC_Groups_Custom>();
						// So now I have a list of SC_Groups_Custom items that match my group number
						// So for each one work out the channel and get the channel info
						foreach (var grCust in CustomList)
						{
							// get the channels that match the group ID
							query = from ch in _IPTVcontext.SC_Channels
									where ch.uniqueID == grCust.ChanUniqueID.ToString() &&
									ch.portalID == int.Parse(@ActivePortal)
									select ch;
							List<SC_Channel> CustomChan = query.ToList<SC_Channel>();
							foreach (var chan in CustomChan)
							{
								Results.Add(chan);
							}
						}
					}
					else // not a custom group
					{
						query = from ch in _IPTVcontext.SC_Channels
								where ch.tvGenreID.ToUpper().Contains(groupnoi) &&
								ch.portalID == int.Parse(@ActivePortal)         // string to int conversion - only channels for the current portal
								select ch;
						Results.AddRange(query.ToList<SC_Channel>());    // this is a list of all channels related to this group(s) (and potentially a few to filter out with the words below)
					}

				}   // this adds each list based upon how many groups are selected to the list to be linked to an XML group
			}
			FinalResults = Results;    // save the results so far - if I am just selecting groups this could be the final list

			// This is the second splitter where I select individual channels to be assigned to that XMLEPG
			// Now if I have selected some items in the middle column that means I am switching them to another country for XML EPG purposes
			if (GroupChanListBox.Value != null)
			{
				if (GroupChanListBox.Value.Length != 0)   // have some channels been selected in the middle splitter
														  // if so just handle these channels
				{
					FinalResults = new List<SC_Channel> { };	// clear the list
					string[] sel = GroupChanListBox.Value;  // this gives me a list of the selected rows.  I deleted the value section in the listbox in the razor file - why?
					if (sel.Length > 0)  // there are selected items
					{
						foreach (var selchan in sel)
						{
							foreach (var chan in Results)
							{
								if (chan.uniqueID == selchan)
								{
									FinalResults.Add(chan); // there are no previous final results
								}
							}

						}
					}
				}
				else FinalResults = Results;    // just use the results from the 1st splitter - nothing selected in 2md
			}

			GroupChanListBox.Value = Array.Empty<string>();     // clear any selections

			int added = 0;  // rows added in this next final step
			int updated = 0;
			string sqlquery;

			// this is the final list of channels that need updating
			// I now create a new line in SC_Channel_Data so that I can record the XMLEPG (or update if it exists)
			foreach (var chan in FinalResults)
			{
				chan.name.Replace("'", "\'");   // replace commas to avoid sqlite error
												// first check and see if there is already a row for this channel in SC_Channel_Data
				IQueryable<SC_Channel_Data> xmlquery2;   // default empty query
														 // this was the only way that I could get a count of records to work.  Raw async just did not work
				xmlquery2 = from xml in _IPTVcontext.SC_Channel_Data
							where xml.PortalID == chan.portalID &&
							xml.channelID == chan.channelID
							select xml;

				ChannelData = xmlquery2.ToList<SC_Channel_Data>();

				if (ChannelData.Count > 0)   // this channel is already present in the SC_Channel_Data
				{   // just update the country
					updated++;
					sqlquery = "UPDATE SC_Channel_Data SET XMLTimeZone = '" + TimeZone + "' WHERE channelID = '" + chan.channelID + "' AND PortalID = '" + chan.portalID + "'";
					int rowsins = await _IPTVcontext.Database.ExecuteSqlRawAsync(sqlquery);
				}
				else    // this channel is not present in the SC_Channel_Data so add it with the base info I have right now
				{
					added++;
					sqlquery = "INSERT INTO SC_Channel_Data VALUES (NULL, " +       // ID
								+chan.channelID + ", " +                            // channelID
								"\"" + chan.name + "\", " +                         // channelName      
								"0, " +                                             // XMLID
								"'', " +                                            // XMLIDName
								"'', " +                                            // XMLChanName
								"'" + TimeZone + "', " +                            // XMLTimeZone
								"0, " +                                             // Score
								"0, " +                                             // Genre
								+chan.portalID +                                    // PortalID
								")";
					int rowsins = await _IPTVcontext.Database.ExecuteSqlRawAsync(sqlquery);
				}
			}
			string msg = "Channels changes - added: " + added + " Updated: " + updated;
			ShowToast(msg);
			TimeZone = "";   // this resets which timezone has been selected - not sure if I like it
		}
	}
}

