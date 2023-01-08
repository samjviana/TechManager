using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ResourceMonitorLib.models;
using ResourceMonitorLib.utils;

namespace ResourceMonitorAPI.dao {
    class ReadingsDAO : DAO<Readings> {
        public ReadingsDAO() : base(typeof(Readings), DatabaseWrapper.Tables.READINGS) {
            
        }
    }
}