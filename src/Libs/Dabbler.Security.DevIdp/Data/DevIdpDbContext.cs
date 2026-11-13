using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.DevIdp.Data;

public class DevIdpDbContext : IdentityDbContext
{
    public DevIdpDbContext(DbContextOptions<DevIdpDbContext> options)
        : base(options)
    {
    }
}
