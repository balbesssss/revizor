using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Responses;
using Revizor_Service.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Cryptography;
using Revizor_Service.Extension;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("revizor")));
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name="Authoriation",
        Description="JWT",
        In=ParameterLocation.Header,
        Type=SecuritySchemeType.Http,
        Scheme=JwtBearerDefaults.AuthenticationScheme,
        BearerFormat="JWT"
    });
    opt.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer",doc)] =[] 
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidateLifetime=true,
        ValidateIssuer=true,
        ValidateIssuerSigningKey=true,
        ValidIssuer=builder.Configuration["JWT:Issuer"],
        ValidAudience=builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)), 
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("admin",p => p.RequireRole("admin"));
    opt.AddPolicy("revizor", p=> p.RequireRole("revizor"));
});
builder.Services.AddScoped<IPasswordHasher<CreateUser>, PasswordHasher<CreateUser>>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ReportsServices>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition");
    });
});
builder.Services.AddValidation();

var app = builder.Build();
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        
        var (statusCode, message, code) = ErrorHandler.GetError(ex);
        
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(ApiResponses.Fail(message, code));
    });
});
app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/roles", async (AppDbContext db, CancellationToken ct) => Results.Ok(ApiResponses<IEnumerable<Role>>.Ok(await db.Roles.AsNoTracking().ToArrayAsync(ct)))).WithTags("Role");

