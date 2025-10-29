using IPTV.data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Syncfusion.Blazor.Schedule;

namespace IPTVData.Pages
{
    public partial class EPG
    {
        //Overall
        Utils _utils;
        public string? ActivePortal;                                    // the active portal

        private DateTime CurrentDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, DateTime.Today.Hour, DateTime.Today.Minute, DateTime.Today.Second);
        private View CurrentView { get; set; } = View.TimelineWeek;
        private string[] groupData = new string[] { "channelName" };    // this is important - if it doesn't match a field name I get errors
        public List<RoomsData> roomsData { get; set; }
        public List<AppointmentData> appointmentData { get; set; }
        public List<SC_Channel_Data>? channelData { get; set; }
        public List<SC_EPG>? EPGData { get; set; }
        public List<epgData>? epgFromDB { get; set; }
        SfSchedule<epgData> ScheduleRef;

		protected override async Task OnInitializedAsync()
        {
            _utils = new Utils();
            // I need to find out the active portal
            ActivePortal = _utils.getPortalNumber();
            GetAppointmentData();
            GetRoomData();
            await GetChannels("BBC");
            epgFromDB = new List<epgData>();
            await GetEPG("");
        }

        public async Task OnDataBound(DataBoundEventArgs<epgData> args)
        {   // to scroll the grid to the current time
			var CurrentTime = DateTime.Now;
            //var Hours = CurrentTime.Hour - 4 < 10 ? '0' + (CurrentTime.Hour - 4).ToString() : (CurrentTime.Hour - 4).ToString();
            var Hours = (CurrentTime.Hour - 1).ToString();   // two hours before the current time
            var Minutes = CurrentTime.Minute.ToString();
            var Time = Hours + ":" + Minutes;
            await ScheduleRef.ScrollToAsync(Time, CurrentTime);
        }

        public async Task GoToCUrrentTime()
        {
			var CurrentTime = DateTime.Now;
			var Hours = (CurrentTime.Hour - 1).ToString();   // two hours before the current time
			var Minutes = CurrentTime.Minute.ToString();
			var Time = Hours + ":" + Minutes;
			await ScheduleRef.ScrollToAsync(Time, CurrentTime);
		}

        ////////////////////////////////////////////////////////////////////////////////////
        //  SC_Channels
        public async Task GetChannels(string filter)
        {   // This version gets all channels whether selected as active or not (that is done by the groups)
            IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
            filter = filter.ToUpper();      // change to filter to upper case to help with matching

            IQueryable<SC_Channel_Data> query;   // default empty query
            if (_IPTVcontext is not null)
            {
                // Filter channels if required
                if (filter != null && filter != "")     // there is some filter text
                {
                    // get the channels that match the filter text
                    query = from ch in _IPTVcontext.SC_Channel_Data
                            where ch.channelName.ToUpper().Contains(filter) &&
                            ch.PortalID == int.Parse(@ActivePortal)         // string to int conversion - only channels for the current portal
                            select ch;
                    channelData = query.ToList<SC_Channel_Data>();
                }
                else // no filter text
                {
                    query = from ch in _IPTVcontext.SC_Channel_Data
                            where ch.PortalID == int.Parse(@ActivePortal)
                            select ch;
                    channelData = query.ToList<SC_Channel_Data>();
                }
                // End of filtering channels
                ///////////////////////////////////////////////////////////
                // Now I have the channels or the filtered subset and I need to get the related XML info

            }
        }

