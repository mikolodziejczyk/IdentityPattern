using IdentityPattern.Identity;
using IdentityPattern.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace IdentityPattern.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationUserManager userManager;
        private readonly ApplicationSignInManager signInManager;
        private readonly IAuthenticationManager authenicationManager;

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager, IAuthenticationManager authenicationManager)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            this.authenicationManager = authenicationManager ?? throw new ArgumentNullException(nameof(authenicationManager));
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult SignIn(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new SignInVM());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignIn(SignInVM model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = userManager.FindByName(model.UserName);

            if (user==null)
            {
                ModelState.AddModelError("", "Nie udało się zalogować.");
                return View(model);
            }

            if (user.AccessFailedCount > 3)
            {
                // require captcha
            }


            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Nie potwierdziłeś adresu e-mail. Na podany adres otrzymałeś wiadomość e-mail z łączem do potwierdzenia adresu.");
                return View(model);
            }

            if (!user.IsApproved)
            {
                ModelState.AddModelError("", "Twoje konto nie zostało jeszcze zaakceptowane przez administratora.");
                return View(model);
            }

            if (user.IsDisabled)
            {
                ModelState.AddModelError("", "Twoje konto zostało zablokowane nie możesz się zalogować..");
                return View(model);
            }



            var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, isPersistent: model.RememberMe, shouldLockout: true);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    ModelState.AddModelError("", "Twoje konto zostało tymczasowo zablokowane. Spróbuj później.");
                    return View(model);
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Nie udało się zalogować.");
                    return View(model);
            }
        }

        public ActionResult SignOut()
        {
            authenicationManager.SignOut();
            return RedirectToAction("SignIn");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View(new RegisterVM());
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            ApplicationUser user = new ApplicationUser() { UserName = model.Email, Email = model.Email };
            IdentityResult result = userManager.Create(user, model.Password);

            if (result.Succeeded)
            {
                string code = userManager.GenerateEmailConfirmationToken(user.Id);

                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                string mailContentPath = System.IO.Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "Templates/ConfirmMailText.txt");

                string mailContent = System.IO.File.ReadAllText(mailContentPath);
                mailContent = String.Format(mailContent, callbackUrl);

                userManager.SendEmail(user.Id, Properties.Settings.Default.ConfirmMailTitle, mailContent);

                return RedirectToAction("RegisterConfirm");
            }
            else
            {
                ModelState.AddModelError("", result.Errors.FirstOrDefault());
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult RegisterConfirm()
        {
            return View();
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}