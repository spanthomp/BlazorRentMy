using System.ComponentModel.DataAnnotations;

namespace RentMyApi.Models.DTOs.Requests
{
    public class UserRegistrationDto
    {
        [Required]
        public string Username { get; set; }

        //add validation attributes for both - both should be required - email should be in email format
        [Required]
        [EmailAddress]
        //will only be responsible for 2 things
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
