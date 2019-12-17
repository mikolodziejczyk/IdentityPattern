using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace IdentityPattern.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; }
        public bool IsDisabled { get; set; }
        public string CompanyDaxCode { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here

            userIdentity.AddClaim(new Claim("ComanyDaxCode", this.CompanyDaxCode));

            if (IsDisabled) throw new InvalidOperationException("The currently logged-in user has been disabled.");

            return userIdentity;
        }
    }
}