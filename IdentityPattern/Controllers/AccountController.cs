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
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using User.Repository;

namespace IdentityPattern.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationUserManager userManager;
        private readonly ApplicationSignInManager signInManager;
        private readonly IAuthenticationManager authenicationManager;
        private readonly CaptchaService captchaService;
        private readonly TemplateEmailService templateEmailService;

        internal static readonly string loginFailedMessage = "Nie udało się zalogować.";
        internal static readonly string emailNotConfirmedMessage = "Nie potwierdziłeś adresu e-mail. Na podany adres otrzymałeś wiadomość e-mail z łączem do potwierdzenia adresu.";
        internal static readonly string accountNotApprovedMessage = "Twoje konto nie zostało jeszcze zaakceptowane przez administratora.";
        internal static readonly string accountDisabledMessage = "Twoje konto zostało zablokowane. Nie możesz się zalogować.";
        internal static readonly string accountLockedOutMessage = "Twoje konto zostało tymczasowo zablokowane. Spróbuj później.";

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager, IAuthenticationManager authenicationManager, CaptchaService captchaService, TemplateEmailService templateEmailService)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            this.authenicationManager = authenicationManager ?? throw new ArgumentNullException(nameof(authenicationManager));
            this.captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
            this.templateEmailService = templateEmailService ?? throw new ArgumentNullException(nameof(templateEmailService));
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

            if (user == null)
            {
                ModelState.AddModelError(String.Empty, loginFailedMessage);
                return View(model);
            }

            if (user.AccessFailedCount > 3)
            {
                // require captcha
            }


            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(String.Empty, emailNotConfirmedMessage);
                return View(model);
            }

            if (!user.IsApproved)
            {
                ModelState.AddModelError(String.Empty, accountNotApprovedMessage);
                return View(model);
            }

            if (user.IsDisabled)
            {
                ModelState.AddModelError(String.Empty, accountDisabledMessage);
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, isPersistent: model.RememberMe, shouldLockout: true);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    ModelState.AddModelError(String.Empty, accountLockedOutMessage);
                    return View(model);
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError(String.Empty, loginFailedMessage);
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            captchaService.VerifyCaptcha(this.Request, Properties.Settings.Default.CaptchaSecret);

            ApplicationUser user = new ApplicationUser() { UserName = model.Email, Email = model.Email };
            IdentityResult result = userManager.Create(user, model.Password);

            if (result.Succeeded)
            {
                string code = userManager.GenerateEmailConfirmationToken(user.Id);
                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                templateEmailService.SendMail(user.Email, Properties.Settings.Default.ConfirmMailTitle, @"Templates\ConfirmMailText.txt", callbackUrl);
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


        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await userManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordVM());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            captchaService.VerifyCaptcha(this.Request, Properties.Settings.Default.CaptchaSecret);

            var user = await userManager.FindByNameAsync(model.Email);
            if (user == null || !(await userManager.IsEmailConfirmedAsync(user.Id)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return View("ForgotPasswordConfirmation");
            }

            // Send an email with this link
            string code = await userManager.GeneratePasswordResetTokenAsync(user.Id);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

            templateEmailService.SendMail(user.Email, Properties.Settings.Default.ResetPasswordTitle, @"Templates\ResetPasswordText.txt", callbackUrl);

            return RedirectToAction("ForgotPasswordConfirmation");

        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View(new ResetPasswordVM() { Code = code });
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation");
            }
            var result = await userManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }


            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [Authorize]
        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordVM());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordVM model)
        {
            if (ModelState.IsValid)
            {
                string userId = User.Identity.GetUserId<string>();

                IdentityResult identityResult = userManager.ChangePassword(userId, model.CurrentPassword, model.NewPassword);

                if (!identityResult.Succeeded)
                {
                    ModelState.AddModelError("", identityResult.Errors.First());
                    return View(model);
                }

                return RedirectToAction("ChangePasswordConfirmation");
            }
            else
            {
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult ChangePasswordConfirmation()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public ActionResult MyAccount()
        {
            string id = User.Identity.GetUserId<string>();
            ApplicationUser user = userManager.FindById(id);
            return View(user);
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