using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace IdentityPattern.Models.UserManagement
{
    public class NewAdminMV
    {
        [Required]
        [RegularExpression(BootstrapTemplates.Helpers.CommonRegex.Mail, ErrorMessageResourceType = typeof(BootstrapTemplates.Resources.MkoResources), ErrorMessageResourceName = "FieldMustBeMail")]
        [Display(Name = "E-mail")]
        [StringLength(200)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; }

        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Powtórz hasło")]
        [Compare("Password", ErrorMessage = "Hasło i powtórzone hasło nie zgadzają się.")]
        public string ConfirmPassword { get; set; }
    }
}