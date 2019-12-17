using RazorGenerator.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.WebPages;

namespace IdentityPattern
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var engine = new PrecompiledMvcEngine(Assembly.Load("BootstrapTemplates"));
            // or using any other way to get the Assembly object of the assembly in which you have your views
            ViewEngines.Engines.Add(engine);
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);

            DefaultModelBinder.ResourceClassKey = "DefaultModelBinderResource";
            ClientDataTypeModelValidatorProvider.ResourceClassKey = "DefaultClientSideValidationResource";

        }

        protected void Application_PostAuthorizeRequest(object sender, EventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pl");
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pl-PL");
        }

    }
}
