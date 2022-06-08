using Dynaframe3.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Dynaframe3.Server.Data
{
    public class ServerDbContext : DbContext
    {
        public virtual DbSet<AppSettings> AppSettings => Set<AppSettings>();

        public virtual DbSet<Device> Devices => Set<Device>();

        public ServerDbContext(DbContextOptions<ServerDbContext> options)
            : base(options) 
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSettings>()
                .ToTable("AppSettings")
                ;

            modelBuilder.Entity<AppSettings>()
                .Ignore(e => e.SearchSubDirectories)
                .HasKey(a => a.Id)
                ;

            modelBuilder.Entity<AppSettings>()
                .Property(a => a.SearchDirectories)
                .HasJsonConversion()
                ;

            modelBuilder.Entity<AppSettings>()
                .Property(a => a.CurrentPlayList)
                .HasJsonConversion()
                ;

            modelBuilder.Entity<AppSettings>()
                .Property(a => a.RemoteClients)
                .HasJsonConversion()
                ;

            modelBuilder.Entity<Device>()
                .ToTable("Devices")
                ;

            modelBuilder.Entity<Device>()
                .HasOne(d => d.AppSettings)
                .WithOne()
                .HasForeignKey("AppSettings")
                ;
                

            modelBuilder.Entity<Device>()
                .HasKey(d => d.Id)
                ;

            modelBuilder.Entity<Device>()
                .HasIndex(d => d.Ip)
                .HasDatabaseName("ix_Device_Ip")
                ;

            base.OnModelCreating(modelBuilder);
        }
    }

    internal static class PropertyExtensions
    {
        public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> property)
            where T : new()
        {

            var converter = new ValueConverter<T, string>
            (
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<T>(v) ?? new()
            );

            var comparer = new ValueComparer<T>
            (
                (l, r) => JsonConvert.SerializeObject(l) == JsonConvert.SerializeObject(r),
                v => v == null ? 0 : JsonConvert.SerializeObject(v).GetHashCode(),
                v => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v))!
            );

            property.HasConversion(converter);
            property.Metadata.SetValueConverter(converter);
            property.Metadata.SetValueComparer(comparer);
            property.HasColumnType("TEXT");

            return property;
        }
    }
}
