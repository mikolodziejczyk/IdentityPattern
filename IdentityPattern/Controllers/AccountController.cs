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

            if (user == null)
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
            VerifyCaptcha();

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
            VerifyCaptcha();

            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if (user == null || !(await userManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await userManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);


                string mailContentPath = System.IO.Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "Templates/ResetPasswordText.txt");

                string mailContent = System.IO.File.ReadAllText(mailContentPath);
                mailContent = String.Format(mailContent, callbackUrl);

                await userManager.SendEmailAsync(user.Id, Properties.Settings.Default.ResetPasswordTitle, mailContent);
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            return View(model);
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


        private void VerifyCaptcha()
        {
            string response = this.Request.Form["g-recaptcha-response"];

            var nvc = new NameValueCollection();
            nvc.Add("secret", Properties.Settings.Default.CaptchaSecret);
            nvc.Add("response", response);
            nvc.Add("remoteip", this.Request.UserHostAddress);
            byte[] result;

            using (WebClient wc = new WebClient())
            {
                result = wc.UploadValues("https://www.google.com/recaptcha/api/siteverify", "POST", nvc);
            }

            string r = Encoding.UTF8.GetString(result);
            dynamic ro = JValue.Parse(r);
            bool success = ro.success.ToObject<bool>();

            if (!success) throw new InvalidOperationException("Invalid captcha");

            string host = ro.hostname.ToObject<string>(); // the host returned by google
            string actualHost = this.Request.Url.Host;
            bool matches = host == actualHost;

            if (!matches) throw new InvalidOperationException("Captcha orgin doesn't match.");
        }
    }
}