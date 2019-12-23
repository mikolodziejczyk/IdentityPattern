using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace IdentityPattern.Models.Identity
{
    public class ResetPasswordVM
    {
        [Required]
        [EmailAddress]
        [Display(Name = "E-mail")]
        [StringLength(200)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100)]
        [Display(Name = "Powtórz nowe hasło")]
        [Compare("Password", ErrorMessage = "Hasło i powtórzone hasło nie zgadzają się.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}