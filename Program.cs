using ChatBoard.Models;
using ChatBoard.Hubs;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ChatBoardContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddSession();
builder.Services.AddSignalR(); // <-- Add SignalR service
var app = builder.Build();
// Middleware
app.UseSession();
app.UseStaticFiles();
app.UseRouting();
// Map hub endpoint
app.MapHub<ChatHub>("/chathub"); // <-- SignalR Hub endpoint
// Map default controller route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.Run();