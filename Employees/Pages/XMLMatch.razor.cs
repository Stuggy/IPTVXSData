using FuzzySharp;
using IPTV.data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Dynamic;
using Serilog;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using Syncfusion.Blazor.Buttons;

// Checkboxes on data grid https://www.syncfusion.com/forums/154811/blazor-column-checkboxes-in-datagrid

/*

The idea here is to have the XML EPG by time zone instead of each separate XML like US locals, Peacock, Supersport etc
I can then assign different time zones to some channels if they have channels from another zone.  An example is supersport
that is listed in UK channels
Also, it helps with the US where I might have multiple XML sources to try and provide a full EPG.  I just specify which timezone(s)
the XML applies to and then the fuzzy matching for those channels takes all of those XML channels into account.

So I use this screen and the fuzzy matching screen and that is all I need.
 
 
 */


namespace IPTVData.Pages
{
	public partial class XMLMatch
	{
		//General
		public string? ActivePortal;                        // the active
		SfGrid<XMLClass> Grid { get; set; }
		Utils _utils;
		bool SAischecked;                           // are the selected rows checked
		bool UKischecked;
		bool USischecked;

		//XML
		public List<XMLClass>? JoinedXML { get; set; }

		//Toasts
		SfToast? ToastObj;
		private string ToastPosition = "Center";

		//Editing
		public List<XMLClass>? SelectedRecords { get; set; }    // the currently selected records
		public List<XMLClass>? PriorSelectedRecords { get; set; }    // the previous selected records
																//public List<int>? CurrentSelectedRows { get; set; }
		public int SelectedRow;								// used for update operation
		public XMLClass? SettingToUpdate { get; set; }

		////////////////////////////////////////////////////////////////////////////////////
		// General
		protected override async Task OnInitializedAsync()
		{
			_utils = new Utils();
			// I need to find out the active portal
			ActivePortal = _utils.getPortalNumber();
			JoinedXML = new List<XMLClass>();
			await GetXML("");
		}
		public async Task OnDataBound(object args)
		{   // to try and set the rows that are selected
			//await Grid.SelectRowsAsync(CurrentSelectedRows?.ToArray());
			List<int> rowstocheck = new List<int>();
			for (int i = 0; i < PriorSelectedRecords?.Count; i++)
			{
				XMLClass? XML = PriorSelectedRecords[i];
				var index = await Grid.GetRowIndexByPrimaryKeyAsync(XML.channelID);
				//Log.Information("Prior selected record index: {index}", index); 
				rowstocheck.Add(index);
			}
			await Grid.SelectRowsAsync(rowstocheck.ToArray());
		}

		public async Task OnRowSelected(RowSelectEventArgs<XMLClass> args)	// this is called on RowSelected so probably would only be as each row is selected
		{   // If 3 rows are selected this is called 3 times
			args.PreventRender = true; //without this, you may see noticeable delay in selection with 75 rows in grid.
			// It only seems to return one row at a time.  However, if I hold shift and select I do get multiple rows
			SelectedRecords = await Grid.GetSelectedRecordsAsync();	// this gets the actual XMLClass record that is selected even if filtered
			// it keeps updating the list of selected records until they are all selected e.g. four rows
			//Log.Information("Record selected count: {count}", SelectedRecords.Count); // this count is correct when selecting with the mouse
			//StateHasChanged();

		}
		public async Task SACheckALL()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if(SelectedRecords?.Count > 0)
			{
				foreach (var XMLChannels_Map in SelectedRecords)
				{
					_IPTVcontext.Database.ExecuteSqlRaw("UPDATE XMLChannels_Map set TimeZoneSA = 1 WHERE ID = @p0", XMLChannels_Map.ID);
					await _IPTVcontext.SaveChangesAsync();
				}
			}
			PriorSelectedRecords = SelectedRecords;
			ClearList();	
			GetXML("");     
		}

