using PoliticStatements;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<StatementData>();
builder.Services.AddScoped<StatementDataDB>();
builder.Services.AddScoped<TextAnalysis>();
builder.Services.AddScoped<SentimentAnalysis>();
builder.Services.AddScoped<NERAnalysis>();
builder.Services.AddScoped<AssociationRulesGenerator>();
builder.Services.AddScoped<NetworkAnalysis>(); 
builder.Services.AddScoped<RhetoricAnalysis>();
builder.Services.AddScoped<TopicAnalysis>(); 
builder.Services.AddScoped<ClusterAnalysis>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
