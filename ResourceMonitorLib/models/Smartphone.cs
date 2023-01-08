using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.SMARTPHONE, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class Smartphone : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        [StringLength(16)]
        public string name { get; set; }
        [Required]
        virtual public smartphone.SPProcessor processor { get; set; }
        [Required]
        virtual public smartphone.SPRAM ram { get; set; }

        [Required]
        virtual public smartphone.SPOperatingSystem operatingsystem { get; set; }
        public bool status { get; set; }
        public string manufacturer { get; set; }
        public string model { get; set; }
        public string language { get; set; }
    }
}
