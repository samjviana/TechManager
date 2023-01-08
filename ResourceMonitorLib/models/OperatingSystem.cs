using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.OPERATINGSYSTEM, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class OperatingSystem : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        public string name { get; set; }
        public string version { get; set; }
        public string build { get; set; }
        public string manufacturer { get; set; }
        public string architecture { get; set; }
        public string serialKey { get; set; }
        public string serialNumber { get; set; }
        public string status { get; set; }
        public DateTime installDate { get; set; }
        public string language { get; set; }
        public string country { get; set; }
        public int codePage { get; set; }
        public string bootDevice { get; set; }
        public string systemPartition { get; set; }
        public string installPath { get; set; }
    }
}
