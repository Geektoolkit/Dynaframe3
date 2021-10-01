using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;


namespace Dynaframe3.Models
{
    public class MediaDataContext : DbContext
    {
        public DbSet<MediaFile> MediaFiles{ get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                optionsBuilder.UseSqlite(
                    @"Data Source=/home/pi/Dynaframe/dynaframe.db");
            }
            else
            {
                optionsBuilder.UseSqlite(
                   @"Data Source=.\dynaframe.db");
            }
        }
    }
    public class MediaFile
    { 
        public int Id { get; set; }
        public string? Path { get; set; }
        public string? Directory { get; set; }
        public string? Type { get; set; }
        public string? Author { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public string? Tags { get; set; }
        public string? DateTaken { get; set; }

    }
}
