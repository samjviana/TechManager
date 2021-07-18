using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using ResourceMonitorLib.utils;

namespace ResourceMonitorAPI.dao {
    abstract class DAO<T> where T : class {
        private Type type;
        private String tableName;

        public DAO(Type type, String tableName) {
            this.type = type;
            this.tableName = tableName;
        }

        public List<T> get() {
            try {
                using (var context = new DatabaseContext()) {
                    var table = (DbSet<T>)typeof(DatabaseContext).GetProperty(tableName).GetValue(context);
                    var queryable = table.AsQueryable();
                    foreach (var property in this.type.GetProperties()) {
                        var isVirtual = property.GetGetMethod().IsVirtual;
                        if (isVirtual) {
                            queryable = queryable.Include(property.Name);
                        }
                    }
                    return queryable.ToList<T>();
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        public T get(int id) {
            try {
                using (var context = new DatabaseContext()) {
                    var table = (DbSet<T>)typeof(DatabaseContext).GetProperty(tableName).GetValue(context);
                    return table.Find(id);
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        public T add(T t) {
            try {
                using (var context = new DatabaseContext()) {
                    var table = (DbSet<T>)typeof(DatabaseContext).GetProperty(tableName).GetValue(context);
                    T added = table.Add(t);
                    context.SaveChanges();
                    return added;
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public bool update(T t) {
            try {
                using (var context = new DatabaseContext()) {
                    var table = (DbSet<T>)typeof(DatabaseContext).GetProperty(tableName).GetValue(context);
                    int id = (int)typeof(T).GetProperty("id").GetValue(t);
                    var item = table.Find(id);
                    item = t;
                    context.SaveChanges();
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}