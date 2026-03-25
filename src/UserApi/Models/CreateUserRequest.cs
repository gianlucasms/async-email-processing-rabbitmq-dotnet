using System.ComponentModel.DataAnnotations;

namespace UserApi.Models;

public sealed record CreateUserRequest(
    [Required, MinLength(2)] string Name,
    [Required, EmailAddress] string Email);
