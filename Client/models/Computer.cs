using ResourceMonitor.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitor.models {
    [Table(DatabaseWrapper.Tables.COMPUTER)]
    class Computer {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public int id { get; set; }
        [Required]
        [Index(IsUnique = true, Order = 1)]
        [StringLength(16)]
        public string name { get; set; }
        [Required]
        virtual public ICollection<Storage> storages { get; set; }
        [Required]
        virtual public ICollection<RAM> rams { get; set; }
        [Required]
        virtual public ICollection<Processor> cpus { get; set; }
        [Required]
        virtual public ICollection<GPU> gpus { get; set; }
        public bool status { get; set; }
    }
}
