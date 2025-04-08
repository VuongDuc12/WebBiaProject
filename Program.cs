using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;
using WebBiaProject.Hubs;
using WebBiaProject.Models;
using WebBiaProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Register ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Identity (choose ONE of these approaches, not both)
// Option 1: Full Identity setup with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true; // Require email confirmation
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Option 2: If you only wanted the simpler AddDefaultIdentity (commented out)
// builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
//     options.SignIn.RequireConfirmedAccount = true)
//     .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();


var app = builder.Build();
app.MapHub<NotificationHub>("/notificationHub");
// Configure the HTTP request pipeline
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

// Route for Areas (e.g., Admin area)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// Default route for Controllers outside Areas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapRazorPages();

app.Run();