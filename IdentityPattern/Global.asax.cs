using Autofac;
using Autofac.Integration.Mvc;
using Microsoft.AspNet.Identity.EntityFramework;
using RazorGenerator.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.WebPages;
using Microsoft.AspNet.Identity.Owin;
using User.Repository;

namespace IdentityPattern
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var builder = new ContainerBuilder();

            // Register your MVC controllers. (MvcApplication is the name of
            // the class in Global.asax.)
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            // assembly scanning for an IIS hosted application
            Assembly[] assemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>().ToArray();
            builder.RegisterAssemblyTypes(assemblies).AsSelf();
            // any othe registration code

            builder.Register<UserStore<ApplicationUser>>((c) => new UserStore<ApplicationUser>(new ApplicationDbContext())).InstancePerRequest();
            builder.Register<ApplicationUserManager>((c) => HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>()).ExternallyOwned();
            builder.Register((c) => HttpContext.Current.GetOwinContext().Authentication).ExternallyOwned();
            builder.RegisterType<ApplicationSignInManager>().AsSelf().InstancePerRequest();

            // here we can add code for ModelBinders etc.

            // Set the dependency resolver to be Autofac.
            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

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
