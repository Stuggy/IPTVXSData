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
//using System.Linq.Dynamic.Core;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Xml;

namespace IPTVData.Pages
{
	public partial class VOD_Movie
	{
		public bool ShowEdit { get; set; }
		public long CategoryEditingId { get; set; }

		//Overall
		private string editMode = "categories";                         // the toggle to define whether I am working on Categories or movies in Categories
		public string? ActivePortal;                                    // the active portal
		private IptvDataContext? _IPTVcontext;
		Utils? _utils;

		// Categories
		public SC_VOD_CAT_MOVIE? CategoryToUpdate { get; set; }
		public List<SC_VOD_CAT_MOVIE>? OurCategories { get; set; }
		SfListBox<string[], SC_VOD_CAT_MOVIE>? CategoryListBox;              // this enables calls to the listbox.  It is referenced in the Blazor file.  I'm not really using this now.
		private string[] CategoryCBvalues = new string[] { };              //  "89, 1025" example data.  This drives the checkbox to be checked or not.  First data is added on read from the database.
		private string[] CategoryCBEmpty = new string[] { };               // An empty list to use to not have anything selected
		public bool onlyshowselectedCategories;                             // show enabled Categories or not
		public string? selectedCroup;                                   // this is the Category ID and is used to show related movies

		//Movies
		public List<SC_VOD_MOVIE>? CategoryMovies { get; set; }
		public string? selectedMovie;                                 // this is the movie ID and is used to ... not sure yet
		SfListBox<string[], SC_VOD_MOVIE>? MovieListBox;

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
			//showCategoryboxes = true;
			await ShowCategories("");
			await ShowCategoryMovies("");
		}

		private void onToggledEnabled(Microsoft.AspNetCore.Components.ChangeEventArgs args)
		{
			ShowCategories("");
			ShowCategoryMovies("");
			//ShowAllmovies("");
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
				refresh();
			}

			if (editMode == "movies")
			{
				ShowCategories("");
				ShowCategoryMovies("");
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
			CategoryCBvalues = new string[] { };            // clear the list for a fresh start
			IQueryable<SC_VOD_CAT_MOVIE> query;             // default empty query
			if (_IPTVcontext is not null)
			{
				if (filter != null && filter != "")
				{
					// get the Categories that match the Category ID and also contain the filter text
					if (onlyshowselectedCategories)
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_MOVIE
								where gr.Name.ToUpper().Contains(filter) &&
								gr.Enabled == 1 &&
								gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
								select gr;
					}
					else
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_MOVIE
								where gr.Name.ToUpper().Contains(filter) &&
								gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
								select gr;
					}
					OurCategories = query.ToList<SC_VOD_CAT_MOVIE>();
					CategoryToUpdate = _IPTVcontext.SC_VOD_CAT_MOVIE.First();  // load the first Category as the one to update
				}
				else
				{
					if (onlyshowselectedCategories)
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_MOVIE
								where gr.PortalID == int.Parse(@ActivePortal) &&    // string to int conversion - only Categories for the current portal
								gr.Enabled == 1
								select gr;
					}
					else
					{
						query = from gr in _IPTVcontext.SC_VOD_CAT_MOVIE
								where gr.PortalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
								select gr;
					}

					OurCategories = query.ToList<SC_VOD_CAT_MOVIE>();
					if (OurCategories.Count != 0)
					{
						CategoryToUpdate = _IPTVcontext.SC_VOD_CAT_MOVIE.First();  // load the first Category as the one to update
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

		private void OnchangeCategory(ListBoxChangeEventArgs<string[], SC_VOD_CAT_MOVIE> args)    // Syncfusion version
		{   // this event fires even when you click on the name.  The checkbox gets checked or unchecked.
			if (editMode == "movies")           // I'm working on movies attached to Categories
			{
				selectedCroup = args.Value[0];  // assign the first and only
				ShowCategoryMovies("");         // show all movies that match the selectedCroup
			}
			else // this is the working on Categories option
			{
				// Here I reset all Categories to disabled and then add in the selected Categories below
				string query = "UPDATE SC_VOD_CAT_MOVIE SET Enabled = 0 WHERE PortalID = " + ActivePortal;
				_IPTVcontext.Database.ExecuteSqlRawAsync(query);

				if (args.Value != null && OurCategories != null) // make sure that it isn't null.  This happens when there are no Categories selected
				{
					CategoryCBvalues = args.Value;     // save the selected Categories into the checkbox array
					foreach (var item in args.Value)
					{
						foreach (var txt in CategoryCBvalues)
						{
							SC_VOD_CAT_MOVIE result = OurCategories.Find(x => x.SC_ID.ToString() == txt);
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

		public async Task DeleteCategory(SC_VOD_CAT_MOVIE ourCategory)
		{
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			if (_IPTVcontext is not null)
			{
				if (ourCategory is not null) _IPTVcontext.SC_VOD_CAT_MOVIE.Remove(ourCategory);
				await _IPTVcontext.SaveChangesAsync();
			}
			await ShowCategories("");
		}

		public async Task UpdateCategory()
		{
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();

			if (_IPTVcontext is not null)
			{
				if (CategoryToUpdate is not null) _IPTVcontext.SC_VOD_CAT_MOVIE.Update(CategoryToUpdate);
				await _IPTVcontext.SaveChangesAsync();
			}
			ShowEdit = false;
		}

		private void CategoriesInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the movies by the text
			args.Value = args.Value.ToUpper();
			ShowCategories(args.Value);
			// Here you can customize your code
		}

		// end of Categories
		////////////////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////////////////////
		//  SC_movies - second splitter
		public async Task ShowCategoryMovies(string filter)
		{
			_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
			filter = filter.ToUpper();  // change to filter to upper case to help with matching

			IQueryable<SC_VOD_MOVIE> query;   // default empty query
			if (_IPTVcontext is not null)
			{
				if (filter != null && filter != "")
				{
					// get the movies that match the Category ID and also contain the filter text
					query = from ch in _IPTVcontext.SC_VOD_MOVIE
							where ch.category_ID == selectedCroup &&
							ch.name.ToUpper().Contains(filter) &&
							ch.portalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
							select ch;
				}
				else
				{
					// get the movies that match the Category ID
					query = from ch in _IPTVcontext.SC_VOD_MOVIE
							where ch.category_ID == selectedCroup &&
							ch.portalID == int.Parse(@ActivePortal)     // string to int conversion - only Categories for the current portal
							select ch;
				}
				CategoryMovies = query.ToList<SC_VOD_MOVIE>();
			}
		}

		private void CategoriesMoviesInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the movies by the text
			ShowCategoryMovies(args.Value);
			// Here you can customize your code
		}

		// end of Movies
		////////////////////////////////////////////////////////////////////////////////////
		///
		////////////////////////////////////////////////////////////////////////////////////
		// third splitter

		private void AllmoviesInputHandler(InputEventArgs args)  // textbox
		{
			// so the textbox has changed and I now need to filter the movies by the text
			// 			ShowAllmovies(args.Value);
			// Here you can customize your code
		}

		// end of third splitter
		////////////////////////////////////////////////////////////////////////////////////
	}
}