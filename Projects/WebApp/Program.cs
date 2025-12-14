using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {options.SignIn.RequireConfirmedAccount = true; options.Stores.SchemaVersion = IdentitySchemaVersions.Version3; })
    .AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminPolicy", policy =>
		policy.RequireRole("Admin"));
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["ConnectionStrings:Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["ConnectionStrings:Authentication:Google:ClientSecret"];
	})
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["ConnectionStrings:Authentication:Facebook:AppId"];
        options.AppSecret = builder.Configuration["ConnectionStrings:Authentication:Facebook:AppSecret"];
	})
	.AddTwitter(options =>
    {
        options.ConsumerKey = builder.Configuration["ConnectionStrings:Authentication:Twitter:ClientId"];
        options.ConsumerSecret = builder.Configuration["ConnectionStrings:Authentication:Twitter:ClientSecret"];
	});

builder.Services.AddRazorPages(options =>
{
	options.Conventions.AuthorizeFolder("/Pools", "AdminPolicy");
    options.Conventions.AuthorizeFolder("/Drivers", "AdminPolicy");
    
    options.Conventions.AuthorizeFolder("/Races")
                       .AuthorizePage("/Races/Import", "AdminPolicy")
                       .AuthorizePage("/Races/Picks/Edit", "AdminPolicy")
                       .AuthorizePage("/Races/Results", "AdminPolicy");

    
    options.Conventions.AuthorizeFolder("/Users", "AdminPolicy");
});

builder.Services.Configure<WebApp.Services.EmailSettings>(builder.Configuration.GetSection("ConnectionStrings:EmailSettings"));
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, WebApp.Services.EmailSender>();

builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    options.AuthenticatorTimeout = TimeSpan.FromMinutes(3);
	options.ChallengeSize = 64;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
