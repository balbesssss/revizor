using Microsoft.AspNetCore.Diagnostics;
using Revizor_Service.Responses;

namespace Revizor_Service.Extension;

public static class MiddleWare
{
    public static void RegisterMiddleWare (this WebApplication AppBuilder)
    {
        var app = AppBuilder;
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
    }
}
