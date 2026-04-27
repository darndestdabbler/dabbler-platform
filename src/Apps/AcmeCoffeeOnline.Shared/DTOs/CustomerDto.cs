using System.ComponentModel.DataAnnotations;

namespace AcmeCoffeeOnline.Shared.DTOs;

public class CustomerDto
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? ZipCode { get; set; }

    [StringLength(11)]
    public string? SocialSecurityNumber { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    [StringLength(200)]
    public string? ModifiedBy { get; set; }
}
