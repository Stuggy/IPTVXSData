using IPTV.data;
using IPTVData.Pages;
using Microsoft.EntityFrameworkCore;


// namespace IPTV.data
// {
    public partial class Utils
    {
        private IptvDataContext? _IPTVcontext;
        private readonly IDbContextFactory<IptvDataContext> ContextFactory;
        private readonly IptvDataContext _context = new IptvDataContext();

        public string getPortalNumber()
        {
            IPTV.data.Setting portal = _context.Settings.FromSql($"SELECT * FROM Settings WHERE Name = 'ActivePortal'").FirstOrDefault();
            return portal.value;
        }

    }
// }

/*
_IPTVcontext ??= await IptvContextFactory.CreateDbContextAsync();
            IQueryable data = _IPTVcontext.Database
                .SqlQuery<string>($"SELECT * FROM Settings WHERE Name = 'ActivePortal'");
            Settings.FirstOrDefault(facility => facility.Employee.Age == userAge);
            ActivePortal = data.FirstorDefault();
*/