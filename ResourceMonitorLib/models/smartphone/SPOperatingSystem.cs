using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models.smartphone {
    [Table(DatabaseWrapper.Tables.SMARTPHONEOS, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class SPOperatingSystem : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        public string name { get; set; }
        public string version { get; set; }
        public string sdkVersion { get; set; }
    }
}
