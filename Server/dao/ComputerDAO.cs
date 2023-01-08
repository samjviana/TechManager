using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ResourceMonitorLib.models;
using ResourceMonitorLib.utils;
using ResourceMonitorServer.utils;

namespace ResourceMonitorAPI.dao {
    class ComputerDAO : DAO<Computer> {
        public ComputerDAO() : base(typeof(Computer), DatabaseWrapper.Tables.COMPUTER) {
            
        }

        public Computer getByUUID(Guid uuid, bool include) {
            if (include) {
                using (var context = new DatabaseContext()) {
                    var table = context.computer.AsQueryable();
                    foreach (var property in typeof(Computer).GetProperties()) {
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
                    Computer computer = table.Where(c => c.uuid == uuid).FirstOrDefault();
                    if (computer == null) {
                        return computer;
                    }

                    var ramTable = context.ram.AsQueryable();
                    foreach (var property in typeof(RAM).GetProperties()) {
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
                            if (!isOverriden && property.Name != "readings") {
                                ramTable = ramTable.Include(property.Name);
                            }
                        }
                    }

                    computer.ram = ramTable.Where(r => r.uuid == computer.ram.uuid).FirstOrDefault();

                    var motherboardTable = context.motherboard.AsQueryable();
                    foreach (var property in typeof(Motherboard).GetProperties()) {
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
                                motherboardTable = motherboardTable.Include(property.Name);
                            }
                        }
                    }

                    computer.motherboard = motherboardTable.Where(m => m.uuid == computer.motherboard.uuid).FirstOrDefault();

                    return computer;
                }
            }
            using (var context = new DatabaseContext()) {
                Computer computer = context.computer.Where(
                    c => c.uuid == uuid
                ).FirstOrDefault();

                return computer;
            }
        }

        public Computer getById(int id) {
            using (var context = new DatabaseContext()) {
                Computer computer = context.computer.Include(
                    DatabaseWrapper.Tables.GPU + "s"
                ).Include(
                    DatabaseWrapper.Tables.STORAGE + "s"
                ).Include(
                    DatabaseWrapper.Tables.PROCESSOR + "s"
                ).Include(
                    DatabaseWrapper.Tables.MOTHERBOARD
                ).Include(
                    DatabaseWrapper.Tables.RAM
                ).Where(
                    c => c.id == id
                ).FirstOrDefault();

                return computer;
            }
        }
    }
}