		public async Task SAClearALL()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (SelectedRecords?.Count > 0)
			{
				foreach (var XMLChannels_Map in SelectedRecords)
				{
					_IPTVcontext.Database.ExecuteSqlRaw("UPDATE XMLChannels_Map set TimeZoneSA = 0 WHERE ID = @p0", XMLChannels_Map.ID);
					await _IPTVcontext.SaveChangesAsync();
				}
			}
			PriorSelectedRecords = SelectedRecords;
			ClearList();
			GetXML("");
		}

		public async Task UKCheckALL()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (SelectedRecords?.Count > 0)
			{
				foreach (var XMLChannels_Map in SelectedRecords)
				{
					_IPTVcontext.Database.ExecuteSqlRaw("UPDATE XMLChannels_Map set TimeZoneUK = 1 WHERE ID = @p0", XMLChannels_Map.ID);
					await _IPTVcontext.SaveChangesAsync();
				}
			}
			PriorSelectedRecords = SelectedRecords;
			ClearList();
			GetXML("");
		}

		public async Task UKClearALL()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (SelectedRecords?.Count > 0)
			{
				foreach (var XMLChannels_Map in SelectedRecords)
				{
					_IPTVcontext.Database.ExecuteSqlRaw("UPDATE XMLChannels_Map set TimeZoneUK = 0 WHERE ID = @p0", XMLChannels_Map.ID);
					await _IPTVcontext.SaveChangesAsync();
				}
			}
			PriorSelectedRecords = SelectedRecords;
			ClearList();
			GetXML("");
		}

		public async Task USCheckALL()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (SelectedRecords?.Count > 0)
			{
				foreach (var XMLChannels_Map in SelectedRecords)
				{
					_IPTVcontext.Database.ExecuteSqlRaw("UPDATE XMLChannels_Map set TimeZoneUS = 1 WHERE ID = @p0", XMLChannels_Map.ID);
					await _IPTVcontext.SaveChangesAsync();
				}
			}
			PriorSelectedRecords = SelectedRecords;
			ClearList();
			await GetXML("");
		}

		public async Task USClearALL()
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (SelectedRecords?.Count > 0)
			{
				foreach (var XMLChannels_Map in SelectedRecords)
				{
					_IPTVcontext.Database.ExecuteSqlRaw("UPDATE XMLChannels_Map set TimeZoneUS = 0 WHERE ID = @p0", XMLChannels_Map.ID);
					await _IPTVcontext.SaveChangesAsync();
				}
			}
			PriorSelectedRecords = SelectedRecords;
			ClearList();
			await GetXML("");
		}

		private void onChangeSA(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{	//onChange Event triggered
			if (args.Value is bool)
			{
				bool ischecked = (bool)args.Value;
				if (ischecked) SACheckALL();
				else SAClearALL();
			}
		}

		private void onChangeUK(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{	//onChange Event triggered
			if (args.Value is bool)
			{
				bool ischecked = (bool)args.Value;
				if (ischecked) UKCheckALL();
				else UKClearALL();
			}
		}

		private void onChangeUS(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{	//onChange Event triggered
			if (args.Value is bool)
			{
				bool ischecked = (bool)args.Value;
				if (ischecked) USCheckALL();
				else USClearALL();
			}
		}

		public async Task ActionComplete(ActionEventArgs<XMLClass> args)
		{
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (args.RequestType == Syncfusion.Blazor.Grids.Action.BeginEdit)
			{
				// Triggers once editing operation completes
			}
			else if (args.RequestType == Syncfusion.Blazor.Grids.Action.Add)
			{
				// Triggers once add operation completes
			}
			else if (args.RequestType == Syncfusion.Blazor.Grids.Action.Cancel)
			{
				// Triggers once cancel operation completes
			}
			else if (args.RequestType == Syncfusion.Blazor.Grids.Action.Save)
			{
				SettingToUpdate = JoinedXML.ElementAt(SelectedRow);
				// Triggers once save operation completes
				if (SettingToUpdate is not null) await Update(SettingToUpdate);
			}
			else if (args.RequestType == Syncfusion.Blazor.Grids.Action.Delete)
			{
				await Delete(args.Data.channelID);
			}
		}

		public async Task Delete(long XMLClassID)
		{   
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			if (_IPTVcontext is not null)
			{
				_IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM XMLChannels_Map WHERE ID = @p0", XMLClassID);
				await _IPTVcontext.SaveChangesAsync();
			}
		}

		public async Task Update(XMLClass XMLClassID)
		{   
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			if (_IPTVcontext is not null)
			{
				_IPTVcontext.Database.ExecuteSqlRaw("UPDATE XMLChannels_Map set TimeZoneSA = @p1, TimeZoneUk = @p2, TimeZoneUS = @p3 WHERE ID = @p0", XMLClassID.ID, XMLClassID.TimeZoneSA, XMLClassID.TimeZoneUK, XMLClassID.TimeZoneUS);
				await _IPTVcontext.SaveChangesAsync();
			}
		}

		////////////////////////////////////////////////////////////////////////////////////
		//  XML
		public async Task GetXML(string filter) // this filters the XML Names and shows the related channels

		{
			JoinedXML = new List<XMLClass> { };
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			// join query
			var queryL =
			   from XMLMap in _IPTVcontext.XMLChannels_Map
			   join XMLChannels in _IPTVcontext.XMLChannels on XMLMap.XMLChan_ID equals XMLChannels.ID
			   // use the p = to avoid CS0833 - it renames the XMLChannels.ID
			   select new { p = XMLChannels.ID, XMLChannels.Name, XMLMap.ID, XMLMap.TimeZoneSA, XMLMap.TimeZoneUK, XMLMap.TimeZoneUS, XMLChannels.Source };

			// Execute the query and output the results
			var result = queryL.ToList();

			foreach (var XMLChannels_Map in result)
			{
				XMLClass c = new XMLClass();
				c.ID = XMLChannels_Map.ID;
				c.channelID = XMLChannels_Map.p;
				c.ChannelName = XMLChannels_Map.Name;
				if (XMLChannels_Map.TimeZoneSA == 1)
				{
					c.TimeZoneSA = true;
				}
				else c.TimeZoneSA = false;
				if (XMLChannels_Map.TimeZoneUK == 1)
				{
					c.TimeZoneUK = true;
				}
				else c.TimeZoneUK = false;
				if(XMLChannels_Map.TimeZoneUS == 1) {
					c.TimeZoneUS = true;
				}
				else c.TimeZoneUS= false;
				c.Source = XMLChannels_Map.Source;
				JoinedXML.Add(c);	
			}
		}

		public void ClearList()     // this forces a refresh of the grid
		{
			Grid.Refresh();
		}

		////////////////////////////////////////////////////////////////////////////////////
		// Utilities
		private async Task ShowToast(string msg)
		{
			ToastObj.Content = msg;
			await this.ToastObj.ShowAsync();
		}
		public void ActionFailureHandler(FailureEventArgs args)
		{
			// Here, you can get the error details in the args.
			int x = 1;
		}

		// I'm not using these two but they are useful for future reference
		public bool BoolValue
		{
			get { return IntValue == 1; }
			set { IntValue = value ? 1 : 0; }
		}

		public int IntValue { get; set; }
		///////////////////////////////////////////////////////////////////
	}

	public class XMLClass
	{
		public int ID { get; set; }
		public int channelID { get; set; }
		public string? ChannelName { get; set; }
		public bool TimeZoneSA { get; set; }
		public bool TimeZoneUK { get; set; }
		public bool TimeZoneUS { get; set; }
		public string? Source { get; set; }
	}
}