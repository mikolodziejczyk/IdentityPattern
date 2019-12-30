using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace IdentityPattern.Models.Identity
{
    public class ChangePasswordVM
    {
        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Dotychczasowe hasło")]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        public string NewPassword { get; set; }

        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Powtórz hasło")]
        [Compare("NewPassword", ErrorMessage = "Hasło i powtórzone hasło nie zgadzają się.")]
        public string RepeatNewPassword { get; set; }
    }
}