        public async Task GetEPG(string filter)
        {   // This version gets all channels whether selected as active or not (that is done by the groups)
			epgFromDB = new List<epgData>();    // reset
			IptvDataContext _IPTVcontext = await IptvContextFactory.CreateDbContextAsync();
            filter = filter.ToUpper();      // change to filter to upper case to help with matching

            IQueryable<SC_EPG> query;   // default empty query
            if (_IPTVcontext is not null)
            {
                // Filter channels if required
                if (filter != null && filter != "")     // there is some filter text
                {
                    // get the channels that match the filter text
                    query = from ch in _IPTVcontext.SC_EPG
                            where ch.title.ToUpper().Contains(filter) &&
                            ch.portalID == int.Parse(@ActivePortal)         // string to int conversion - only channels for the current portal
                            select ch;
                    EPGData = query.ToList<SC_EPG>();
                }
                else // no filter text
                {
                    query = from ch in _IPTVcontext.SC_EPG
                            where ch.portalID == int.Parse(@ActivePortal)
                            select ch;
                    EPGData = query.ToList<SC_EPG>();
                }
                // End of filtering channels
                ///////////////////////////////////////////////////////////
                // Now I have the channels or the filtered subset and I need to get the related XML info
                foreach (var ch in EPGData) // now I have to create the datetime variables - to do that I have to create a separate list since the db table doesn't have a datetime field
                {
                    epgData e = new epgData(); // = new epgData();
                    e.ID = ch.ID;
                    e.uniqueBroadcastId = ch.uniqueBroadcastId;
                    e.title = ch.title;
                    e.channelNumber = ch.channelNumber;
                    e.plot = ch.plot;
                    e.portalID = ch.portalID;
                    e.category = ch.category;
                    e.categoryKodi = ch.categoryKodi;
                    e.episodeNumber = ch.episodeNumber;
                    e.episodeName = ch.episodeName;
                    //e.startTimeDT = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 2, 0, 0);
                    //e.endTimeDT = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 4, 0, 0);
                    e.startTimeDT = DateTimeOffset.FromUnixTimeSeconds(ch.startTime).DateTime;
                    e.endTimeDT = DateTimeOffset.FromUnixTimeSeconds(ch.endTime).DateTime;
                    epgFromDB.Add(e);
                }
            }
        }

        public partial class RoomsData
        {
            public string Name { get; set; }
            public int Id { get; set; }
            public string Color { get; set; }
            public int Capacity { get; set; }
            public string Type { get; set; }
        }

        public partial class epgData
        {
            public int ID { get; set; }
            public int? uniqueBroadcastId { get; set; }
            public string? title { get; set; }
            public int channelNumber { get; set; }
            public string? plot { get; set; }
            public int portalID { get; set; }
            public int? year { get; set; }
            public string? category { get; set; }
            public int? categoryKodi { get; set; }
            public int? episodeNumber { get; set; }
            public string? episodeName { get; set; }
            public DateTime? startTimeDT { get; set; }  // added to create this datetime after loading data
            public DateTime? endTimeDT { get; set; }    // added to create this datetime after loading data
        }

        public class AppointmentData
        {
            public int ID { get; set; }
            public string Subject { get; set; }
            public string Location { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Description { get; set; }
            public bool IsAllDay { get; set; }
            public string RecurrenceRule { get; set; }
            public string RecurrenceException { get; set; }
            public Nullable<int> RecurrenceID { get; set; }
        }

        public List<EPG.RoomsData> GetRoomData()
        {
            roomsData = new List<RoomsData> {
                new RoomsData { Name = "Jammy", Id = 1, Color = "#ea7a57", Capacity = 20, Type = "Conference" },
                new RoomsData { Name = "Tweety", Id = 2, Color = "#7fa900", Capacity = 7, Type = "Cabin" },
                new RoomsData { Name = "Nestle", Id = 3, Color = "#5978ee", Capacity = 5, Type = "Cabin" },
                new RoomsData { Name = "Phoenix", Id = 4, Color = "#fec200", Capacity = 15, Type = "Conference" },
                new RoomsData { Name = "Mission", Id = 5, Color = "#df5286", Capacity = 25, Type = "Conference" },
            };
            return roomsData;
        }
        public List<EPG.AppointmentData> GetAppointmentData()
        {
            appointmentData = new List<AppointmentData> {
                new AppointmentData { ID = 4501, Subject = "Paris", StartTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 2, 0, 0) , EndTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 4, 0, 0) },
                new AppointmentData { ID = 2, Subject = "Germany", StartTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 1, 0, 0) , EndTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 2, 30, 0) },
                new AppointmentData { ID = 3, Subject = "Spain", StartTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 12, 0, 0) , EndTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 16, 30, 0) },
                new AppointmentData { ID = 2, Subject = "Holland", StartTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 2, 30, 0) , EndTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 3, 30, 0) },
                new AppointmentData { ID = 4, Subject = "France", StartTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 16, 31, 0) , EndTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 17, 30, 0) }

            };
            return appointmentData;
        }

    }


}
