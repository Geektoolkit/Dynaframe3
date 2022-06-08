using Microsoft.EntityFrameworkCore;
using System;


namespace Dynaframe3.Models
{
    public class MediaDataContext : DbContext
    {
        public DbSet<MediaFile> MediaFiles { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(
                $"Data Source={AppDomain.CurrentDomain.BaseDirectory}dynaframe.db");
        }
    }
    public class MediaFile
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Directory { get; set; }
        public string Type { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
        public string Tags { get; set; }
        public string DateTaken { get; set; }

    }
}
