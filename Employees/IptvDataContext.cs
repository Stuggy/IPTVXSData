using Microsoft.EntityFrameworkCore;


// see https://www.youtube.com/watch?v=RtKsBvfTUlE
namespace IPTV.data
{
	public partial class IptvDataContext : DbContext
    {
        public DbSet<Channel> Channels { get; set; }    // it did have public virtual here - I removed it
        public DbSet<Portal> Portals { get; set; }
        public DbSet<SC_Groups> SC_Groups { get; set; }
		public DbSet<SC_Channel> SC_Channels { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<XMLChannel> XMLChannels { get; set; }
		public DbSet<SC_Channel_Data> SC_Channel_Data { get; set; }
		public DbSet<SC_VOD_CAT_MOVIE> SC_VOD_CAT_MOVIE { get; set; }
        public DbSet<SC_VOD_CAT_SERIES> SC_VOD_CAT_SERIES { get; set; }
        public DbSet<SC_VOD_MOVIE> SC_VOD_MOVIE { get; set; }
        public DbSet<SC_VOD_SERIES> SC_VOD_SERIES { get; set; }
        public DbSet<XMLRename> XMLRename { get; set; }
		public DbSet<SC_Groups_Custom> SC_Groups_Custom { get; set; }
        public DbSet<SC_EPG> SC_EPG { get; set; }
        public DbSet<XMLChannels_Map> XMLChannels_Map { get; set; }
		public DbSet<SC_Channel_Disabled> SC_Channel_Disabled { get; set; }

		public IptvDataContext()
        {
        }

        public IptvDataContext(DbContextOptions<IptvDataContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //IPTV_DB_Path was set to G:\IPTV\BlazorTVDB\Employees\Data
            string path2 = "Filename=%IPTV_DB_Path%\\IPTV.db";  // change this environment variable if I want to change the location of the database file
            string path = Environment.ExpandEnvironmentVariables(path2); // IPTV_DB_Path
            optionsBuilder.UseSqlite(path);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.Property(e => e.ID)
                    .ValueGeneratedOnAdd()
                    //                  .ValueGeneratedNever()   // this did not work with my autoincremented index - it caused a crash
                    .HasColumnName("ID");
            });

            OnModelCreatingPartial(modelBuilder);   // might need this twice

            modelBuilder.Entity<Portal>(entity =>
            {
                entity.Property(e => e.ID)
                    .ValueGeneratedOnAdd()
                    //                  .ValueGeneratedNever()   // this did not work with my autoincremented index - it caused a crash
                    .HasColumnName("ID");
            });

            OnModelCreatingPartial(modelBuilder);

            modelBuilder.Entity<SC_Groups>(entity =>
            {
                entity.Property(e => e.ID)
                    .ValueGeneratedOnAdd()
                    //                  .ValueGeneratedNever()   // this did not work with my autoincremented index - it caused a crash
                    .HasColumnName("ID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

