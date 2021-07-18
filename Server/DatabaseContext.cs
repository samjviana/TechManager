using ResourceMonitorLib.models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorLib.utils {
    class DatabaseContext : DbContext {
        public DbSet<Computer> Computers { get; set; }
        public DbSet<Processor> Processors { get; set; }
        public DbSet<Storage> Storages { get; set; }
        public DbSet<GPU> GPUs { get; set; }
        public DbSet<RAM> RAMs { get; set; }

        public DatabaseContext() : base("ResourceMonitorDB") {
            Database.SetInitializer<DatabaseContext>(new CreateDatabaseIfNotExists<DatabaseContext>());
        }
    }
}
