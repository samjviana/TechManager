using ResourceMonitorAPI.dao;
using ResourceMonitorLib.models;
using ResourceMonitorLib.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitorAPI.dao {
    class UserDAO : DAO<User> {
        public UserDAO() : base(typeof(User), DatabaseWrapper.Tables.USER) {

        }
    }
}
