using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.STORAGE, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class Storage : Model {
        [Key]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        public int index { get; set; }
        [Required]
        public string name { get; set; }
        public double size { get; set; }
        public string disks { get; set; }
        public double read { get; set; }
        public double write { get; set; }
        [NotMapped]
        public Sensors sensors { get; set; }

        public class Sensors {
            public double temperature { get; set; }
            public double load { get; set; }
            public double read { get; set; }
            public double write { get; set; }
        }

        public Storage() {
            name = "Unknown";
            id = 0;
        }
    }
}
