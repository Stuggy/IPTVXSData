using IPTV.data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Grids.Internal;

namespace IPTVData.Pages
{
    public partial class Settings
    {
        //public long ChannelEditingId { get; set; }

        // Channels
        private IptvDataContext? _IPTVcontext;
        public Setting? SettingToUpdate { get; set; }
        public List<Setting>? GridData { get; set; }
        SfGrid<Setting>? Grid;
        public List<int>? SelectedRowIndexes { get; set; }
        public int SelectedRow;     // used for update operation

        protected override async Task OnInitializedAsync()
        {
            await LoadSettings();
        }

        public async Task LoadSettings()
        {
            _IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();

            if (_IPTVcontext is not null)
            {
                GridData = await _IPTVcontext.Settings.ToListAsync();
                GridData = GridData.OrderBy(x => x.name).ToList();
            }
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
                _IPTVcontext.Database.ExecuteSqlRaw("DELETE FROM Settings WHERE ID = @p0", Setting_ID);
                await _IPTVcontext.SaveChangesAsync();
            }
            await LoadSettings();
        }

        public async Task GetSelectedRecords(RowSelectEventArgs<Setting> args)
        {
            // this function works and is used in the update code below
            SelectedRowIndexes = await Grid.GetSelectedRowIndexes();
            SelectedRow = SelectedRowIndexes.FirstOrDefault();
            StateHasChanged();
        }

        public void ActionComplete(ActionEventArgs<Setting> args)
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
                SettingToUpdate = GridData.ElementAt(SelectedRow);
                // Triggers once save operation completes
                if (SettingToUpdate is not null) _IPTVcontext.Settings.Update(SettingToUpdate);
                _IPTVcontext.SaveChangesAsync();
                LoadSettings();     // this works to refresh the display
            }
            else if (args.RequestType == Syncfusion.Blazor.Grids.Action.Delete)
            {
                Delete(args.Data.ID);
            }
        }
    }
}