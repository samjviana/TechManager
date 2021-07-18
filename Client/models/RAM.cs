using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitor.models {
    [Table(DatabaseWrapper.Tables.RAM)]
    class RAM {
        [Key]
        public int id { get; set; }
        [Required]
        public string name { get; set; }
        public double capacity { get; set; }
    }
}
