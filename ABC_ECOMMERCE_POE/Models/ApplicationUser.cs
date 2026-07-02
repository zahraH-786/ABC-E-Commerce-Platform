using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ABC_ECOMMERCE_POE.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        public string Surname { get; set; }

      
    }
}
