using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitor.models {
    [Table(DatabaseWrapper.Tables.PROCESSOR)]
    class Processor {
        [Key]
        public int id { get; set; }
        [Required]
        public int number { get; set; }
        [Required]
        public string name { get; set; }
        public double temperature { get; set; }
        public double clock { get; set; }
        public double power { get; set; }
        public int cores { get; set; }
    }
}
