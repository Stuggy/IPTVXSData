using IPTV.data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor.DropDowns;
using System.Linq;
//using System.Linq.Dynamic.Core;

namespace IPTVData.Pages
{
	public partial class Portals
	{
		public bool ShowCreate { get; set; }
		public bool ShowEdit { get; set; }
		private bool IsGuideCached { get; set; }
		private bool PortalIsActive { get; set; }
		public long SelectedPortalID { get; set; } // the ID selected
		private bool RequiresFreshToken { get; set; }

		// Portals
		//private IptvDataContext? _IPTVcontext;
		public Portal? NewPortal { get; set; }
		public Portal? PortalToUpdate { get; set; }
		public List<Portal>? OurPortals { get; set; }
		private bool onlyactiveportals;
		private long[] portalSelectedvalues = new long[] { };
		private SfListBox<long[], Portal> PortalListBoxObj;     // this doesn't work and stays null

		protected override async Task OnInitializedAsync()
		{
			ShowCreate = false;
			onlyactiveportals = true;
			await ShowPortals();
		}

		public void onActivePortalsChange(Microsoft.AspNetCore.Components.ChangeEventArgs args)		// show active portals toggle
		{
			ShowPortals();	// this refreshes the list
		}

		private async void onPortalChange(ListBoxChangeEventArgs<long[], Portal> args)
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			SelectedPortalID = args.Value[0];  // assign the first and only
			PortalToUpdate = _IPTVcontext.Portals.FirstOrDefault(x => x.ID == SelectedPortalID);
			IsGuideCached = PortalToUpdate.CacheGuideData == 1 ? true : false;
			PortalIsActive = PortalToUpdate.Active == 1 ? true : false;
			RequiresFreshToken = PortalToUpdate.RequiresFreshToken == 1 ? true : false;
		}

		public void CacheCheckboxChange(Microsoft.AspNetCore.Components.ChangeEventArgs args)	// guide cache checkbox has been toggled
		{
			// 			bool chceked = args.Equals(checked);
		}

		//------------------ Create! ----------------///
		public void ShowCreateForm()
		{
			ShowCreate = true;
			NewPortal = new Portal();
			NewPortal.GuideCacheTime = 24;
			NewPortal.EPGTimeShift = 0;
			NewPortal.Active = 1;
			NewPortal.RequiresFreshToken = 0;
		}

		public async Task CreateNewPortal()
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (NewPortal is not null)
			{   // if the Id is not unique to the database I get an error.  It is supposed to auto increment.
				NewPortal.CacheGuideData = IsGuideCached ? 1 : 0;
				NewPortal.Active = 1;
				_IPTVcontext?.Portals.Add(NewPortal);
				_IPTVcontext?.SaveChangesAsync();
			}

			ShowCreate = false;
			await ShowPortals();
		}

		//------------------ Read! ----------------///

		public async Task ShowPortals()
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				IQueryable<Portal> query;             // default empty query
				if(onlyactiveportals)
				{
					query = from p in _IPTVcontext.Portals
							where p.Active == 1
							select p;
				}
				else
				{
					query = from p in _IPTVcontext.Portals
							select p;
				}

				OurPortals = query.ToList<Portal>();

				PortalToUpdate = OurPortals.First();  // load the first portal as the one to update
				SelectedPortalID = PortalToUpdate.ID;
				IsGuideCached = PortalToUpdate.CacheGuideData == 1 ? true : false;
				PortalIsActive = PortalToUpdate.Active == 1 ? true : false;
				RequiresFreshToken = PortalToUpdate.RequiresFreshToken == 1 ? true : false;
			}
		}

		//------------------ Update! ----------------///

		public async Task ShowPortalEditForm(Portal ourPortal)
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				PortalToUpdate = _IPTVcontext.Portals.FirstOrDefault(x => x.ID == ourPortal.ID);
			}
		}

		public async Task UpdatePortal()
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				if (PortalToUpdate is not null)
				{
					PortalToUpdate.CacheGuideData = IsGuideCached ? 1 : 0;
					PortalToUpdate.Active = PortalIsActive ? 1 : 0;
					PortalToUpdate.RequiresFreshToken = RequiresFreshToken ? 1 : 0;
					_IPTVcontext.Portals.Update(PortalToUpdate);
					await _IPTVcontext.SaveChangesAsync();
				}
			}
		}

		//------------------ Delete! ----------------///

		public async Task DeletePortal(Portal ourPortal)
		{
			IptvDataContext? _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
			if (_IPTVcontext is not null)
			{
				if (ourPortal is not null) _IPTVcontext.Portals.Remove(ourPortal);
				await _IPTVcontext.SaveChangesAsync();
			}
			await ShowPortals();
		}
	}
}