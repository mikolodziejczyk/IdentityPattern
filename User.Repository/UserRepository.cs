using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User.Repository
{
    public class UserRepository
    {
        private const string UserApprovedTemplateRelativePath = @"Templates/UserApproved.txt";
        private readonly ApplicationUserManager userManager;
        private readonly TemplateEmailService templateEmailService;

        public UserRepository(ApplicationUserManager userManager, TemplateEmailService templateEmailService)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.templateEmailService = templateEmailService ?? throw new ArgumentNullException(nameof(templateEmailService));
        }

        public IEnumerable<ApplicationUser> GetPage(string searchExpression, bool? isApproved, bool? isDisabled, int pageIndex, int pageSize, string sortColumn, string sortDir, out int totalRows)
        {
            IEnumerable<ApplicationUser> r;

            using (ApplicationDbContext context = new ApplicationDbContext())
            {

                IQueryable<ApplicationUser> query = context.Users;

                if (String.IsNullOrWhiteSpace(searchExpression) == false)
                {
                    query = query.Where(x => x.Email.Contains(searchExpression) || x.CompanyDaxCode.Contains(searchExpression));
                }

                if (isApproved.HasValue)
                {
                    query = query.Where(x => x.IsApproved == isApproved.Value);
                }

                if (isDisabled.HasValue)
                {
                    query = query.Where(x => x.IsDisabled == isDisabled.Value);
                }

                totalRows = query.Count();

                switch (sortColumn + sortDir)
                {
                    case "EmailASC": query = query.OrderBy(x => x.Email); break;
                    case "EmailDESC": query = query.OrderByDescending(x => x.Email); break;
                    case "CompanyDaxCodeASC": query = query.OrderBy(x => x.CompanyDaxCode); break;
                    case "CompanyDaxCodeDESC": query = query.OrderByDescending(x => x.CompanyDaxCode); break;
                }

                r = query.Skip(pageIndex * pageSize).Take(pageSize).ToArray();

                IdentityRole[] roles = context.Roles.ToArray();

                foreach(ApplicationUser user in r)
                {
                    var userRoleIds = user.Roles.Select(x => x.RoleId);
                    var userRoles = userRoleIds.Select(x => roles.FirstOrDefault(y => y.Id == y.Id)).Where(x => x != null);
                    user.RoleNames = userRoles.Select(x => x.Name).ToArray();
                }

            }

            return r;

        }

        public ApplicationUser Get(string id)
        {
            ApplicationUser r;

            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                r = context.Users.FirstOrDefault(x => x.Id == id);
                r.RoleNames = userManager.GetRoles(id).ToArray();
            }

            return r;
        }

        public void Approve(string id)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                ApplicationUser user = context.Users.First(x => x.Id == id);
                user.IsApproved = true;
                context.SaveChanges();

                templateEmailService.SendMail(user.Email, Properties.Settings.Default.UserApprovedEMailTitle, UserApprovedTemplateRelativePath);
            }
        }

        public void ToggleDisable(string id, bool shouldBeDisabled)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                ApplicationUser user = context.Users.First(x => x.Id == id);
                user.IsDisabled = shouldBeDisabled;
                context.SaveChanges();

                userManager.UpdateSecurityStamp(id);
            }
        }

        public async Task Delete(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            var logins = user.Logins;
            var rolesForUser = await userManager.GetRolesAsync(id);

            ApplicationDbContext context = (ApplicationDbContext)userManager.UserStore.Context;

            using (var transaction = context.Database.BeginTransaction())
            {
                foreach (var login in logins.ToList())
                {
                    await userManager.RemoveLoginAsync(login.UserId, new UserLoginInfo(login.LoginProvider, login.ProviderKey));
                }

                foreach (var role in rolesForUser.ToList())
                {
                    var result = await userManager.RemoveFromRoleAsync(user.Id, role);
                }

                await userManager.DeleteAsync(user);
                transaction.Commit();
            }
        }

    }
}
