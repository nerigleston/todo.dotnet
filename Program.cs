using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ToDoList.Authorization;
using ToDoList.Repositories;
using ToDoList.Services;
using ToDoList.Data;
using System.Text;
using Amazon.S3;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT_KEY is missing from environment variables");
}

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ToDo API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta forma: Bearer {seu token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
           {
               new OpenApiSecurityScheme
               {
                   Reference = new OpenApiReference
                   {
                       Type = ReferenceType.SecurityScheme,
                       Id = "Bearer"
                   }
               },
               new string[] {}
           }
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanView", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole(RolePermissions.Admin) ||
            context.User.IsInRole(RolePermissions.User) && RolePermissions.HasPermission(RolePermissions.User, "View")
        )
    );

    options.AddPolicy("CanEdit", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole(RolePermissions.Admin) ||
            context.User.IsInRole(RolePermissions.User) && RolePermissions.HasPermission(RolePermissions.User, "Edit")
        )
    );

    options.AddPolicy("CanCreate", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole(RolePermissions.Admin) ||
            context.User.IsInRole(RolePermissions.User) && RolePermissions.HasPermission(RolePermissions.User, "Create")
        )
    );

    options.AddPolicy("CanDelete", policy =>
        policy.RequireRole(RolePermissions.Admin)
    );
});

builder.Services.AddSingleton<TodoDbContext>();
builder.Services.AddScoped<TodoRepository>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<S3Service>();
builder.Services.AddAWSService<IAmazonS3>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ToDo API V1");
    });

    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/swagger");
            return;
        }
        await next();
    });
}
app.UseMiddleware<AuthorizationMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
