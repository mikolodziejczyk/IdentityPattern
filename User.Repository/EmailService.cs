using log4net;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;

namespace User.Repository
{
    public class EmailService : IIdentityMessageService
    {
        public static readonly ILog log = LogManager.GetLogger(typeof(EmailService));

        public virtual Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            MailMessage mailMessage = new MailMessage();
            mailMessage.To.Add(message.Destination);
            mailMessage.Subject = message.Subject;
            mailMessage.Body = message.Body;
            mailMessage.IsBodyHtml = true;

            SmtpClient smtpClient = new SmtpClient();

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch(Exception e)
            {
                string logMessage = String.Format("Sending a message to '{0}' with the subject '{1}' failed.", mailMessage.To.ToString(), mailMessage.Subject );
                log.Error(logMessage, e);
                throw;
            }

            log.InfoFormat("A message to '{0}' with the subject '{1}' has been sent.", mailMessage.To.ToString(), mailMessage.Subject);
            
            return Task.FromResult(0);
        }


    }
}