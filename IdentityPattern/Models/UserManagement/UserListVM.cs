using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using User.Repository;

namespace IdentityPattern.Models.UserManagement
{
    public class UserListVM
    {
        public string SearchExpression { get; set; }
        public int PageIndex { get; set; } 
        public int PageSize { get; set; }
        public int TotalRows { get; set; }
        public bool? IsApproved { get; set; }
        public bool? IsDisabled { get; set; }
        public IEnumerable<ApplicationUser> Users { get; set; }

    }
}