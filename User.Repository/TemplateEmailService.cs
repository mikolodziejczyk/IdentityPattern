using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User.Repository
{
    public class TemplateEmailService
    {
        private readonly IIdentityMessageService identityMessageService;
        public string basePath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;

        public TemplateEmailService(IIdentityMessageService identityMessageService)
        {
            this.identityMessageService = identityMessageService ?? throw new ArgumentNullException(nameof(identityMessageService));
        }

        public virtual void SendMail(string to, string subject, string fileRelativePath, string[] templateArguments)
        {
            string mailContent = GetFileContent(fileRelativePath);
            mailContent = String.Format(mailContent, templateArguments);

            IdentityMessage identityMessage = new IdentityMessage()
            {
                Destination = to,
                Body = mailContent,
                Subject = subject
            };

            this.identityMessageService.Send(identityMessage);
        }

        public void SendMail(string to, string subject, string fileRelativePath, string templateArgument)
        {
            this.SendMail(to, subject, fileRelativePath, new string[] { templateArgument });
        }

        public void SendMail(string to, string subject, string fileRelativePath)
        {
            this.SendMail(to, subject, fileRelativePath, new string[] { });
        }

        internal virtual string GetFileContent(string fileRelativePath)
        {
            string mailContentPath = System.IO.Path.Combine(basePath, fileRelativePath);
            string mailContent = System.IO.File.ReadAllText(mailContentPath);
            return mailContent;

        }

    }
}
