using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class RegisterForm
    {
        [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Phone] public string? PhoneNumber { get; set; }
        [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        [Required, Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = string.Empty;
    }
}