app.MapPost("/roles/", async (AppDbContext db, CreateRole dto, CancellationToken ct) =>
{
    db.Roles.Add(new Role (dto.Name.ToLower()));
    await db.SaveChangesAsync(ct);
    return Results.Ok(ApiResponses.Ok("Role is created"));
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapDelete("/roles/{roleId}", async (AppDbContext db, int roleId, CancellationToken ct) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r => r.Id== roleId, ct);
    if (role == null) return Results.NotFound(ApiResponses.Fail("Role not found", ExceptionCode.DoesNotExists));
    db.Roles.Remove(role);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapGet("/roles/{roleId}", async (AppDbContext db, int roleId, CancellationToken ct) =>
{
    var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r=> r.Id==roleId, ct);
    return role != null ? Results.Ok(ApiResponses<Role>.Ok(data: role)) : Results.NotFound(ApiResponses.Fail("Role does not exists", ExceptionCode.DoesNotExists));
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapPatch("/roles/{roleId}",async (AppDbContext db, int roleId, PatchRole dto, CancellationToken ct) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id == roleId, ct);
    if (role == null) {return Results.NotFound(ApiResponses.Fail("Role does not exists", ExceptionCode.DoesNotExists));}
    role.SetFields(dto);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("Role");

app.MapGet("/users", async (AppDbContext db, CancellationToken ct) => 
{
    var users = await db.Users
    .Include(u=> u.Role)
    .AsNoTracking()
    .Select(u=> new UserResponse
    {
        Id = u.Id,
        FullName = u.GetFullName(),
        Role = u.Role!.Name,
        CreatedAt = u.CreatedAt
    }).ToArrayAsync(ct);
    return Results.Ok(ApiResponses<IEnumerable<UserResponse>>.Ok(data: users));
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapPost("/users/", async (IPasswordHasher<CreateUser> hasher,AppDbContext db, CreateUser dto, CancellationToken ct) =>
{
    var user = new User (dto.FirstName,dto.SecondName,dto.LastName,dto.RoleId);
    user.SetPassword(hasher.HashPassword(dto, dto.Password));
    db.Users.Add(user);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/users/{user.Id}", ApiResponses.Ok("User is created"));
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapDelete("/users/{userId}", async (AppDbContext db, Guid userId, CancellationToken ct) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == userId, ct);
    if (user==null) {return Results.NotFound(ApiResponses.Fail("User not found", ExceptionCode.DoesNotExists));}
    user.DeleteUser();
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapGet("/users/{userId}", async (AppDbContext db, Guid userId, CancellationToken ct) =>
{
    var user = await db.Users
        .AsNoTracking()
        .Include(u=> u.Role)
        .FirstOrDefaultAsync(u=> u.Id == userId, ct);
    return user != null ? Results.Ok(ApiResponses<UserResponse>.Ok(new ()
    {
        Id = user.Id,
        FullName = user.GetFullName(),
        Role = user.Role!.Name,
        CreatedAt = user.CreatedAt
    })) : Results.NotFound(ApiResponses.Fail("User does not exists", ExceptionCode.DoesNotExists));
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapPatch("/users/{userId}",async (AppDbContext db, Guid userId, PatchUser dto, CancellationToken ct ) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == userId, ct);
    if (user == null) { return Results.NotFound(ApiResponses.Fail("User does not exists", ExceptionCode.DoesNotExists));}
    user.SetFields(dto);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("User");

app.MapPost("/login", async (AppDbContext db, IPasswordHasher<User> hasher, IConfiguration cfg, HttpContext httpContext, Login dto, CancellationToken ct) =>
{
    var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u=> u.FirstName == dto.FirstName
                                                                        && u.SecondName==dto.SecondName 
                                                                        && u.LastName==dto.LastName, ct);
    if (user==null)
    {
        return Results.Unauthorized();
    }
    if (hasher.VerifyHashedPassword(user,user.Password, dto.Password.ToString()) != PasswordVerificationResult.Success)
    {
        return Results.Unauthorized();
    }
    user.UpdateRefreshData(RefreshToken.GenerateRefreshToken(),DateTime.UtcNow.AddDays(7));

    await db.SaveChangesAsync(ct);
    
    httpContext.AppendCookiesInfo("refreshToken", user.RefreshToken!, (DateTime)user.RefreshExpireTime!);

    return Results.Ok(
        ApiResponses<Token>.Ok(
            new (){ 
                AccessToken = JWT.GenToken(user,cfg)
                }
            )
        );
})
.WithTags("service");

app.MapPost("/refresh", async (AppDbContext db, HttpContext http, IConfiguration cfg, CancellationToken ct) =>
{
    var refreshToken = http.Request.Cookies["refreshToken"];
    if (refreshToken == null) return Results.Unauthorized();
    
    var refreshHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    
    var user = await db.Users
        .Include(u=>u.Role)
        .FirstOrDefaultAsync(u=> u.RefreshToken == refreshHash, ct);
    if (user==null) return Results.Unauthorized();

    if (user.RefreshExpireTime is null || user.RefreshExpireTime < DateTime.UtcNow) 
    {
        user.RevokeRefresh();
        await db.SaveChangesAsync(ct);
        return Results.Unauthorized();
    }

    var newRefresh = RefreshToken.GenerateRefreshToken();
    var newRefreshHash =  Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(newRefresh)));

    user.UpdateRefreshData(newRefreshHash, DateTime.UtcNow.AddDays(7));

    await db.SaveChangesAsync(ct);

    http.AppendCookiesInfo("refreshToken", newRefresh , (DateTime)user.RefreshExpireTime);
    return Results.Ok(new { accessToken = JWT.GenToken(user, cfg)});
})
.WithTags("service");

app.MapGet("/users/me", async (AppDbContext db, HttpContext http, ClaimsPrincipal claims, CancellationToken ct) =>
{
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) return Results.Unauthorized();
    var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u=> u.Id == userId, ct);
    if (user == null) return Results.Unauthorized();

    var userInfo = new
    {
        Id = user.Id,
        FirstName = user.FirstName,
        SecondName = user.SecondName,
        LastName = user.LastName,
        Role = user.Role!.Name,
        CreatedAt = user.CreatedAt
    };

    var json = JsonSerializer.Serialize(userInfo);
    var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");

    http.AppendCookiesInfo("userInfo", base64 , (DateTime)user.RefreshExpireTime);

    return Results.NoContent();
})
.RequireAuthorization(policy => policy.RequireRole("admin","revizor"))
.WithTags("service");

app.MapPost("/users/me/change-password/", async (AppDbContext db, ClaimsPrincipal claims, IPasswordHasher<User> hasher, string pwd, CancellationToken ct) =>
{   
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) return Results.NotFound();

    var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == userId, ct); 
    if (user == null) return Results.NotFound();

    user.SetPassword(hasher.HashPassword(user, pwd));
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization(policy => policy.RequireRole("admin","revizor"))
.WithTags("service");

app.MapGet("/facilities", async (AppDbContext db, CancellationToken ct) => Results.Ok(ApiResponses<IEnumerable<Facility>>.Ok(await db.Facilities.AsNoTracking().ToArrayAsync(ct))))
.RequireAuthorization("admin")
.WithTags("Facility");

app.MapPost("/facilities/", async (AppDbContext db, CreateFacility dto, CancellationToken ct) =>
{
    await db.Facilities.AddAsync(new Facility (dto.Title,dto.Location));
    await db.SaveChangesAsync(ct);
    return Results.Created("/facilities/",ApiResponses.Ok("Facility is created"));
})
.RequireAuthorization("admin")
.WithTags("Facility");

app.MapDelete("/facilities/{facilityId}", async (AppDbContext db, Guid facilityId, CancellationToken ct) =>
{
    var facility = await db.Facilities.FirstOrDefaultAsync(f => f.Id==facilityId,ct);
    if (facility == null) return Results.NotFound(ApiResponses.Fail("Facility not found", ExceptionCode.DoesNotExists));
    db.Facilities.Remove(facility);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("Facility");

app.MapGet("/facilities/{facilityId}", async (AppDbContext db, Guid facilityId, CancellationToken ct) =>
{
    var facility = await db.Facilities.AsNoTracking().FirstOrDefaultAsync(f=> f.Id==facilityId,ct);
    if (facility == null) return Results.NotFound(ApiResponses.Fail("Facility does not exists", ExceptionCode.DoesNotExists));
    return Results.Ok(ApiResponses<Facility>.Ok(data: facility));
})
.RequireAuthorization("admin")
.WithTags("Facility");

app.MapPatch("/facilities/{facilityId}",async (AppDbContext db, Guid facilityId, PatchFacility dto, CancellationToken ct ) =>
{
    var facility = await db.Facilities.FirstOrDefaultAsync(f=> f.Id == facilityId,ct);
    if (facility == null) return Results.NotFound(ApiResponses.Fail("Facility does not exists", ExceptionCode.DoesNotExists));
    facility.SetFields(dto);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("Facility");

app.MapGet("/plan", async (AppDbContext db) => Results.Ok(ApiResponses<IEnumerable<Plan>>.Ok(await db.Plans.AsNoTracking().ToArrayAsync())))
.RequireAuthorization("admin")
.WithTags("Plan");

app.MapGet("/plan/{planId}", async (AppDbContext db, Guid planId, CancellationToken ct) =>
{
    var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p=> p.Id == planId, ct);
    return plan != null? Results.Ok(ApiResponses<Plan>.Ok(plan)) : Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));
})
.RequireAuthorization("admin")
.WithTags("Plan");

app.MapPost("/plan/", async (AppDbContext db, ClaimsPrincipal claims, CreatePlan dto, CancellationToken ct) =>
{
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) return  Results.Unauthorized();
    db.Plans.Add(new Plan(dto.Name, userId, dto.PeriodStart,dto.PeriodEnd));
    await db.SaveChangesAsync(ct);
    return Results.Created("/plan/",ApiResponses.Ok("Plan is created"));    
})
.RequireAuthorization("admin")
.WithTags("Plan");

app.MapPatch("/plan/{planId}",async (AppDbContext db, Guid planId,ClaimsPrincipal claims, PatchPlan dto, CancellationToken ct ) =>
{
    var plan = await db.Plans.FirstOrDefaultAsync(p=>p.Id==planId,ct);
    if (plan==null) return Results.NotFound(ApiResponses.Fail("plan not found",ExceptionCode.NotFound));
    
    plan.SetFields(dto);

    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("Plan");

app.MapDelete("/plan/{planId}", async (AppDbContext db, Guid planId, CancellationToken ct) =>
{
    var plan = await db.Plans.FirstOrDefaultAsync(p=> p.Id == planId,ct);
    if (plan == null) return Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));
    db.Plans.Remove(plan);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();     
})
.RequireAuthorization("admin")
.WithTags("Plan");

app.MapGet("/plan/current", async (AppDbContext db, ClaimsPrincipal claims, CancellationToken ct ) =>
{
    var currentPlan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p=>p.IsArchived==false, ct);
    if (currentPlan == null) return Results.Ok(ApiResponses.Ok("current plan is empty"));

    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier),out Guid userId);
    if (!success) return Results.Unauthorized();

    if (claims.FindFirstValue(ClaimTypes.Role) == "admin")
    {
        var assigments = await db.PlanAssigments
            .AsNoTracking()
            .Where(r=> r.PlanId == currentPlan.Id)
            .Include(r=> r.Facility)
            .Include(r=> r.PassBy)
            .Include(r=> r.ResponsiblePerson)
            .ToListAsync(ct);
        var results = assigments.GroupBy(a=> a.Facility).Select(g=> new AssigmentItem
        {
            Id = g.Select(p=> p.Id).First(),
            FacilityId = g.Key.Id,
            FacilityTitle = g.Key.Title,
            FacilityLocation = g.Key.Location,
            PassAt = g.Select(u=> u.PassAt).FirstOrDefault(),
            Revizors = g.Select(u=> new UserShort {
                Id = u.PassById,
                FullName = u.PassBy!.GetFullName(),
                }).ToList(),
            ResponsiblePerson = g.Select(r => new UserShort
            {
                Id = (Guid)r.ResponsiblePersonId!,
                FullName = r.ResponsiblePerson!.GetFullName()
            }).FirstOrDefault(),
        }).ToList();
        return Results.Ok(ApiResponses<DataCurrentPlanOfAssigment>.Ok(new () {NamePlan = currentPlan.Name, Assigments=results}));
    }
    if (claims.FindFirstValue(ClaimTypes.Role) == "revizor")
    {
        var assigments = await db.PlanAssigments
            .AsNoTracking()
            .Where(r=> r.PlanId == currentPlan.Id)
            .Where(r=> r.PassById == userId)
            .Include(r=> r.Facility)
            .Include(r=> r.PassBy)
            .Include(r=> r.ResponsiblePerson)
            .ToListAsync();
        return Results.Ok(ApiResponses<List<PlanAssigment>>.Ok(assigments));
    }
    else {return Results.BadRequest();}

})
.RequireAuthorization(policy => policy.RequireRole("admin","revizor"))
.WithTags("Plan");

app.MapPost("/plan/archived", async (AppDbContext db, CancellationToken ct) =>
{
    var plan = await db.Plans.FirstOrDefaultAsync(p=> p.IsArchived == false);
    if (plan == null) return Results.NotFound(ApiResponses.Fail("plan not found",ExceptionCode.NotFound));
    plan.Archived();
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization("admin")
.WithTags("Plan");

app.MapGet("/plan-assigment/{planId}" , async (AppDbContext db, Guid planId, CancellationToken ct) =>
{
    var assigments = await db.PlanAssigments
        .AsNoTracking()
        .Where(p=> p.PlanId == planId)
        .Include(p=> p.PassBy)
        .Include(p=> p.ResponsiblePerson)
        .Include(p=> p.Facility)
        .ToArrayAsync(ct);
    var result = assigments.GroupBy(p=> p.Facility)
        .Select(r=> new DataPlanOfAssigment
        {
            Facility = $"{r.Key.Title} {r.Key.Location}",
            PassAt = r.Select(p=> p.PassAt).First(),
            Revizors = r.Select(r=> new UserShort
            {
                Id = r.PassById,
                FullName = r.PassBy!.GetFullName()
            }).ToList(),
            ResponsiblePerson = r.Select(p=> new UserShort
            {
                Id = (Guid)p.ResponsiblePersonId!,
                FullName = p.ResponsiblePerson!.GetFullName()
            }).First()
        }).ToArray();
    return Results.Ok(ApiResponses<IEnumerable<DataPlanOfAssigment>>.Ok(result)) ;
});

app.MapPost("/plan-assigment/{planId}", async (AppDbContext db,ClaimsPrincipal claims, Guid planId, List<CreateAssigment> dtos, CancellationToken ct) =>
{
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) return Results.Unauthorized();

    var plan = await db.Plans.FirstOrDefaultAsync(p=> p.Id == planId, ct);
    if (plan == null) return Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));

    if (dtos.Count == 0) return Results.Ok(ApiResponses.Ok("you send empty body"));

    foreach (var assigment in dtos)
    {
        db.PlanAssigments.Add(new (
            plan.Id,
            assigment.PassById,
            assigment.PassAt,
            assigment.FacilityId,
            assigment.ResponsiblePersonId));
    }
    await db.SaveChangesAsync(ct);
    return Results.Ok(ApiResponses.Ok("Plan Assigments create")); 
    
})
.RequireAuthorization("admin")
.WithTags("PlanAssigment");

