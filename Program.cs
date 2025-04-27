using PoliticStatements;
using PoliticStatements.Repositories;
using PoliticStatements.Services;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<StatementData>();
builder.Services.AddScoped<TextAnalysis>();
builder.Services.AddScoped<SentimentAnalysis>();
builder.Services.AddScoped<NERAnalysis>();
builder.Services.AddScoped<EmotionAnalysis>();
builder.Services.AddScoped<StylometryAnalysis>();

builder.Services.AddSingleton<StatementRepository>();
builder.Services.AddSingleton<EntityRepository>();
builder.Services.AddSingleton<EmotionRepository>();
builder.Services.AddSingleton<PoliticianRepository>();
builder.Services.AddSingleton<StylometryRepository>();
builder.Services.AddSingleton<SentimentRepository>();
builder.Services.AddHostedService<DataPreloadService>();
var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
