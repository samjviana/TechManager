using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.READINGS, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class Readings : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [NotMapped]
        public override Guid uuid { get; set; }
        [Required]
        public DateTime date { get; set; }
        [Required]
        virtual public Computer computer { get; set; }
        public string processors { get; set; }
        public string gpus { get; set; }
        public string ram { get; set; }
        public string storages { get; set; }
    }
}
