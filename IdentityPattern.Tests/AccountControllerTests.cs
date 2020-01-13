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
using System.Security.Claims;
using System.Security.Principal;
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
        string nonExistingUserName = "not_existing@user.somewhere.com";
        RegisterVM registerModel;
        string confirmationCode = "abcd12345";
        ForgotPasswordVM forgotPasswordVM;
        ResetPasswordVM resetPasswordVM;

        Mock<HttpContextBase> contextMock;
        Mock<HttpRequestBase> requestMock;
        Mock<HttpResponseBase> responseMock;
        Mock<HttpSessionStateBase> sessionMock;
        Mock<HttpServerUtilityBase> serverMock;

        ControllerContext controllerContext;

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
            controllerContext = new ControllerContext(contextMock.Object, new RouteData(), accountController);
            accountController.ControllerContext = controllerContext;

            applicationUser = new ApplicationUser() { Id = Guid.NewGuid().ToString(), UserName = "test@somewhere.com", Email = "test@somewhere.com", EmailConfirmed = true, IsApproved = true, IsDisabled = false };
            signInModel = new SignInVM() { UserName = applicationUser.UserName, Password = "Test123!" };

            // applicationUserManager.FindByNameAsync() for the specified user name returns the predefined ApplicationUser
            applicationUserManagerMock.Setup(x => x.FindByNameAsync(signInModel.UserName)).Returns(Task.FromResult<ApplicationUser>(applicationUser));

            // applicationUserManager.FindByNameAsync() for notExistingUserName returns null
            applicationUserManagerMock.Setup(x => x.FindByNameAsync(nonExistingUserName)).Returns(Task.FromResult<ApplicationUser>(null));

            // applicationUserManagerMock.CreateAsync by default returns success
            applicationUserManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult<IdentityResult>(IdentityResult.Success));

            // applicationUserManagerMock.GenerateEmailConfirmationTokenAsync by default returns success
            applicationUserManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(confirmationCode));

            // signInManager.PasswordSignInAsync() for the specified credentials returns Success
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.Success));

            registerModel = new RegisterVM() { Email = applicationUser.Email, Password = "Test123!", ConfirmPassword = "Test123!" };

            forgotPasswordVM = new ForgotPasswordVM() { Email = applicationUser.Email };

            // applicationUserManagerMock.GenerateEmailConfirmationTokenAsync by default returns success
            applicationUserManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(confirmationCode));

            resetPasswordVM = new ResetPasswordVM() { Email = applicationUser.Email, Code = "abc123", Password = "Test123!", ConfirmPassword = "Test123!" };

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
            signInModel.UserName = nonExistingUserName;

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
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
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
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
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
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SignInPOST_LockedUserLogsIn_ExpectedErrorMessageReturned()
        {
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.LockedOut));

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "/Home/Index");

            AssertModelErrorMessage(accountController.ModelState, AccountController.accountLockedOutMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SignInPOST_IncorrectCredentialsSpecified_ExpectedErrorMessageReturned()
        {
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.Failure));

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "/Home/Index");

            AssertModelErrorMessage(accountController.ModelState, AccountController.loginFailedMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }


        [Test]
        public async Task SignInPOST_UnexpectedReturnValueFromPasswordSignIn_ExpectedErrorMessageReturned()
        {
            signInManagerMock.Setup(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult<SignInStatus>(SignInStatus.RequiresVerification));

            ViewResult result = (ViewResult)await accountController.SignIn(signInModel, "/Home/Index");

            AssertModelErrorMessage(accountController.ModelState, AccountController.loginFailedMessage);

            Assert.AreEqual(signInModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            // PasswordSignInAsync has been called exaclty once with the exactly specified parameters
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(signInModel.UserName, signInModel.Password, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
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

        [Test]
        public void RegisterPOST_UserTriesToRegisterWithIncorrectCaptcha_MethodThrowsAnException()
        {
            captchaServiceMock.Setup(x => x.VerifyCaptcha(It.IsAny<HttpRequestBase>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new InvalidOperationException());

            Assert.Throws<InvalidOperationException>(
                () => { accountController.Register(registerModel); }
                );

            captchaServiceMock.Verify(x => x.VerifyCaptcha(this.accountController.Request, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void RegisterPOST_UserSpecifiesWrongData_ExpectedModelErrorMessageSet()
        {
            // we are testing the path when applicationUserManagerMock.Create() returns an error

            string errorMessageFromCreate = "An error message from create";
            applicationUserManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult<IdentityResult>(new IdentityResult(errorMessageFromCreate)));

            ViewResult result = (ViewResult)accountController.Register(registerModel);

            AssertModelErrorMessage(accountController.ModelState, errorMessageFromCreate);

            Assert.AreEqual(registerModel, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }


        [Test]
        public void RegisterPOST_UserSpecifieCorrectDate_RegisterCalledWithExpectedParameters()
        {
            ApplicationUser receivedApplicationUser = null;
            string receivedPassword = null;

            applicationUserManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(Task.FromResult<IdentityResult>(IdentityResult.Success))
                .Callback((ApplicationUser applicationUser, string password) => { receivedApplicationUser = applicationUser; receivedPassword = password; });

            ActionResult result = (ActionResult)accountController.Register(registerModel);

            Assert.AreEqual(registerModel.Email, receivedApplicationUser.Email);
            Assert.AreEqual(registerModel.Password, receivedPassword);
        }

        [Test]
        public void RegisterPOST_UserSpecifieCorrectDate_ConfirmationMailHasBeenGenerated()
        {
            ApplicationUser receivedApplicationUser = null;
            string receivedPassword = null;

            applicationUserManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(Task.FromResult<IdentityResult>(IdentityResult.Success))
                .Callback((ApplicationUser applicationUser, string password) => { applicationUser.Id = Guid.NewGuid().ToString(); receivedApplicationUser = applicationUser; receivedPassword = password; });

            ActionResult result = (ActionResult)accountController.Register(registerModel);

            string expectedCallbackUrl = accountController.GenerateConfirmationCallbackUrl(receivedApplicationUser.Id, confirmationCode);

            applicationUserManagerMock.Verify(x => x.GenerateEmailConfirmationTokenAsync(receivedApplicationUser.Id), Times.Once);
            templateEmailServiceMock.Verify(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);

            templateEmailServiceMock.Verify(x => x.SendMail(receivedApplicationUser.Email, IdentityPattern.Properties.Settings.Default.ConfirmMailTitle,
                AccountController.ConfirmUserMailTemplateFileRelativePath, It.Is<string[]>((args) => args[0] == expectedCallbackUrl)), Times.Once);
        }

        [Test]
        public void RegisterPOST_RegistrationCorrect_UserRedirectedToConfirmPage()
        {
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/Account/Register", UriKind.Absolute));

            RedirectToRouteResult result = (RedirectToRouteResult)accountController.Register(registerModel);

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("RegisterConfirm", result.RouteValues["Action"]);
        }

        [Test]
        public void RegisterConfirm_RegisterConfirmCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)accountController.RegisterConfirm();
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }

        [Test]
        public async Task ConfirmEmail_NoRequiredData_ConfirmNotCalledAndErrorViewReturned()
        {
            ViewResult result;

            result = (ViewResult)await accountController.ConfirmEmail(null, confirmationCode);
            Assert.AreEqual("Error", result.ViewName);

            result = (ViewResult)await accountController.ConfirmEmail(Guid.NewGuid().ToString(), null);
            Assert.AreEqual("Error", result.ViewName);

            result = (ViewResult)await accountController.ConfirmEmail(null, null);
            Assert.AreEqual("Error", result.ViewName);

            applicationUserManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ConfirmEmail_RequiredDataPresent_ConfirmEmailAsyncCalledWithExpectedArgumentsAndConfirmViewReturned()
        {
            applicationUserManagerMock.Setup(x => x.ConfirmEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(IdentityResult.Success));

            string userId = Guid.NewGuid().ToString();

            ViewResult result = (ViewResult)await accountController.ConfirmEmail(userId, confirmationCode);

            applicationUserManagerMock.Verify(x => x.ConfirmEmailAsync(userId, confirmationCode), Times.Once);

            Assert.AreEqual("ConfirmEmail", result.ViewName);
        }

        [Test]
        public void ForgotPasswordGET_MethodCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)accountController.ForgotPassword();

            Assert.AreEqual(typeof(ForgotPasswordVM), result.Model.GetType());
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }

        [Test]
        public async Task ForgotPasswordPOST_ModelStateInvalid_ViewReturned_GeneratePasswordResetTokenAsyncNotCalled()
        {
            accountController.ModelState.AddModelError(nameof(SignInVM.UserName), "Any error message");

            ViewResult result = (ViewResult)await accountController.ForgotPassword(forgotPasswordVM);

            Assert.AreEqual(forgotPasswordVM, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            applicationUserManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ForgotPasswordPOST_UserTriesToRegister_CaptchaVerified()
        {
            await accountController.ForgotPassword(forgotPasswordVM);

            captchaServiceMock.Verify(x => x.VerifyCaptcha(this.accountController.Request, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ForgotPasswordPOST_UserTriesToCallWithIncorrectCaptcha_MethodThrowsAnException()
        {
            captchaServiceMock.Setup(x => x.VerifyCaptcha(It.IsAny<HttpRequestBase>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new InvalidOperationException());

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => { await accountController.ForgotPassword(forgotPasswordVM); }
                );

            captchaServiceMock.Verify(x => x.VerifyCaptcha(this.accountController.Request, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task ForgotPasswordPOST_NonExistingUserSpecified_ForgotPasswordConfirmationReturned()
        {
            forgotPasswordVM.Email = nonExistingUserName;

            ViewResult result = (ViewResult)await accountController.ForgotPassword(forgotPasswordVM);

            Assert.AreEqual("ForgotPasswordConfirmation", result.ViewName); // return the view for this method
        }

        [Test]
        public async Task ForgotPasswordPOST_UserWithoutConfirmedMail_ForgotPasswordConfirmationReturned()
        {
            applicationUser.EmailConfirmed = false;

            ViewResult result = (ViewResult)await accountController.ForgotPassword(forgotPasswordVM);

            Assert.AreEqual("ForgotPasswordConfirmation", result.ViewName); // return the view for this method
        }

        [Test]
        public async Task ForgotPasswordPOST_UserSpecifiedCorrectData_ResetPasswordMailHasBeenGenerated()
        {
            applicationUser.EmailConfirmed = true;
            await accountController.ForgotPassword(forgotPasswordVM);

            string expectedCallbackUrl = accountController.GeneratePasswordResetUrl(applicationUser.Id, confirmationCode);

            applicationUserManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(applicationUser.Id), Times.Once);
            templateEmailServiceMock.Verify(x => x.SendMail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            templateEmailServiceMock.Verify(x => x.SendMail(applicationUser.Email, IdentityPattern.Properties.Settings.Default.ResetPasswordTitle,
                AccountController.ResetPasswordMailTemplateFileRelativePath, It.Is<string[]>((args) => args[0] == expectedCallbackUrl)), Times.Once);
        }

        [Test]
        public async Task ForgotPasswordPOST_UserSpecifiedCorrectData_UserRedirectedToForgotPasswordConfirmation()
        {
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/Account/ForgotPassword", UriKind.Absolute));

            applicationUser.EmailConfirmed = true;
            RedirectToRouteResult result = (RedirectToRouteResult)await accountController.ForgotPassword(forgotPasswordVM);

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("ForgotPasswordConfirmation", result.RouteValues["Action"]);
        }

        [Test]
        public void ForgotPasswordConfirmationGET_MethodCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)accountController.ForgotPassword();

            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }

        [Test]
        public void ResetPasswordGET_NoCode_ErrorViewReturned()
        {
            ViewResult result = (ViewResult)accountController.ResetPassword((string)null);

            Assert.AreEqual("Error", result.ViewName); // return the view for this method
        }

        [Test]
        public void ResetPasswordGET_CodePresent_ModelWithCodeAndViewReturned()
        {
            string code = "abc123";
            ViewResult result = (ViewResult)accountController.ResetPassword(code);

            Assert.AreEqual(typeof(ResetPasswordVM), result.Model.GetType());
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
            Assert.AreEqual(code, ((ResetPasswordVM)result.Model).Code);
        }

        [Test]
        public async Task ResetPasswordPOST_ModelStateInvalid_ViewReturned_ResetPasswordNotCalled()
        {
            accountController.ModelState.AddModelError(nameof(ResetPasswordVM.Email), "Any error message");

            ViewResult result = (ViewResult)await accountController.ResetPassword(resetPasswordVM);

            Assert.AreEqual(resetPasswordVM, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            applicationUserManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<String>()), Times.Never);
        }

        [Test]
        public async Task ResetPasswordPOST_NonExistingUserSpecified_ConfirmationViewReturnedButResetPasswordNotCalled()
        {
            resetPasswordVM.Email = nonExistingUserName;

            RedirectToRouteResult result = (RedirectToRouteResult)await accountController.ResetPassword(resetPasswordVM);


            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("ResetPasswordConfirmation", result.RouteValues["Action"]);

            applicationUserManagerMock.Verify(x => x.FindByNameAsync(resetPasswordVM.Email), Times.Once);
            applicationUserManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<String>()), Times.Never);
        }

        [Test]
        public async Task ResetPasswordPOST_CorrectDataSpecified_ResetPasswordAsyncCalledAndConfirmationViewReturned()
        {
            applicationUserManagerMock.Setup(x => x.ResetPasswordAsync(applicationUser.Id, resetPasswordVM.Code, resetPasswordVM.Password)).Returns(Task.FromResult(IdentityResult.Success));

            RedirectToRouteResult result = (RedirectToRouteResult)await accountController.ResetPassword(resetPasswordVM);


            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("ResetPasswordConfirmation", result.RouteValues["Action"]);

            applicationUserManagerMock.Verify(x => x.FindByNameAsync(resetPasswordVM.Email), Times.Once);
            applicationUserManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<String>()), Times.Once);
            applicationUserManagerMock.Verify(x => x.ResetPasswordAsync(applicationUser.Id, resetPasswordVM.Code, resetPasswordVM.Password), Times.Once);
        }

        [Test]
        public async Task ResetPasswordPOST_ResetPasswordAsyncReturnedError_ExpectedErrorMessageReturned()
        {
            string resetPasswordErrorMessage = "Reset password error message.";
            applicationUserManagerMock.Setup(x => x.ResetPasswordAsync(applicationUser.Id, resetPasswordVM.Code, resetPasswordVM.Password)).Returns(Task.FromResult(new IdentityResult(resetPasswordErrorMessage)));

            ViewResult result = (ViewResult)await accountController.ResetPassword(resetPasswordVM);

            Assert.AreEqual(resetPasswordVM, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            applicationUserManagerMock.Verify(x => x.FindByNameAsync(resetPasswordVM.Email), Times.Once);
            applicationUserManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<String>()), Times.Once);
            applicationUserManagerMock.Verify(x => x.ResetPasswordAsync(applicationUser.Id, resetPasswordVM.Code, resetPasswordVM.Password), Times.Once);

            AssertModelErrorMessage(accountController.ModelState, resetPasswordErrorMessage);
        }

        [Test]
        public void ResetPasswordConfirmationGET_MethodCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)accountController.ResetPasswordConfirmation();

            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }

        [Test]
        public void ChangePasswordGET_MethodCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)accountController.ChangePassword();

            Assert.AreEqual(typeof(ChangePasswordVM), result.Model.GetType());
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }

        [Test]
        public void ChangePasswordPOST_InputDataCorrect_ChangePasswordCalledAndRedirectReturned()
        {
            #region mocking controller.User and User.Identity.GetUserId<string>()

            string userid = Guid.NewGuid().ToString();
            string userName = "test@somewhere.com";

            List<Claim> claims = new List<Claim>{
             new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", userName),
             new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userid)
            };

            var genericIdentity = new GenericIdentity(userName);
            genericIdentity.AddClaims(claims);
            var genericPrincipal = new GenericPrincipal(genericIdentity, new string[] { });

            contextMock.SetupGet(x => x.User).Returns(genericPrincipal);

            #endregion mocking controller.User and User.Identity.GetUserId<string>()

            ChangePasswordVM changePasswordVM = new ChangePasswordVM() { CurrentPassword = "Password123", NewPassword = "Password321", RepeatNewPassword = "Password321" };

            applicationUserManagerMock.Setup(x => x.ChangePasswordAsync(userid, changePasswordVM.CurrentPassword, changePasswordVM.NewPassword)).Returns(Task.FromResult(IdentityResult.Success));

            RedirectToRouteResult result = (RedirectToRouteResult)accountController.ChangePassword(changePasswordVM);


            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("ChangePasswordConfirmation", result.RouteValues["Action"]);

            applicationUserManagerMock.Verify(x => x.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<String>()), Times.Once);
            applicationUserManagerMock.Verify(x => x.ChangePasswordAsync(userid, changePasswordVM.CurrentPassword, changePasswordVM.NewPassword), Times.Once);
        }

        [Test]
        public void ChangePasswordPOST_ChangePasswordReturnsError_ExpectedErrorMessageReturned()
        {
            #region mocking controller.User and User.Identity.GetUserId<string>()

            string userid = Guid.NewGuid().ToString();
            string userName = "test@somewhere.com";

            List<Claim> claims = new List<Claim>{
             new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", userName),
             new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userid)
            };

            var genericIdentity = new GenericIdentity(userName);
            genericIdentity.AddClaims(claims);
            var genericPrincipal = new GenericPrincipal(genericIdentity, new string[] { });

            contextMock.SetupGet(x => x.User).Returns(genericPrincipal);

            #endregion mocking controller.User and User.Identity.GetUserId<string>()

            ChangePasswordVM changePasswordVM = new ChangePasswordVM() { CurrentPassword = "Password123", NewPassword = "Password321", RepeatNewPassword = "Password321" };

            string changePasswordErrorMessage = "Change password error message.";
            applicationUserManagerMock.Setup(x => x.ChangePasswordAsync(userid, changePasswordVM.CurrentPassword, changePasswordVM.NewPassword)).Returns(Task.FromResult(new IdentityResult(changePasswordErrorMessage)));

            ViewResult result = (ViewResult)accountController.ChangePassword(changePasswordVM);

            Assert.AreEqual(changePasswordVM, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            AssertModelErrorMessage(accountController.ModelState, changePasswordErrorMessage);

            applicationUserManagerMock.Verify(x => x.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<String>()), Times.Once);
            applicationUserManagerMock.Verify(x => x.ChangePasswordAsync(userid, changePasswordVM.CurrentPassword, changePasswordVM.NewPassword), Times.Once);
        }

        [Test]
        public async Task ChangePasswordPOST_ModelStateInvalid_ViewReturned_ChangePasswordNotCalled()
        {
            accountController.ModelState.AddModelError(nameof(ChangePasswordVM.NewPassword), "Any error message");

            ChangePasswordVM changePasswordVM = new ChangePasswordVM() { CurrentPassword = "Password123", NewPassword = "Password321", RepeatNewPassword = "Password321" };
            ViewResult result = (ViewResult)accountController.ChangePassword(changePasswordVM);

            Assert.AreEqual(changePasswordVM, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            applicationUserManagerMock.Verify(x => x.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<String>()), Times.Never);
        }

        [Test]
        public void ChangePasswordConfirmationGET_MethodCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)accountController.ChangePasswordConfirmation();

            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
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
