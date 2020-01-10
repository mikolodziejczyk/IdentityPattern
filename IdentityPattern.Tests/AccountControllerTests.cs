using IdentityPattern.Controllers;
using IdentityPattern.Models.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using User.Repository;

namespace IdentityPattern.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private AccountController accountController;

        Mock<ApplicationUserManager> applicationUserManagerMock;
        Mock<ApplicationSignInManager> signInManagerMock;
        Mock<IAuthenticationManager> authenicationManagerMock;
        Mock<CaptchaService> captchaServiceMock;
        Mock<TemplateEmailService> templateEmailServiceMock;

        SignInVM signInModel;
        ApplicationUser applicationUser;
        string notExistingUserName = "not_existing@user.somewhere.com";
        string confirmationCode = "abcd12345";
        RegisterVM registerModel;


        Mock<HttpContextBase> contextMock;
        Mock<HttpRequestBase> requestMock;
        Mock<HttpResponseBase> responseMock;
        Mock<HttpSessionStateBase> sessionMock;
        Mock<HttpServerUtilityBase> serverMock;

        [SetUp]
        public void Setup()
        {
            Mock<UserStore<ApplicationUser>> userStoreMock = new Mock<UserStore<ApplicationUser>>();
            Mock<IIdentityMessageService> identityMessageServiceMock = new Mock<IIdentityMessageService>();

            applicationUserManagerMock = new Mock<ApplicationUserManager>(userStoreMock.Object);
            authenicationManagerMock = new Mock<IAuthenticationManager>();
            signInManagerMock = new Mock<ApplicationSignInManager>(applicationUserManagerMock.Object, authenicationManagerMock.Object);
            captchaServiceMock = new Mock<CaptchaService>();
            templateEmailServiceMock = new Mock<TemplateEmailService>(identityMessageServiceMock.Object);

            accountController = new AccountController(applicationUserManagerMock.Object, signInManagerMock.Object, authenicationManagerMock.Object, captchaServiceMock.Object, templateEmailServiceMock.Object);

            #region Controller.Url

            contextMock = new Mock<HttpContextBase>();
            requestMock = new Mock<HttpRequestBase>();
            responseMock = new Mock<HttpResponseBase>();
            sessionMock = new Mock<HttpSessionStateBase>();
            serverMock = new Mock<HttpServerUtilityBase>();

            contextMock.Setup(ctx => ctx.Request).Returns(requestMock.Object);
            contextMock.Setup(ctx => ctx.Response).Returns(responseMock.Object);
            contextMock.Setup(ctx => ctx.Session).Returns(sessionMock.Object);
            contextMock.Setup(ctx => ctx.Server).Returns(serverMock.Object);

            requestMock.SetupGet(x => x.ApplicationPath).Returns("/");
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/Account/SignIn", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());

            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(x => x);

            contextMock.SetupGet(x => x.Request).Returns(requestMock.Object);
            contextMock.SetupGet(x => x.Response).Returns(responseMock.Object);

            var routes = new RouteCollection();
            RouteConfig.RegisterRoutes(routes);
            UrlHelper urlHelper = new UrlHelper(new RequestContext(contextMock.Object, new RouteData()), routes);

            #endregion Controller.Url

            accountController.Url = urlHelper;
            accountController.ControllerContext = new ControllerContext(contextMock.Object, new RouteData(), accountController);

            applicationUser = new ApplicationUser() { Id = Guid.NewGuid().ToString(), UserName = "test@somewhere.com", Email = "test@somewhere.com", EmailConfirmed = true, IsApproved = true, IsDisabled = false };
            signInModel = new SignInVM() { UserName = applicationUser.UserName, Password = "Test123!" };

            // applicationUserManager.FindByNameAsync() for the specified user name returns the predefined ApplicationUser
            applicationUserManagerMock.Setup(x => x.FindByNameAsync(signInModel.UserName)).Returns(Task.FromResult<ApplicationUser>(applicationUser));

            // applicationUserManager.FindByNameAsync() for notExistingUserName returns null
            applicationUserManagerMock.Setup(x => x.FindByNameAsync(notExistingUserName)).Returns(Task.FromResult<ApplicationUser>(null));

            // applicationUserManagerMock.CreateAsync by default returns success
            applicationUserManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult<IdentityResult>(IdentityResult.Success));

            // applicationUserManagerMock.CreateAsync by default returns success
            applicationUserManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(confirmationCode));

            // signInManager.PasswordSignInAsync() for the specified credentials returns Success
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.Success));

            registerModel = new RegisterVM() { Email = "test@somewhere.com", Password = "Test123!", ConfirmPassword = "Test123!" };
        }

        [Test]
        public async Task SignInPOST_ModelStateInvalid_ViewReturned_LoginNotCalled()
        {
            accountController.ModelState.AddModelError(nameof(SignInVM.UserName), "Any error message");

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "");

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task SignInPOST_UserDoesNotExist_ExpectedModelErrorMessageSet_UserSearchedFor_LoginNotCalled()
        {
            // this user does to exist
            signInModel.UserName = notExistingUserName;

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "");

            AssertModelErrorMessage(accountController.ModelState, AccountController.loginFailedMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            applicationUserManagerMock.Verify(x => x.FindByNameAsync(signInModel.UserName), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task SignInPOST_UserEmailNotConfirmed_ExpectedModelErrorMessageSet_LoginNotCalled()
        {
            applicationUser.EmailConfirmed = false;

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "");

            AssertModelErrorMessage(accountController.ModelState, AccountController.emailNotConfirmedMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }


        [Test]
        public async Task SignInPOST_AccountNotApproved_ExpectedModelErrorMessageSet_LoginNotCalled()
        {
            applicationUser.IsApproved = false;

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "");

            AssertModelErrorMessage(accountController.ModelState, AccountController.accountNotApprovedMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task SignInPOST_UserCanLoginAndSpecifiesProperCredentials_LoginCalledAndRedirectReturned()
        {
            string localReturnUrl = "/Home/Index";
            RedirectResult result = (RedirectResult)await accountController.SignIn(signInModel, localReturnUrl);

            Assert.AreEqual(localReturnUrl, result.Url);

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SignInPOST_UserLogsInWithRememberMe_PasswordSignInAsyncCalledWithIsPersistent()
        {
            signInModel.RememberMe = true;

            ActionResult result = (ActionResult)await accountController.SignIn(signInModel, null);

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, true, It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SignInPOST_UserLogsInWithoutReturnUrl_LoginCalledAndRedirectToHomeIndexReturned()
        {
            RedirectToRouteResult result = (RedirectToRouteResult)await accountController.SignIn(signInModel, null);

            Assert.AreEqual("Home", result.RouteValues["Controller"]);
            Assert.AreEqual("Index", result.RouteValues["Action"]);

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SignInPOST_UserLogsInCorrectlyWithExternalUrl_LoginCalledAndRedirectToRouteReturned()
        {
            string localReturnUrl = "http://www.global-solutions.pl";
            RedirectToRouteResult result = (RedirectToRouteResult)await accountController.SignIn(signInModel, localReturnUrl);

            Assert.AreEqual("Home", result.RouteValues["Controller"]);
            Assert.AreEqual("Index", result.RouteValues["Action"]);

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SignInPOST_LockedUserLogsIn_ExpectedErrorMessageReturned()
        {
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.LockedOut));

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "/Home/Index");

            AssertModelErrorMessage(accountController.ModelState, AccountController.accountLockedOutMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SignInPOST_IncorrectCredentialsSpecified_ExpectedErrorMessageReturned()
        {
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.Failure));

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "/Home/Index");

            AssertModelErrorMessage(accountController.ModelState, AccountController.loginFailedMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }


        [Test]
        public async Task SignInPOST_UnexpectedReturnValueFromPasswordSignIn_ExpectedErrorMessageReturned()
        {
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.RequiresVerification));

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "/Home/Index");

            AssertModelErrorMessage(accountController.ModelState, AccountController.loginFailedMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password , It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }


        [Test]
        public void SignOut_MethodCalled_UserSignedOutAndRedirectedToLoginPage()
        {
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/Account/SignOut", UriKind.Absolute));

            RedirectToRouteResult result = (RedirectToRouteResult)accountController.SignOut();

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("SignIn", result.RouteValues["Action"]);

            authenicationManagerMock.Verify(x => x.SignOut(), Times.Once);
        }

        [Test]
        public void RegisterGET_MethodCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)accountController.Register();

            Assert.AreEqual(typeof(RegisterVM), result.Model.GetType());
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }

        [Test]
        public void RegisterPOST_ModelStateInvalid_ViewReturned_UserManagerCreateNotCalled()
        {
            accountController.ModelState.AddModelError(nameof(SignInVM.UserName), "Any error message");

            ViewResult result = (ViewResult)accountController.Register(registerModel);

            Assert.AreEqual(registerModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            applicationUserManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void RegisterPOST_UserTriesToRegister_CaptchaVerified()
        {
            accountController.Register(registerModel);

            captchaServiceMock.Verify(x => x.VerifyCaptcha(this.accountController.Request, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Asserts that the specified model error has been set on the whole model.
        /// </summary>
        /// <param name="modelState"></param>
        /// <param name="expectedErrorMessage"></param>
        private void AssertModelErrorMessage(ModelStateDictionary modelState, string expectedErrorMessage)
        {
            string actualErrorMessage = GetFirstErrorValue(modelState);
            Assert.AreEqual(expectedErrorMessage, actualErrorMessage);

            string actualErrorProperty = modelState.First().Key;
            Assert.AreEqual(String.Empty, actualErrorProperty);
        }

        private string GetFirstErrorValue(ModelStateDictionary modelState)
        {
            return modelState.First().Value.Errors.First().ErrorMessage;
        }

    }
}
