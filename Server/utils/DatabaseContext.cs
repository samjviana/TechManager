using Npgsql;
using ResourceMonitorLib.models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorServer.utils {
    public class DatabaseContext : DbContext {
        public DbSet<Computer> computer { get; set; }
        public DbSet<Processor> processor { get; set; }
        public DbSet<Storage> storage { get; set; }
        public DbSet<GPU> gpu { get; set; }
        public DbSet<RAM> ram { get; set; }
        public DbSet<PhysicalMemory> physicalmemory { get; set; }
        public DbSet<Motherboard> motherboard { get; set; }
        public DbSet<SuperIO> superio { get; set; }
        public DbSet<Readings> readings { get; set; }
        public DbSet<User> user { get; set; }
        public DbSet<ResourceMonitorLib.models.OperatingSystem> operatingsystem { get; set; }
        public DbSet<InstalledSoftware> installedsoftware { get; set; }
        public DbSet<Smartphone> smartphone { get; set; }
        public DbSet<ResourceMonitorLib.models.smartphone.SPProcessor> smartphoneprocessor { get; set; }
        public DbSet<ResourceMonitorLib.models.smartphone.SPRAM> smartphoneram { get; set; }
        public DbSet<ResourceMonitorLib.models.smartphone.SPOperatingSystem> smartphoneos { get; set; }
        public DbSet<ResourceMonitorLib.models.smartphone.SPReadings> smartphonereadings { get; set; }
        
        public DatabaseContext() : base("TechManagerDB") {
            Database.SetInitializer<DatabaseContext>(new CreateDatabaseIfNotExists<DatabaseContext>());
            //Database.SetInitializer<DatabaseContext>(new DropCreateDatabaseIfModelChanges<DatabaseContext>());
        }

        public override int SaveChanges() {
            var entries = ChangeTracker.Entries().Where(
                (entry) => {
                    if (entry.Entity is Model && (entry.State == EntityState.Added || entry.State == EntityState.Modified)) {
                        return true;
                    }
                    return false;
                }
            );

            foreach (var entry in entries) {
                if (entry.State == EntityState.Added) {
                    ((Model)entry.Entity).added = DateTime.Now;
                }

                ((Model)entry.Entity).update = DateTime.Now;
            }

            try {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException ex) {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }
        }
    }
    class NpgSqlConfiguration : DbConfiguration {
        public NpgSqlConfiguration() {
            var name = "Npgsql";

            SetProviderFactory(
                providerInvariantName: name,
                providerFactory: NpgsqlFactory.Instance
            );

            SetProviderServices(
                providerInvariantName: name,
                provider: NpgsqlServices.Instance
            );

            SetDefaultConnectionFactory(
                connectionFactory: new NpgsqlConnectionFactory()
            );
        }
    }
}
