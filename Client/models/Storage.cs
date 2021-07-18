using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitor.models {
    [Table(DatabaseWrapper.Tables.STORAGE)]
    class Storage {
        [Key]
        public int id { get; set; }
        [Required]
        public int number { get; set; }
        [Required]
        public string name { get; set; }
        public double size { get; set; }
        public string disks { get; set; }
    }
}
