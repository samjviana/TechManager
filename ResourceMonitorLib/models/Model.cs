using System;
using System.ComponentModel.DataAnnotations;

namespace ResourceMonitorLib.models {
    public abstract class Model {
        public abstract int id { get; set; }
        public abstract Guid uuid { get; set; }
        [Required]
        public DateTime added { get; set; }
        [Required]
        public DateTime update { get; set; }
    }
}