using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models.smartphone {
    [Table(DatabaseWrapper.Tables.SMARTPHONEREADINGS, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class SPReadings : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [NotMapped]
        public override Guid uuid { get; set; }
        [Required]
        public DateTime date { get; set; }
        [Required]
        virtual public Smartphone smartphone { get; set; }
        public string processor { get; set; }
        public string ram { get; set; }
    }
}
