using IPTV.data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.DropDowns.Internal;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Kanban.Internal;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.PivotView;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Xml;

// checkbox example https://blazor.syncfusion.com/demos/listbox/checkbox/
// the value field in the ListBoxFieldSettings is what drives whether a checkbox is checked or not
// so for the vehicle example the 
// <SfListBox Value="@value" DataSource="@vehicleData" is what defines where the checbox value list comes from
namespace IPTVData.Pages
{
    public partial class Demo
    {
        public class VehicleData
        {
            public string Text { get; set; }
            public string Id { get; set; }
			public static ObservableCollection<VehicleData> getListData()

            {
                ObservableCollection<VehicleData> data = new ObservableCollection<VehicleData>();
                data.Add(new VehicleData() { Text = "Hennessey Venom", Id = "Vehicle-01" });
                data.Add(new VehicleData() { Text = "Bugatti Chiron", Id = "Vehicle-02" });
                data.Add(new VehicleData() { Text = "Bugatti Veyron Super Sport", Id = "Vehicle-03" });
                data.Add(new VehicleData() { Text = "SSC Ultimate Aero", Id = "Vehicle-04" });
                data.Add(new VehicleData() { Text = "Koenigsegg CCR", Id = "Vehicle-05" });
                data.Add(new VehicleData() { Text = "McLaren F1", Id = "Vehicle-06" });
                data.Add(new VehicleData() { Text = "Aston Martin One- 77", Id = "Vehicle-07" });
                data.Add(new VehicleData() { Text = "Jaguar XJ220", Id = "Vehicle-08" });
                return data;
            }
        }

        public ObservableCollection<VehicleData> Vehicles { get; set; }
        private bool channelmode;
        private bool groupmode;
        private string editMode = "groups";
		private string[] value = new string[] { "Vehicle-02" }; // this will check the box

		protected override void OnInitialized()
        {
            Vehicles = VehicleData.getListData();
            Vehicles.Add(new VehicleData() { Text = "Ferrari LaFerrari", Id = "Vehicle-09" });  // this auto shows up
        }

        private void modifyData()
        {
            Vehicles.Add(new VehicleData() { Text = "Ferrari LaFerrari", Id = "Vehicle-09" });
        }

        private void OnClickHandler()   // For group button to see if I like using it
        {
            //if(args.GetType == )
            if (groupmode)
            {
                editMode = "groups";
                //OnChangeGroupMode();
                //string msg = "Groupmode";
                //ShowToast(msg);

            }
            if (channelmode)
            {
                editMode = "channels";
                //OnChangeGroupMode();
                //string msg = "channelmode";
                //ShowToast(msg);
            }
        }

		public void CheckABox()     // this forces a refresh of the grid
		{
			value = new string[] { "Vehicle-02", "Vehicle-04" };
		}

		public void ClearData()     // this forces a refresh of the grid
		{
			value = new string[] {  };
            Vehicles?.Clear();
		}




	}
}