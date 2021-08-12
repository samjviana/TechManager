using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models.smartphone {
    [Table(DatabaseWrapper.Tables.SMARTPHONEPROCESSOR, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class SPProcessor : Model {
        [Key]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        public string name { get; set; }
        public double temperature { get; set; }
        public double clock { get; set; }
        public int cores { get; set; }
        [NotMapped]
        public Sensors sensors { get; set; }
        public string manufacturer { get; set; }
        public string architecture { get; set; }

        public class Sensors {
            public double temperature { get; set; }
            public double clock { get; set; }
        }
    }

}
