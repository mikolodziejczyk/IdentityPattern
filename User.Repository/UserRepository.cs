using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User.Repository
{
    public class UserRepository
    {
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
    }
}
