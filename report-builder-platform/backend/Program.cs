using backend.Data;
using backend.Repositories;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IReportQueryBuilderService, ReportQueryBuilderService>();
builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
builder.Services.AddScoped<ISavedReportRepository, SavedReportRepository>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();
