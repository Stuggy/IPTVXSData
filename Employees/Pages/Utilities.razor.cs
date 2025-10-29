using IPTV.data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Kanban.Internal;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.PivotView;
using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.NetworkInformation;

//using System.Linq.Dynamic.Core;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Collections.ObjectModel;
using static IPTVData.Pages.Demo;

namespace IPTVData.Pages
{
	public partial class Utilities
	{
		public bool ShowEdit { get; set; }
		public bool ShowData { get; set; }

		//Overall
		private string editMode = "categories";                         // the toggle to define whether I am working on Categories or movies in Categories
		public string? ActivePortal;                                    // the active portal
																		//private IptvDataContext? _IPTVcontext;
		Utils _utils;

		//Listbox
		public ObservableCollection<String> LogItems { get; set; }      // ObservableCollection is the way to go to get auto refresh on data update

		//Toasts
		SfToast? ToastObj;
		private string ToastPosition = "Center";
		private string ToastContent = "no message";

		////////////////////////////////////////////////////////////////////////////////////
		// General

		protected override async Task OnInitializedAsync()
		{   // this is only called if override is specified
			ShowData = true;
			_utils = new Utils();
			// I need to find out the active portal
			ActivePortal = _utils.getPortalNumber();
			LogItems = new ObservableCollection<String>();
			LogItems.Add("Log started");

		}

		public async Task ResetXMLChannelsMap()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			// get the list of indexes (ID) of the XMLChannels table
			int count = 0;

			List<XMLChannel>? Chans;
			Chans = _IPTVcontext.XMLChannels.FromSql($"SELECT * FROM XMLChannels").ToList();

			foreach (var chan in Chans)
			{
				_IPTVcontext.Database.ExecuteSqlRaw("INSERT INTO XMLChannels_Map (XMLChan_ID, TimezoneSA, TimezoneUK, TimezoneUS) VALUES (@p0, 0, 0, 0)", chan.ID);

				count++;
			}
			await _IPTVcontext.SaveChangesAsync();

			string msg = count.ToString() + " rows added to XMLChannel_Map table";
			await ShowToast(msg);
		}

		public async Task ResetXMLChannels()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			_IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM XMLChannels");
			await _IPTVcontext.SaveChangesAsync();

			string msg = "XMLChannels table cleared";
			await ShowToast(msg);
		}

		public async Task RemoveOldPortalData()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			// get the list of indexes (ID) of the XMLChannels table
			int count = 0;
			List<Portal>? portals;
			portals = _IPTVcontext.Portals.FromSql($"SELECT * FROM Portals WHERE Active = 0").ToList();
			String log = "Inactive portal count: " + portals.Count.ToString();
			LogItems.Add(log);
			foreach (var portal in portals)
			{
				log = "Processing portal # : " + portal.ID.ToString();
				LogItems.Add(log);
				int rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_Channel_Data WHERE PortalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_Channel_Data items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_Channel_Disabled WHERE Portal = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_Channel_Disabled items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_Channels WHERE portalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_Channels items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_Channels_Cache WHERE portalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_Channel_Cache items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_EPG WHERE portalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_EPG items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_Groups WHERE PortalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_Groups_Custom items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_Groups_Custom WHERE PortalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_Groups_Custom items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_VOD_CAT_MOVIE WHERE PortalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_VOD_CAT_MOVIE items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_VOD_CAT_SERIES WHERE PortalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_VOD_CAT_SERIES items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				/*	Maybe keep for movie links
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_VOD_MOVIE WHERE portalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_VOD_MOVIE items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				*/
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_VOD_SERIES WHERE portalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_VOD_SERIES items removed: " + rows.ToString();
					LogItems.Add(log);
				}
				rows = _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM SC_VOD_SERIES_EPISODE WHERE portalID = @p0", portal.ID);
				if (rows != 0)
				{
					log = "     - SC_VOD_SERIES_EPISODE items removed: " + rows.ToString();
					LogItems.Add(log);
				}
			}
			LogItems.Add("End of cleanup");
		}

		private void onToggledEnabled(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{
			refresh();
		}

		private async Task ShowToast(string msg)
		{
			ToastObj.Content = msg;
			await this.ToastObj.ShowAsync();
		}

		private async void refresh()    // try to force refresh - this seems to mostly work
		{
			await InvokeAsync(StateHasChanged);
		}

		public async Task ResetMovies()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			_IPTVcontext.Database.ExecuteSqlRaw("UPDATE Settings SET value = 0 WHERE Name = 'VODMoviesLastUpdate'");
			await _IPTVcontext.SaveChangesAsync();

			string msg = "Movies reset - ready for update";
			await ShowToast(msg);
		}

		public async Task ResetSeries()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			_IPTVcontext.Database.ExecuteSqlRaw("UPDATE Settings SET value = 0 WHERE Name = 'VODSeriesLastUpdate'");
			await _IPTVcontext.SaveChangesAsync();

			string msg = "Series reset - ready for update";
			await ShowToast(msg);
		}

		public async Task ResetXMLEPG()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			_IPTVcontext.Database.ExecuteSqlRaw("UPDATE Settings SET value = 0 WHERE Name = 'XMLEPGLastUpdate'");
			await _IPTVcontext.SaveChangesAsync();

			string msg = "XML EPG reset - ready for update";
			await ShowToast(msg);
		}
		// end of general
		////////////////////////////////////////////////////////////////////////////////////

	}
}