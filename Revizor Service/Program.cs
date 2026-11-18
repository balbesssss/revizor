using Revizor_Service.Extension;
using Revizor_Service.Endoints;

var builder = WebApplication.CreateBuilder(args);
builder.RegisterServices();

var app = builder.Build();

app.RegisterMiddleWare();
app.RoleBuilderEndoints();
app.UserBuiderEndpoints();
app.ServiceBuilderEndpoints();
app.FacilityBuilderEndpoints();
app.PlanBuilderEndpoint();
app.PlanAssigmentBuilderEndpoint();
app.RecordBuilderEndpoints();
app.ReportBuilderEndpoints();

app.Run();