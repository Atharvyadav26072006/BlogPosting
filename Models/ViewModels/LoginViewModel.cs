using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email Is a Required Field")]

        [EmailAddress(ErrorMessage = "Email must be in proper format ")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password Is a Required Field")]

        [DataType(DataType.Password)]
        public string Password { get; set; }
        
    }
}
