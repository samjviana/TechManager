using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.PHYSICALMEMORY, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class PhysicalMemory : Model {
        [Key]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        public double capacity { get; set; }
    }
}
