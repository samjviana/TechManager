using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ResourceMonitor.utils;
using ResourceMonitorLib.models;
using ResourceMonitorLib.utils;

namespace ResourceMonitorAPI.dao {
    class SmartphoneDAO : DAO<Smartphone> {
        public SmartphoneDAO() : base(typeof(Smartphone), DatabaseWrapper.Tables.SMARTPHONE) {
            
        }

        public Smartphone getByUUID(Guid uuid, bool include) {
            if (include) {
                using (var context = new DatabaseContext()) {
                    var table = context.smartphone.AsQueryable();
                    foreach (var property in typeof(Smartphone).GetProperties()) {
                        var isVirtual = property.GetGetMethod().IsVirtual;
                        var isOverriden = false;
                        if (property.GetGetMethod(false).GetBaseDefinition() != property.GetGetMethod(false)) {
                            isOverriden = true;
                        }
                        var isModel = false;
                        if (property.PropertyType.BaseType != null) {
                            isModel = property.PropertyType.BaseType.Name.ToString() == "Model";
                        }
                        if (isVirtual || isModel) {
                            if (!isOverriden) {
                                table = table.Include(property.Name);
                            }
                        }
                    }
                    Smartphone smartphone = table.Where(c => c.uuid == uuid).FirstOrDefault();
                    if (smartphone == null) {
                        return smartphone;
                    }

                    return smartphone;
                }
            }
            using (var context = new DatabaseContext()) {
                Smartphone smartphone = context.smartphone.Where(
                    c => c.uuid == uuid
                ).FirstOrDefault();

                return smartphone;
            }
        }

        public Smartphone getById(int id) {
            using (var context = new DatabaseContext()) {
                Smartphone smartphone = context.smartphone.Include(
                    s => s.ram
                ).Include(
                    s => s.processor
                ).Include(
                    s => s.operatingsystem
                ).Where(
                    s => s.id == id
                ).FirstOrDefault();

                return smartphone;
            }
        }
    }
}