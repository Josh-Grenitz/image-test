using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AdminDashboardService.Middleware
{
    public class AuthorizationFailureMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (InvalidOperationException iv)
            {
                // Active directory authorization throws an invalid operation exception that says "no authentication scheme was specified
                // when a user is unauthorized. This is a workaround to catch that error and return a proper unauthorized message.
                if (iv.Message.Contains("No authenticationScheme was specified"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await context.Response.WriteAsync("Access denied. You do not have permission.");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
