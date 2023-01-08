using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.models {
    [Table(DatabaseWrapper.Tables.COMPUTER, Schema = DatabaseWrapper.Schemas.RESOURCEMONITOR)]
    public class Computer : Model {
        [Key]
        [Index(IsUnique = true, Order = 0)]
        public override int id { get; set; }
        [Required]
        [StringLength(16)]
        public string name { get; set; }
        public bool partOfDomain{ get; set; }
        public string domain { get; set; }
        public string workGroup { get; set; }
        public string dnsName { get; set; }
        public string domainRole { get; set; }
        public string currentUser { get; set; }
        public string computerType { get; set; }
        public string manufacturer { get; set; }
        public string model { get; set; }
        public string powerState { get; set; }
        public string ownerContact { get; set; }
        public string ownerName { get; set; }
        public string supportContact { get; set; }
        public string systemType { get; set; }
        public string thermalState { get; set; }

        [Required]
        [Index(IsUnique = true, Order = 1)]
        public override Guid uuid { get; set; }
        [Required]
        virtual public ICollection<Storage> storages { get; set; }
        [Required]
        virtual public RAM ram { get; set; }
        [Required]
        virtual public ICollection<Processor> processors { get; set; }
        [Required]
        virtual public ICollection<GPU> gpus { get; set; }
        [Required]
        virtual public ICollection<InstalledSoftware> installedsoftwares { get; set; }
        [Required]
        virtual public Motherboard motherboard { get; set; }
        [Required]
        virtual public OperatingSystem operatingsystem { get; set; }
        public bool status { get; set; }
    }
}
