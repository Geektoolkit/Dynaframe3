using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynaframe3.Shared
{
    public class Device
    {
        public int Id { get; set; }

        [Required]
        public string HostName { get; set; } = "";

        [Required]
        public string Ip { get; set; } = "";

        [Required]
        public int Port { get; set; }
        public DateTimeOffset LastCheckin { get; set; }

        public AppSettings AppSettings { get; set; } = new();
    }

    public class DeviceUpsert
    {
        public string HostName { get; set; } = "";
        public string Ip { get; set; } = "";
        public int Port { get; set; }
    }
}
