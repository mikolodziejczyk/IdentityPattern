using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User.Repository
{
    public static class ApplicationUserExtensions
    {
        public static string[] GetDisplayRoleNames(this ApplicationUser user)
        {
            return user.RoleNames.Select(x => GetDisplayRoleNameForRoleName(x)).OrderBy(x => x).ToArray();
        }

        public static string GetCombinedDisplayRoleNames(this ApplicationUser user)
        {
            return String.Join(", ", GetDisplayRoleNames(user));
        }

        public static string GetDisplayRoleNameForRoleName(string roleName)
        {
            string r;

            switch(roleName.ToLower()) {
                case "admin": r = "Administrator"; break;
                case "salesman": r = "Sprzedawca"; break;
                case "client": r = "Klient"; break;
                default: r = "nierozpoznana"; break;
            }

            return r;
        }
    }
}
