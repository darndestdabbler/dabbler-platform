if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?"))
{
    Console.WriteLine("""
        dabbler-devidp — Loopback dev IdP for Dabbler platform development.

        Usage:
          dabbler-devidp [options]

        Options:
          --urls <url>            Override the listen URL (default: https://localhost:5100)
          --environment <name>    ASP.NET Core environment name (default: Development)
          --help, -h, -?          Show this help and exit

        Configuration:
          ConnectionStrings__DevIdp   SQLite connection string (default: Data Source=devidp.db)
          DevIdp__TenantId            Mock tenant GUID issued in tokens (default: 00000000-0000-0000-0000-000000000001)

        See docs/getting-started.md for setup instructions.
        """);
    return;
}

await Dabbler.Security.DevIdp.DevIdpHost.RunAsync(args);
