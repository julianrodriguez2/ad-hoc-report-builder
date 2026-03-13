using backend.Data;
using backend.Repositories;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ReportingOptions>(builder.Configuration.GetSection("Reporting"));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IReportQueryBuilderService, ReportQueryBuilderService>();
builder.Services.AddScoped<IReportGuardrailService, ReportGuardrailService>();
builder.Services.AddScoped<IReportPreviewService, ReportPreviewService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
builder.Services.AddScoped<ISavedReportRepository, SavedReportRepository>();
builder.Services.AddHttpClient();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AppDbInitializer.SeedMetadataAsync(dbContext);
}

app.UseAuthorization();
app.MapControllers();

app.Run();
