using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.USER, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class User : Model {
        [Key]
        public override int id { get; set; }
        public override Guid uuid { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 0)]
        public string username { get; set; }
        [Required]
        public string password { get; set; }
    }
}
