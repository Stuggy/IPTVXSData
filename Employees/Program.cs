using IPTV.data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Syncfusion.Blazor;
// using SyncfusionBlazorApp5.Components;

var builder = WebApplication.CreateBuilder(args);   // this means it is a Blazor server and not client side app https://stackoverflow.com/questions/64544253/check-if-blazor-app-is-webassembly-or-server

//Register Syncfusion license
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWH5feHVURmNcWE13VkU=");


// these don't refer to another db.  It seems the local IPTV.db is always used no matter what the string is.
//var connectionString = builder.Configuration.GetConnectionString("ChannelDB");  // original working

//var connectionString = builder.Configuration.GetConnectionString("IPTVdbUserdata"); // IPTVdbUserdata ChannelDB
//var connectionString = builder.Configuration.GetConnectionString("Data Source=%IPTV_DB_Path%\\IPTV.db; Version=3"); // IPTVdbUserdata ChannelDB
//var connectionString = builder.Configuration.GetConnectionString("Data Source=C:\\Users\\Stu\\AppData\\Roaming\\Kodi\\userdata\\addon_data\\pvr.stalker.recorder\\IPTV.db; Version=3");
//var connectionString = "Data Source=C:\\Users\\Stu\\AppData\\Roaming\\Kodi\\userdata\\addon_data\\pvr.stalker.recorder\\IPTV.db; Version=3";
var connectionString = "hmm";   // the data appears even using this so the connectionString seems to have no impact

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSyncfusionBlazor();


// Database
//builder.Services.AddDbContextFactory<IptvDataContext>(options => options.UseSqlite(connectionString)); // connectionString
//builder.Services.AddDbContext<IptvDataContext>(options => options.UseSqlite(x => x.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")))); // another version - I'm not using it for now
//builder.Services.AddDbContext<IptvDataContext>(options => options.UseSqlite(options.GetConnectionString("DefaultConnection"))); // another version - I'm not using it for now
//builder.Services.AddDbContext<IptvDataContext>(options => options.UseSqlite("DefaultConnection"));
builder.Services.AddDbContextFactory<IptvDataContext>(options => options.UseSqlite(connectionString));  // original working

builder.Services.AddSyncfusionBlazor();   

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

string path2 = "%IPTV_DB_Path%\\TVDBXS-log.txt";  // change this environment variable if I want to change the location of the database file
string pathandfile = Environment.ExpandEnvironmentVariables(path2); // IPTV_DB_Path
// Ensure that the log file is empty 
//using (var fs = File.OpenWrite(pathandfile)) { fs.SetLength(0); }

if (System.IO.File.Exists(pathandfile))
{
    string filetodelete = pathandfile.Substring(0,pathandfile.Length - 4);
    filetodelete += "2023.txt";
    System.IO.File.Delete(filetodelete);
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(pathandfile,
        rollingInterval: RollingInterval.Year,
        rollOnFileSizeLimit: false)
    .CreateLogger();

// how to pause the app
//Console.WriteLine("Press any key to exit...");
//Console.ReadLine();
try
{
    // Your program here...
    const string name = "New program run ------------------------------------------------------------------>";
    Log.Information("From program.cs, {Name}!", name);
    //throw new InvalidOperationException("Oops...");
}
catch (Exception ex)
{
    Log.Error(ex, "Unhandled exception");
}
//finally
//{
//    await Log.CloseAndFlushAsync(); // ensure all logs written before app exits
//}

app.Run();
