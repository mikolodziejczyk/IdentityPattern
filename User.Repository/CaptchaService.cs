using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace User.Repository
{
    public class CaptchaService
    {
        internal const string invalidCaptchaExceptionMessage = "Invalid captcha";
        internal const string orginDoesNotMatchExceptionMessage = "Captcha orgin does not match.";
        internal const string captchaVerifyAddress = "https://www.google.com/recaptcha/api/siteverify";

        public virtual void VerifyCaptcha(HttpRequestBase request, string captchaSecret, string captchaFormFieldName = "g-recaptcha-response")
        {
            string response = request.Form[captchaFormFieldName];

            var nvc = new NameValueCollection();
            nvc.Add("secret", captchaSecret);
            nvc.Add("response", response);
            nvc.Add("remoteip", request.UserHostAddress);
            byte[] result;

            using (WebClient wc = new WebClient())
            {
                result = wc.UploadValues(captchaVerifyAddress, "POST", nvc);
            }

            string r = Encoding.UTF8.GetString(result);
            dynamic ro = JValue.Parse(r);
            bool success = ro.success.ToObject<bool>();

            if (!success) throw new InvalidOperationException(invalidCaptchaExceptionMessage);

            string host = ro.hostname.ToObject<string>(); // the host returned by google
            string actualHost = request.Url.Host;
            bool matches = host == actualHost;

            if (!matches) throw new InvalidOperationException(orginDoesNotMatchExceptionMessage);
        }
    }
}
