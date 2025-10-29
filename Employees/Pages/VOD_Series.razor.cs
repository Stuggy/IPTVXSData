using IPTV.data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Notifications;
//using System.Linq.Dynamic.Core;

namespace IPTVData.Pages
{
	public partial class VOD_Series
	{
		public bool ShowEdit { get; set; }
		public long CategoryEditingId { get; set; }

		//Overall
		private string editMode = "categories";                         // the toggle to define whether I am working on Categories or seriess in Categories
		public string? ActivePortal;                                    // the active portal
		private IptvDataContext? _IPTVcontext;
		Utils? _utils;

		// Categories
		public SC_VOD_CAT_SERIES? CategoryToUpdate { get; set; }
		public List<SC_VOD_CAT_SERIES>? OurCategories { get; set; }
		SfListBox<string[], SC_VOD_CAT_SERIES>? CategoryListBox;        // this enables calls to the listbox.  It is referenced in the Blazor file.  I'm not really using this now.
		private string[] CategoryCBvalues = new string[] { };          //  "89, 1025" example data.  This drives the checkbox to be checked or not.  First data is added on read from the database.
		private string[] CategoryCBEmpty = new string[] { };               // An empty list to use to not have anything selected
		public bool onlyshowselectedCategories;                             // show enabled Categories or not
		public string? selectedCroup;                                       // this is the Category ID and is used to show related seriess

		//Series
		public List<SC_VOD_SERIES>? CategorySeries { get; set; }
		public string? selectedSeries;                                 // this is the series ID and is used to ... not sure yet
		SfListBox<string[], SC_VOD_SERIES>? ChanListBox;

		//Toasts
		SfToast? ToastObj;
		private string ToastPosition = "Center";
		private string ToastContent = "";

		////////////////////////////////////////////////////////////////////////////////////
		// General

		protected override async Task OnInitializedAsync()
		{
			_utils = new Utils();
			// I need to find out the active portal
			ActivePortal = _utils.getPortalNumber();
			await ShowCategories("");
			await ShowCategoryseriess("");
		}

		private void onToggledEnabled(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{
			ShowCategories("");
			ShowCategoryseriess("");
			//ShowAllseriess("");
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

		// end of general
		////////////////////////////////////////////////////////////////////////////////////

		private void OnChangeCategoryMode(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{
			if (editMode == "categories")
			{
				ShowCategories("");
				// 				ShowAllseriess("");
				refresh();
			}

			if (editMode == "series")
			{
				ShowCategories("");
				ShowCategoryseriess("");
				// 				ShowAllseriess("");
				refresh();
			}
		}

		public async Task ShowCategories(string filter)
		{
			IEnumerable<string> LoadedCategoryIDs2 = new List<string>();
			if (OurCategories != null)  // this solves the first refresh that doesn't otherwise work
			{
				OurCategories.Clear();  // trying to force refresh
			}

			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			CategoryCBvalues = new string[] { };        // clear the list for a fresh start
			IQueryable<SC_VOD_CAT_SERIES> query;           // default empty query
			if (_IPTVcontext is not null)
			{
				if (filter != null && filter != "")
				{
					// get the Categories that match the Category ID and also contain the filter text
					if (onlyshowselectedCategories)
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_SERIES
								where gr.Name.ToUpper().Contains(filter) &&
								gr.Enabled == 1 &&
								gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
								select gr;
					}
					else
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_SERIES
								where gr.Name.ToUpper().Contains(filter) &&
								gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
								select gr;
					}
					OurCategories = query.ToList<SC_VOD_CAT_SERIES>();
					CategoryToUpdate = _IPTVcontext.SC_VOD_CAT_SERIES.First();  // load the first Category as the one to update
				}
				else
				{
					if (onlyshowselectedCategories)
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_SERIES
								where gr.PortalID == int.Parse(@ActivePortal) &&    // string to int conversion - only Categories for the current portal
								gr.Enabled == 1
								select gr;
					}
					else
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_SERIES
								where gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
								select gr;
					}

					OurCategories = query.ToList<SC_VOD_CAT_SERIES>();
					if (OurCategories.Count != 0)
					{
						CategoryToUpdate = _IPTVcontext.SC_VOD_CAT_SERIES.First();  // load the first Category as the one to update
					}
				}

				foreach (var Category in OurCategories)    // If a Category is marked as enabled then set the checkbox
				{
					if (Category is not null)
					{
						if (Category.Enabled == 1)
						{
							LoadedCategoryIDs2 = LoadedCategoryIDs2.Concat(new string[] { Category.SC_ID });

						}
					}
				}
				CategoryCBvalues = LoadedCategoryIDs2.ToArray(); // this is needed or the checkboxes aren't checked
																 //                      refresh(); // no impact

			}
		}

