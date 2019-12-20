using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IdentityPattern.Controllers
{
    public class AboutController : Controller
    {
        [AllowAnonymous]
        public ActionResult TermsAndConditions()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult PrivacyPolicy()
        {
            return View();
        }
    }
}