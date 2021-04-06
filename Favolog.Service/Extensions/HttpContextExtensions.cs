using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace Favolog.Service.Extensions
{
    public static class HttpContextExtensions
    {
        public static int? GetLoggedInUserId(this HttpContext httpContext)
        {
            const string userIdClaim = "internalId";            

            string claimValue = httpContext?.User?.FindFirstValue(userIdClaim);

            if (int.TryParse(claimValue, out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}