app.MapPost("/record/", async (AppDbContext db, ClaimsPrincipal claims,CreateRecord dto, CancellationToken ct) =>
{
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) {return Results.NotFound(ApiResponses.Fail("user not found", ExceptionCode.NotFound));}
    
    var currentPlan = await db.Plans.FirstOrDefaultAsync(p => p.IsArchived==false,ct);
    if (currentPlan == null) return Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));

    var assigment = await db.PlanAssigments.FirstOrDefaultAsync(p=> p.PlanId == currentPlan.Id && p.PassById == userId, ct);
    if (assigment == null) return Results.NotFound(ApiResponses.Fail("assigment not found",ExceptionCode.NotFound));

    var record = new Record (
        assigment.Id,
        dto.EndAt,
        dto.PassageWay);
    db.Records.Add(record);
    await db.SaveChangesAsync();
    return Results.Created($"/record/{record.Id}",ApiResponses.Ok("record add"));
})
.RequireAuthorization(policy => policy.RequireRole("admin","revizor"))
.WithTags("Record");

app.MapPatch("/record/{recordId}", async (AppDbContext db, ClaimsPrincipal claims,Guid recordId, PatchRecord dto, CancellationToken ct) =>
{
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) {return Results.NotFound(ApiResponses.Fail("user not found", ExceptionCode.NotFound));}

    var record = await db.Records.FirstOrDefaultAsync(r=> r.Id == recordId, ct);
    if (record == null) return Results.NotFound(ApiResponses.Fail("record not found",ExceptionCode.NotFound));

    record.SetFields(dto);

    await db.SaveChangesAsync(ct);
    return Results.NoContent();
})
.RequireAuthorization(policy => policy.RequireRole("admin","revizor"))
.WithTags("Record");

