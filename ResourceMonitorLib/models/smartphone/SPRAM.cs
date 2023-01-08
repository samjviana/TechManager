﻿using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models.smartphone {
    [Table(DatabaseWrapper.Tables.SMARTPHONERAM, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class SPRAM : Model {
        [Key]
        public override int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        public double total { get; set; }
        [NotMapped]
        public Sensors sensors { get; set; }

        public class Sensors {
            public double free { get; set; }
            public double used { get; set; }
            public double load { get; set; }
        }
    }
}
