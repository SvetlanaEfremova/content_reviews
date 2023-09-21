using course_project.Models;
using course_project.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = "451299270810-a8s5m0g0emhc3v5r4m38h59a6drchj6b.apps.googleusercontent.com";
        options.ClientSecret = "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";
    })
    .AddFacebook(options => 
    {
        options.ClientId = "285586804181967";
        options.ClientSecret = "4a081294e7b7e9d7bf2c69d9c2bd052c";
    })
    .AddCookie(options => {
        options.Events.OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync;
    });
string connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection));

builder.Services.AddTransient<ReviewService>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<TagService>();
builder.Services.AddTransient<CommentService>();
builder.Services.AddTransient<ReactionService>();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();


builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 0;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 0;
    options.User.RequireUniqueEmail = true;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options => {
    options.ValidationInterval = TimeSpan.FromMinutes(1); 
});
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddSignalR();
var app = builder.Build();
var supportedCultures = new List<CultureInfo>
{
    new CultureInfo("en-US"),
    new CultureInfo("ru-RU"),
};
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var roleExists = roleManager.RoleExistsAsync("Admin").Result;
    if (!roleExists)
    {
        var role = new IdentityRole("Admin");
        var roleResult = roleManager.CreateAsync(role).Result;
    }
    
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapHub<CommentsHub>("/commentsHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.Run();
