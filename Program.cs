using Microsoft.OpenApi.Models;

var b = WebApplication.CreateBuilder(args);

b.Services.AddDbContext<AppDb>(o => o.UseSqlite(b.Configuration.GetConnectionString("Default")));

b.Services.AddIdentity<AppUser, IdentityRole>(o => {
    o.Password.RequiredLength = 8;
    o.Password.RequireUppercase = o.Password.RequireLowercase = o.Password.RequireDigit = o.Password.RequireNonAlphanumeric = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    o.Lockout.AllowedForNewUsers = true;
    o.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDb>().AddDefaultTokenProviders();

var jwtKey = b.Configuration["Jwt:Key"] ?? "AcxiomCRM-Dev-Signing-Key-Change-In-Production-2026!";
b.Services.AddAuthentication(o => {
    o.DefaultScheme = "SmartAuth";
    o.DefaultAuthenticateScheme = "SmartAuth";
    o.DefaultChallengeScheme = "SmartAuth";
})
.AddPolicyScheme("SmartAuth", "Cookie or Bearer", o => {
    o.ForwardDefaultSelector = ctx => {
        string auth = ctx.Request.Headers.Authorization;
        if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return JwtBearerDefaults.AuthenticationScheme;
        return IdentityConstants.ApplicationScheme;
    };
})
.AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = b.Configuration["Jwt:Issuer"] ?? "AcxiomCRM",
        ValidAudience = b.Configuration["Jwt:Audience"] ?? "AcxiomCRM.Clients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

b.Services.ConfigureApplicationCookie(o => {
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = c => {
        if (c.Request.Path.StartsWithSegments("/api")) { c.Response.StatusCode = 401; return Task.CompletedTask; }
        c.Response.Redirect(c.RedirectUri); return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = c => {
        if (c.Request.Path.StartsWithSegments("/api")) { c.Response.StatusCode = 403; return Task.CompletedTask; }
        c.Response.Redirect(c.RedirectUri); return Task.CompletedTask;
    };
});

b.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddFixedWindowLimiter("login", x => { x.PermitLimit = 10; x.Window = TimeSpan.FromMinutes(1); });
});

b.Services.AddHttpContextAccessor();
b.Services.AddScoped<AuditService>();
b.Services.AddScoped<JwtTokenService>();
b.Services.AddScoped<PasswordHistoryService>();

b.Services.AddControllersWithViews(o => {
    o.Filters.Add(new AuthorizeFilter());
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

b.Services.AddEndpointsApiExplorer();
b.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "AcxiomCRM REST API",
        Version = "v1",
        Description = "Secured REST API for Customers, Leads, Opportunities, Follow-Ups, and Sales Pipeline Reports."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = b.Build();

await Seeder.RunAsync(app.Services);

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcxiomCRM API v1");
        c.RoutePrefix = "swagger";
    });
} else {
    app.UseExceptionHandler(a => a.Run(async c => {
        c.Response.StatusCode = 500;
        await c.Response.WriteAsync("An unexpected error occurred.");
    }));
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
app.Run();
