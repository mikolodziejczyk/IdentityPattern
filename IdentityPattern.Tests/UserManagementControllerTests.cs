using IdentityPattern.Controllers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
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
    public class UserManagementControllerTests
    {
        private UserManagementController userManagementController;

        Mock<ApplicationUserManager> applicationUserManagerMock;
        Mock<UserRepository> userRepositoryMock;

        [SetUp]
        public void Setup()
        {
            Mock<UserStore<ApplicationUser>> userStoreMock = new Mock<UserStore<ApplicationUser>>();
            Mock<IIdentityMessageService> identityMessageServiceMock = new Mock<IIdentityMessageService>();
            Mock<TemplateEmailService> templateEmailServiceMock = new Mock<TemplateEmailService>(identityMessageServiceMock.Object);

            applicationUserManagerMock = new Mock<ApplicationUserManager>(userStoreMock.Object);
            userRepositoryMock = new Mock<UserRepository>(applicationUserManagerMock.Object, templateEmailServiceMock.Object);

            userManagementController = new UserManagementController(userRepositoryMock.Object, applicationUserManagerMock.Object);
        }

        [Test]
        public void DetailsGET_SpecifiedId_UserDataRetrievedAndReturned()
        {
            string userId = Guid.NewGuid().ToString();
            ApplicationUser applicationUser = new ApplicationUser() { Id = userId, Email = "someone@somewhere.com" };

            userRepositoryMock.Setup(x => x.Get(userId)).Returns(applicationUser);

            ViewResult result = (ViewResult)userManagementController.Details(userId);

            Assert.AreEqual(null, result.View);
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
    }
}
