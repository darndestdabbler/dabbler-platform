// Minimal host so SDK.Web is satisfied; the smoke test only asserts compile success.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();
