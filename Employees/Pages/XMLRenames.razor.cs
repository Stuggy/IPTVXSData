using IPTV.data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor.Grids;

namespace IPTVData.Pages
{
    public partial class XMLRenames
    {
        // XML
        private IptvDataContext? _IPTVcontext;
        public XMLRename? XMLEntryToUpdate { get; set; }
        public List<XMLRename>? GridData { get; set; }
        SfGrid<XMLRename>? Grid;
        public List<int>? SelectedRowIndexes { get; set; }
        public int SelectedRow;     // used for update operation

        protected override async Task OnInitializedAsync()
        {
            await ShowXMLData();
        }

        public async Task ShowXMLData()
        {
            _IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
            if (_IPTVcontext is not null)
            {
                GridData = await _IPTVcontext.XMLRename.ToListAsync();
            }
            GridData = GridData.OrderBy(x => x.BetterRename).ToList();
        }

        public async Task Add()
        {
            // there appears to not be a need for me to actually code the addition of the record
        }

        public async Task Update()
        {
            // there appears to not be a need for me to actually code the update of the record
            //SettingToUpdate = GridData.ElementAt(SelectedRow);
            //await Grid.UpdateRow(SelectedRow, SettingToUpdate);
        }

        public async Task Delete(long Setting_ID)
        {   // I might also be able to use the update code to achieve the same thing
            _IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
            if (_IPTVcontext is not null)
            {
                _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM XMLRename WHERE ID = @p0", Setting_ID);
                await _IPTVcontext.SaveChangesAsync();
            }
            await ShowXMLData();
        }

        public async Task GetSelectedRecords(RowSelectEventArgs<XMLRename> args)
        {
            // this function works and is used in the update code below
            SelectedRowIndexes = await Grid.GetSelectedRowIndexes();
            SelectedRow = SelectedRowIndexes.FirstOrDefault();
            StateHasChanged();
        }

        public void ActionComplete(ActionEventArgs<XMLRename> args)
        {
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
                XMLEntryToUpdate = GridData.ElementAt(SelectedRow);
                // Triggers once save operation completes
                if (XMLEntryToUpdate is not null) _IPTVcontext.XMLRename.Update(XMLEntryToUpdate);
                _IPTVcontext.SaveChangesAsync();
            }
            else if (args.RequestType == Syncfusion.Blazor.Grids.Action.Delete)
            {
                Delete(args.Data.ID);
            }
        }
    }
}