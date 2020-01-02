﻿using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User.Repository
{
    public class UserRepository
    {
        private readonly ApplicationUserManager userManager;

        public UserRepository(ApplicationUserManager userManager)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public IEnumerable<ApplicationUser> GetPage(string searchExpression, bool? isApproved, bool? isDisabled, int pageIndex, int pageSize, string sortColumn, string sortDir, out int totalRows)
        {
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

                return query.Skip(pageIndex * pageSize).Take(pageSize).ToArray();
            }

        }

        public ApplicationUser Get(string id)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                return context.Users.FirstOrDefault(x => x.Id == id);
            }
        }

        public void Approve(string id)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                ApplicationUser user = context.Users.First(x => x.Id == id);
                user.IsApproved = true;
                context.SaveChanges();

                // other steps, like sending an e-mail
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