		private void OnchangeCategory(ListBoxChangeEventArgs<string[], SC_VOD_CAT_SERIES> args)    // Syncfusion version
		{   // this event fires even when you click on the name.  The checkbox gets checked or unchecked.
			if (editMode == "series")             // I'm working on seriess attached to Categories
			{
				selectedCroup = args.Value[0];  // assign the first and only
				ShowCategoryseriess("");          // show all seriess that match the selectedCroup
			}
			else // this is the working on Categories option
			{
				// Here I reset all Categories to disabled and then add in the selected Categories below
				string query = "UPDATE SC_VOD_CAT_SERIES SET Enabled = 0 WHERE PortalID = " + ActivePortal;
				_IPTVcontext.Database.ExecuteSqlRawAsync(query);

				if (args.Value != null && OurCategories != null) // make sure that it isn't null.  This happens when there are no Categories selected
				{
					CategoryCBvalues = args.Value;     // save the selected Categories into the checkbox array
					foreach (var item in args.Value)
					{
						foreach (var txt in CategoryCBvalues)
						{
							SC_VOD_CAT_SERIES result = OurCategories.Find(x => x.SC_ID.ToString() == txt);
							if (result != null)
							{
								_IPTVcontext.Attach(result);     // from https://www.learnentityframeworkcore.com/dbcontext/modifying-data
								result.Enabled = 1;
								_IPTVcontext.Entry(result).Property("Enabled").IsModified = true;
							}
						}
					}
				}
				else    // no items selected
				{
					CategoryCBvalues = CategoryCBEmpty;
				}
				_IPTVcontext.SaveChanges();     // Now it updates all fields that changed and is way faster than doing it in the foreach loop
			}
		}

		public async Task DeleteCategory(SC_VOD_CAT_SERIES ourCategory)
		{
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			if (_IPTVcontext is not null)
			{
				if (ourCategory is not null) _IPTVcontext.SC_VOD_CAT_SERIES.Remove(ourCategory);
				await _IPTVcontext.SaveChangesAsync();
			}
			await ShowCategories("");
		}

		public async Task UpdateCategory()
		{
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				if (CategoryToUpdate is not null) _IPTVcontext.SC_VOD_CAT_SERIES.Update(CategoryToUpdate);
				await _IPTVcontext.SaveChangesAsync();
			}
			ShowEdit = false;
		}

		private void CategoriesInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the seriess by the text
			args.Value = args.Value.ToUpper();
			ShowCategories(args.Value);
			// Here you can customize your code
		}

		// end of Categories
		////////////////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////////////////////
		//  SC_seriess - second splitter
		public async Task ShowCategoryseriess(string filter)
		{
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			filter = filter.ToUpper();  // change to filter to upper case to help with matching

			IQueryable<SC_VOD_SERIES> query;   // default empty query
			if (_IPTVcontext is not null)
			{
				if (filter != null && filter != "")
				{
					// get the seriess that match the Category ID and also contain the filter text
					query = from ch in _IPTVcontext.SC_VOD_SERIES
							where ch.category_ID == selectedCroup &&
							ch.name.ToUpper().Contains(filter) &&
							ch.portalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
							select ch;
				}
				else
				{
					// get the seriess that match the Category ID
					query = from ch in _IPTVcontext.SC_VOD_SERIES
							where ch.category_ID == selectedCroup &&
							ch.portalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
							select ch;
				}
				CategorySeries = query.ToList<SC_VOD_SERIES>();
			}
		}

		private void CategoriesSeriesInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the series by the text
			ShowCategoryseriess(args.Value);
			// Here you can customize your code
		}

		// end of seriess
		////////////////////////////////////////////////////////////////////////////////////
		///
		////////////////////////////////////////////////////////////////////////////////////
		// third splitter

		private void AllseriessInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the series by the text
			// 			ShowAllseriess(args.Value);
			// Here you can customize your code
		}

		// end of third splitter
		////////////////////////////////////////////////////////////////////////////////////
	}
}