﻿using IdentityPattern.Models.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using User.Repository;

namespace IdentityPattern.Controllers
{
    [Authorize]
    public class UserManagementController : Controller
    {
        private readonly UserRepository userRepository;

        public UserManagementController(UserRepository userRepository)
        {
            this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public ActionResult Index(bool? isApproved, bool? isDisabled, string search, int page = 1, string sort = "Email", string sortDir = "ASC")
        {
            int pageSize = 20;
            int totalRows;

            var users = userRepository.GetPage(search, isApproved, isDisabled,  page - 1, pageSize, sort, sortDir, out totalRows);

            UserListVM vm = new UserListVM()
            {
                PageSize = pageSize,
                SearchExpression = search,
                TotalRows = totalRows,
                IsApproved = isApproved,
                IsDisabled = isDisabled,
                Users = users
            };

            return View(vm);
        }

        public ActionResult Details(string id)
        {
            ApplicationUser applicationUser = userRepository.Get(id);
            return View(applicationUser);
        }

        [HttpPost]
        public ActionResult Details(string id, string operation)
        {
            if (operation == "delete")
            {
                try
                {
                    
                }
                catch
                {
                    return View("OperationFailed", Tuple.Create("Nie udało się usunąć użytkownika.", Url.Action("Details", new { id = id })));
                }

                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Details", new { id = id });
            }
        }
    }
}