app.MapGet("/reports/{planId}/generate", async (AppDbContext db,ReportsServices reportsServices, ClaimsPrincipal claims, Guid planId, CancellationToken ct) =>
{
    var succes = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u=> u.Id==userId, ct);
    if (user == null) return Results.Unauthorized();
    var success = await reportsServices.CreateDocumentReportAdmin(planId,user, ct);
    if (success == 0) return Results.BadRequest();
    return Results.Ok(ApiResponses.Ok("Success generate"));
})
.RequireAuthorization("admin")
.WithTags("Report");

app.MapGet("/reports/my", async (AppDbContext db, Guid reportId, ClaimsPrincipal claims) =>
{
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) return Results.Unauthorized();
    var reports = await db.Reports.Where(r=> r.UserId == userId).ToArrayAsync();
    return Results.Ok(ApiResponses<IEnumerable<Report>>.Ok(reports));
})
.RequireAuthorization(policy => policy.RequireRole("admin","revizor"))
.WithTags("Report");

app.MapGet("/reports", async (AppDbContext db) =>
{
    var results = await db.Reports
        .AsNoTracking()
        .Select(p=> new DataReportToFront {
            PlanId = p.Plan!.Id,
            Name = p.Plan.Name,
            PeriodStart = p.Plan.PeriodStart,
            PeriodEnd = (DateOnly)p.Plan.PeriodEnd!,
            }).ToArrayAsync();
    return Results.Ok(ApiResponses<IEnumerable<DataReportToFront>>.Ok(results));
})
.RequireAuthorization("admin")
.WithTags("Report");

