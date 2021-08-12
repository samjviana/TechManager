using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.INSTALLEDSOFTWARE, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class InstalledSoftware : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        public string name { get; set; }
        public DateTime installDate { get; set; }
        public string publisher { get; set; }
        public string version { get; set; }
        public double size { get; set; }
    }
}
