using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.GPU, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class GPU : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        public int number { get; set; }
        [Required]
        public string name { get; set; }
        public double temperature { get; set; }
        public double coreClock { get; set; }
        public double memoryClock { get; set; }
        public double power { get; set; }
        [NotMapped]
        public Sensors sensors { get; set; }

        public class Sensors {
            public double temperature { get; set; }
            public double load { get; set; }
            public double coreClock { get; set; }
            public double memoryClock { get; set; }
            public double power { get; set; }
        }
    }
}
