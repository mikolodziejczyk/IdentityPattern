﻿using IdentityPattern.Models.UserManagement;
using log4net;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using User.Repository;

namespace IdentityPattern.Controllers
{
    [Authorize]
    public class UserManagementController : Controller
    {
        internal const string AdminRoleName = "Admin";
        private readonly UserRepository userRepository;
        private readonly ApplicationUserManager userManager;

        private static readonly ILog log = LogManager.GetLogger(typeof(UserManagementController));

        public UserManagementController(UserRepository userRepository, ApplicationUserManager userManager)
        {
            this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public ActionResult Index(bool? isApproved, bool? isDisabled, string search, int page = 1, string sort = "Email", string sortDir = "ASC")
        {
            int pageSize = 20;
            int totalRows;

            var users = userRepository.GetPage(search, isApproved, isDisabled, page - 1, pageSize, sort, sortDir, out totalRows);

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
        public async Task<ActionResult> Details(string id, string operation)
        {
            if (operation == "delete")
            {
                try
                {
                    await userRepository.Delete(id);
                }
                catch (Exception e)
                {
                    log.Error($"Removing the user {id} failed.", e);
                    return View("OperationFailed", Tuple.Create("Nie udało się usunąć użytkownika.", Url.Action("Details", new { id = id })));
                }

                return RedirectToAction("Index");
            }
            if (operation == "disable" || operation == "enable")
            {
                try
                {
                    bool shouldDisable = operation == "disable";
                    userRepository.ToggleDisable(id, shouldDisable);

                    if (shouldDisable)
                    {
                        log.InfoFormat("The user {0} has been disabled by {1}.", id, User.Identity.Name);
                    }
                    else
                    {
                        log.InfoFormat("The user {0} has been enabled by {1}.", id, User.Identity.Name);
                    }
                }
                catch(Exception e)
                {
                    log.Error($"Disabling/enabling ({operation}) the user {id} failed.", e);
                    return View("OperationFailed", Tuple.Create("Nie udało się zaktualizować użytkownika.", Url.Action("Details", new { id = id })));
                }

                return RedirectToAction("Details", new { id = id });
            }
            if (operation == "approve")
            {
                try
                {
                    userRepository.Approve(id);
                    log.InfoFormat("The user {0} has been approved by {1}.", id, User.Identity.Name);
                }
                catch(Exception e)
                {
                    log.Error($"Approving the user {id} failed.", e);
                    return View("OperationFailed", Tuple.Create("Nie udało się zaakceptować użytkownika.", Url.Action("Details", new { id = id })));
                }

                return RedirectToAction("Details", new { id = id });
            }
            else
            {
                return RedirectToAction("Details", new { id = id });
            }
        }

        [HttpGet]
        public ActionResult NewAdmin()
        {
            return View(new NewAdminMV());
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult NewAdmin(NewAdminMV model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = new ApplicationUser() { UserName = model.Email, Email = model.Email, IsApproved = true, EmailConfirmed = true };
            IdentityResult result = userManager.Create(user, model.Password); // user.Id is set here

            if (result.Succeeded)
            {
                userManager.AddToRole(user.Id, AdminRoleName);

                log.InfoFormat("A new administrator {0} has been added by {1}.", model.Email, User.Identity.Name);

                return RedirectToAction("Details", new { id = user.Id });
            }
            else
            {
                ModelState.AddModelError("", result.Errors.FirstOrDefault());
            }

            return View(model);
        }
    }
}