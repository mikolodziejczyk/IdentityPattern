using IdentityPattern.Controllers;
using IdentityPattern.Models.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
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

            applicationUser = new ApplicationUser() { Id = Guid.NewGuid().ToString(), UserName = "test@somewhere.com", Email = "test@somewhere.com",  EmailConfirmed = true, IsApproved = true, IsDisabled = false };
            signInModel = new SignInVM() { UserName = applicationUser.UserName, Password =  "Test123!"};
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
            applicationUserManagerMock.Setup(x => x.FindByNameAsync(It.Is<string>((s) => signInModel.UserName == s))).Returns(Task.FromResult<ApplicationUser>(null));

            await accountController.SignIn(signInModel, "");

            AssertModelErrorMessage(accountController.ModelState, AccountController.loginFailedMessage);

            applicationUserManagerMock.Verify(x => x.FindByNameAsync(It.Is<string>((s) => signInModel.UserName == s)), Times.Once);
            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task SignInPOST_UserEmailNotConfirmed_ExpectedModelErrorMessageSet_LoginNotCalled()
        {
            applicationUser.EmailConfirmed = false;
            
            applicationUserManagerMock.Setup(x => x.FindByNameAsync(It.Is<string>((s) => signInModel.UserName == s))).Returns(Task.FromResult<ApplicationUser>(applicationUser));

            await accountController.SignIn(signInModel, "");

            AssertModelErrorMessage(accountController.ModelState, AccountController.emailNotConfirmedMessage);

            signInManagerMock.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }


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
