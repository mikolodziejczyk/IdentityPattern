using IdentityPattern.Controllers;
using IdentityPattern.Models.UserManagement;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
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
    public class UserManagementControllerTests
    {
        private UserManagementController userManagementController;

        Mock<ApplicationUserManager> applicationUserManagerMock;
        Mock<UserRepository> userRepositoryMock;

        Mock<HttpContextBase> contextMock;
        Mock<HttpRequestBase> requestMock;
        Mock<HttpResponseBase> responseMock;
        Mock<HttpSessionStateBase> sessionMock;
        Mock<HttpServerUtilityBase> serverMock;

        string runningUserId = Guid.NewGuid().ToString();
        string runningUserName = "test@somewhere.com";

        [SetUp]
        public void Setup()
        {
            Mock<UserStore<ApplicationUser>> userStoreMock = new Mock<UserStore<ApplicationUser>>();
            Mock<IIdentityMessageService> identityMessageServiceMock = new Mock<IIdentityMessageService>();
            Mock<TemplateEmailService> templateEmailServiceMock = new Mock<TemplateEmailService>(identityMessageServiceMock.Object);

            applicationUserManagerMock = new Mock<ApplicationUserManager>(userStoreMock.Object);
            userRepositoryMock = new Mock<UserRepository>(applicationUserManagerMock.Object, templateEmailServiceMock.Object);

            userManagementController = new UserManagementController(userRepositoryMock.Object, applicationUserManagerMock.Object);

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
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/UserManagement/Index", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());

            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(x => x);

            contextMock.SetupGet(x => x.Request).Returns(requestMock.Object);
            contextMock.SetupGet(x => x.Response).Returns(responseMock.Object);

            var routes = new RouteCollection();
            RouteConfig.RegisterRoutes(routes);
            UrlHelper urlHelper = new UrlHelper(new RequestContext(contextMock.Object, new RouteData()), routes);

            #endregion Controller.Url

            userManagementController.Url = urlHelper;
            ControllerContext controllerContext = new ControllerContext(contextMock.Object, new RouteData(), userManagementController);
            userManagementController.ControllerContext = controllerContext;

            #region mocking controller.User and User.Identity.GetUserId<string>()



            List<Claim> claims = new List<Claim>{
             new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", runningUserName),
             new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", runningUserId)
            };

            var genericIdentity = new GenericIdentity(runningUserName);
            genericIdentity.AddClaims(claims);
            var genericPrincipal = new GenericPrincipal(genericIdentity, new string[] { });

            contextMock.SetupGet(x => x.User).Returns(genericPrincipal);

            #endregion mocking controller.User and User.Identity.GetUserId<string>()
        }

        [Test]
        public void IndexGET_UserPageRequested_UserListRetrievedAndReturned()
        {
            ApplicationUser applicationUser = new ApplicationUser() { Id = Guid.NewGuid().ToString(), Email = "someone@somewhere.com" };

            int totalRows = 1;
            userRepositoryMock.Setup(x => x.GetPage(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), out totalRows)).Returns(new ApplicationUser[] { applicationUser });

            ViewResult result = (ViewResult)userManagementController.Index(null, null, null, 1, "Email", "ASC");

            Assert.AreEqual(String.Empty, result.ViewName);

            UserListVM userListVM = (UserListVM)result.Model;

            Assert.AreEqual(null, userListVM.SearchExpression);
            Assert.AreEqual(totalRows, userListVM.TotalRows);
            Assert.AreEqual(null, userListVM.IsApproved);
            Assert.AreEqual(null, userListVM.IsDisabled);
            Assert.AreEqual(new ApplicationUser[] { applicationUser }, userListVM.Users);

            userRepositoryMock.Verify(x => x.GetPage(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), out totalRows), Times.Once);
            userRepositoryMock.Verify(x => x.GetPage(null, null, null, 0, userListVM.PageSize, "Email", "ASC", out totalRows), Times.Once);
        }

        [Test]
        public void DetailsGET_SpecifiedId_UserDataRetrievedAndReturned()
        {
            string userId = Guid.NewGuid().ToString();
            ApplicationUser applicationUser = new ApplicationUser() { Id = userId, Email = "someone@somewhere.com" };

            userRepositoryMock.Setup(x => x.Get(userId)).Returns(applicationUser);

            ViewResult result = (ViewResult)userManagementController.Details(userId);

            Assert.AreEqual(String.Empty, result.ViewName);
            Assert.AreEqual(applicationUser, result.Model);

            userRepositoryMock.Verify(x => x.Get(It.IsAny<string>()), Times.Once);
            userRepositoryMock.Verify(x => x.Get(userId), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_DeleteUser_UserRepositoryDeleteInvokedAndRedirectedToIndex()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.Delete(userId)).Returns(Task.CompletedTask);

            RedirectToRouteResult result = (RedirectToRouteResult)await userManagementController.Details(userId, "delete");

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("Index", result.RouteValues["Action"]);

            userRepositoryMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Once);
            userRepositoryMock.Verify(x => x.Delete(userId), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_DeleteUserFails_OperationFailedViewReturned()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.Delete(userId)).Throws(new InvalidOperationException());

            ViewResult result = (ViewResult)await userManagementController.Details(userId, "delete");

            Assert.AreEqual("OperationFailed", result.ViewName);

            userRepositoryMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Once);
            userRepositoryMock.Verify(x => x.Delete(userId), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_DisableUser_UserRepositoryToggleDisableInvokedAndRedirectedToDetails()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.ToggleDisable(userId, true));

            RedirectToRouteResult result = (RedirectToRouteResult)await userManagementController.Details(userId, "disable");

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("Details", result.RouteValues["Action"]);
            Assert.AreEqual(userId, result.RouteValues["Id"]);

            userRepositoryMock.Verify(x => x.ToggleDisable(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            userRepositoryMock.Verify(x => x.ToggleDisable(userId, true), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_DisableUserFails_OperationFailedViewReturned()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.ToggleDisable(userId, true)).Throws(new InvalidOperationException());

            ViewResult result = (ViewResult)await userManagementController.Details(userId, "disable");

            Assert.AreEqual("OperationFailed", result.ViewName);

            userRepositoryMock.Verify(x => x.ToggleDisable(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            userRepositoryMock.Verify(x => x.ToggleDisable(userId, true), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_EnableUser_UserRepositoryToggleDisableInvokedAndRedirectedToDetails()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.ToggleDisable(userId, false));

            RedirectToRouteResult result = (RedirectToRouteResult)await userManagementController.Details(userId, "enable");

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("Details", result.RouteValues["Action"]);
            Assert.AreEqual(userId, result.RouteValues["Id"]);

            userRepositoryMock.Verify(x => x.ToggleDisable(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            userRepositoryMock.Verify(x => x.ToggleDisable(userId, false), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_EnableUserFails_OperationFailedViewReturned()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.ToggleDisable(userId, false)).Throws(new InvalidOperationException());

            ViewResult result = (ViewResult)await userManagementController.Details(userId, "enable");

            Assert.AreEqual("OperationFailed", result.ViewName);

            userRepositoryMock.Verify(x => x.ToggleDisable(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            userRepositoryMock.Verify(x => x.ToggleDisable(userId, false), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_ApproveUser_UserRepositoryApproveInvokedAndRedirectedToDetails()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.Approve(userId));

            RedirectToRouteResult result = (RedirectToRouteResult)await userManagementController.Details(userId, "approve");

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("Details", result.RouteValues["Action"]);
            Assert.AreEqual(userId, result.RouteValues["Id"]);

            userRepositoryMock.Verify(x => x.Approve(It.IsAny<string>()), Times.Once);
            userRepositoryMock.Verify(x => x.Approve(userId), Times.Once);
        }

        [Test]
        public async Task DetailsPOST_ApproveFails_OperationFailedViewReturned()
        {
            string userId = Guid.NewGuid().ToString();
            userRepositoryMock.Setup(x => x.Approve(userId)).Throws(new InvalidOperationException());

            ViewResult result = (ViewResult)await userManagementController.Details(userId, "approve");

            Assert.AreEqual("OperationFailed", result.ViewName);

            userRepositoryMock.Verify(x => x.Approve(It.IsAny<string>()), Times.Once);
            userRepositoryMock.Verify(x => x.Approve(userId), Times.Once);
        }

        [Test]
        public void NewAdminGET_MethodCalled_ViewReturned()
        {
            ViewResult result = (ViewResult)userManagementController.NewAdmin();

            Assert.AreEqual(typeof(NewAdminMV), result.Model.GetType());
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }

        [Test]
        public void NewAdminPOST_ModelStateInvalid_ViewReturned_CreateUserNotCalled()
        {
            NewAdminMV newAdminMV = new NewAdminMV() { Email = "somebody@somewhere.com", Password = "abc123!", ConfirmPassword = "abc123!" };

            userManagementController.ModelState.AddModelError(nameof(NewAdminMV.Email), "Any error message");

            ViewResult result = (ViewResult)userManagementController.NewAdmin(newAdminMV);

            Assert.AreEqual(newAdminMV, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method

            applicationUserManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Test]
        public void NewAdminPOST_UserSpecifiesWrongData_ExpectedModelErrorMessageSet()
        {
            // we are testing the path when applicationUserManagerMock.Create() returns an error

            string errorMessageFromCreate = "An error message from create";
            applicationUserManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult<IdentityResult>(new IdentityResult(errorMessageFromCreate)));

            NewAdminMV newAdminMV = new NewAdminMV() { Email = "somebody@somewhere.com", Password = "abc123!", ConfirmPassword = "abc123!" };

            ViewResult result = (ViewResult)userManagementController.NewAdmin(newAdminMV);

            AssertModelErrorMessage(userManagementController.ModelState, errorMessageFromCreate);

            Assert.AreEqual(newAdminMV, result.Model);
            Assert.AreEqual(String.Empty, result.ViewName); // return the view for this method
        }


        [Test]
        public void NewAdminPOST_CorrectDataSpecified_CreateCalledWithExpectedParametersAndRoleAddedAndDetailsReturned()
        {
            string createdUserId = Guid.NewGuid().ToString();
            ApplicationUser receivedApplicationUser = null;
            string receivedPassword = null;

            applicationUserManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Returns(Task.FromResult<IdentityResult>(IdentityResult.Success))
                .Callback((ApplicationUser applicationUser, string password) => { applicationUser.Id = createdUserId; receivedApplicationUser = applicationUser; receivedPassword = password; });

            NewAdminMV newAdminMV = new NewAdminMV() { Email = "somebody@somewhere.com", Password = "abc123!", ConfirmPassword = "abc123!" };

            RedirectToRouteResult result = (RedirectToRouteResult)userManagementController.NewAdmin(newAdminMV);

            // has applicationUserManager.CreateAsync() been called with correct parameters?

            Assert.AreEqual(newAdminMV.Email, receivedApplicationUser.Email);
            Assert.AreEqual(newAdminMV.Password, receivedPassword);
            Assert.IsTrue(receivedApplicationUser.IsApproved);
            Assert.IsTrue(receivedApplicationUser.EmailConfirmed);

            // has applicationUserManager.AddToRole() been called with correct parameters?

            applicationUserManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            applicationUserManagerMock.Verify(x => x.AddToRoleAsync(createdUserId, UserManagementController.AdminRoleName), Times.Once);

            // is details view returned

            Assert.AreEqual(null, result.RouteValues["Controller"]);
            Assert.AreEqual("Details", result.RouteValues["Action"]);
            Assert.AreEqual(createdUserId, result.RouteValues["Id"]);
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
