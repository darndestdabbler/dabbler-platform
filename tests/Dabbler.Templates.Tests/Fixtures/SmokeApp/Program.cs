using SmokeApp.Data;

// Minimal Web host. The dabbler-crud-page smoke proves DI wire-up by calling
// the scaffolded AddSmokeScaffolding extension here; the build fails if the
// extension's signature, return type, or referenced services don't compile
// against this consumer's Program.cs surface.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSmokeScaffolding(builder.Configuration);
var app = builder.Build();
app.Run();