app.MapGet("/reports/{planId}" , async (AppDbContext db, ReportsServices reportsServices, Guid planId, CancellationToken ct) =>
{ 
    var result = await reportsServices.GetRecordsByPlan(planId, ct);
    return Results.Ok(ApiResponses<IEnumerable<DataReportWithRecords>>.Ok(result));
})
.RequireAuthorization("admin")
.WithTags("Report");

app.MapGet("/reports/{reportId}/download", async (AppDbContext db, IConfiguration cfg, ReportsServices reportsServices,ClaimsPrincipal claims ,Guid reportId, CancellationToken ct) =>
{
    var success = Guid.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId);
    if (!success) return Results.Unauthorized();
    
    var user = await db.Users.FirstOrDefaultAsync(u=>u.Id == userId, ct);
    if (user == null) return Results.Unauthorized();

    var report = await db.Reports
                        .Include(r=> r.Plan)
                        .FirstOrDefaultAsync(p=> p.Id == reportId, ct);
    if (report==null) return Results.NotFound(
                                ApiResponses.Fail(
                                """
                                report not found, try to generate report use next link '/generate-reports/planId' . where planId - your necessary plan 
                                """,
                                ExceptionCode.NotFound));

    return Results.File(
        path: $"{cfg["folderReports"]}/Admin/{report.Plan.PeriodStart.Month}_{report.Plan.PeriodStart.Year}.xlsx",
        contentType: "application/vnd.openxmlformats-officed    ocument.spreadsheetml.sheet",
        fileDownloadName: $"{report.Plan.PeriodStart.Month}_{report.Plan.PeriodStart.Year}.xlsx"
    );
})
.RequireAuthorization("admin")
.WithTags("Report");

app.Run();