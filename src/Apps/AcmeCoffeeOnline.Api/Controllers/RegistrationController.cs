using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Models;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly AcmeCoffeeDbContext _db;
    private readonly DabblerSecurityDbContext _securityDb;
    private readonly IUserService _userService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        AcmeCoffeeDbContext db,
        DabblerSecurityDbContext securityDb,
        IUserService userService,
        ILogger<RegistrationController> logger)
    {
        _db = db;
        _securityDb = securityDb;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> RegisterAsync(
        [FromBody] RegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if an AppUser with this email already exists
        var existingUser = await _userService.GetByEmailAsync(normalizedEmail);
        if (existingUser is not null)
            return Conflict(new { message = "An account with this email address already exists." });

        // Verify the Customer role exists before proceeding
        var customerRole = await _securityDb.AppRoles
            .FirstOrDefaultAsync(r => r.Name == "Customer", cancellationToken);
        if (customerRole is null)
        {
            _logger.LogError("Customer role not found in security database. Registration cannot proceed.");
            return StatusCode(500, new { message = "Registration is currently unavailable." });
        }

        // Use transactions to ensure atomicity across both databases
        await using var securityTransaction = await _securityDb.Database.BeginTransactionAsync(cancellationToken);
        await using var appTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Create the AppUser via Dabbler Security (createdBy = 0 for self-registration)
            var appUser = await _userService.CreateAsync(
                normalizedEmail,
                $"{request.FirstName} {request.LastName}",
                createdByUserId: 0);

            // Assign Customer role
            _securityDb.AppUserRoles.Add(new AppUserRole
            {
                UserId = appUser.Id,
                RoleId = customerRole.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = appUser.Id
            });
            await _securityDb.SaveChangesAsync(cancellationToken);

            // Create the Customer business record
            var customer = new Customer
            {
                Email = normalizedEmail,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = request.Phone?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                ModifiedBy = normalizedEmail
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);

            await securityTransaction.CommitAsync(cancellationToken);
            await appTransaction.CommitAsync(cancellationToken);

            return Ok(new { message = "Registration successful. Check your email to sign in." });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Registration failed due to constraint violation for {Email}", normalizedEmail);
            await securityTransaction.RollbackAsync(CancellationToken.None);
            await appTransaction.RollbackAsync(CancellationToken.None);
            return Conflict(new { message = "An account with this email address already exists." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", normalizedEmail);
            await securityTransaction.RollbackAsync(CancellationToken.None);
            await appTransaction.RollbackAsync(CancellationToken.None);
            return StatusCode(500, new { message = "An unexpected error occurred during registration." });
        }
    }
}
