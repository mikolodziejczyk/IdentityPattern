using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace IdentityPattern.Models.Identity
{
    public class ForgotPasswordVM
    {
        [Required]
        [RegularExpression(BootstrapTemplates.Helpers.CommonRegex.Mail, ErrorMessageResourceType = typeof(BootstrapTemplates.Resources.MkoResources), ErrorMessageResourceName = "FieldMustBeMail")]
        [Display(Name = "E-mail")]
        [StringLength(200)]
        public string Email { get; set; }
    }
}