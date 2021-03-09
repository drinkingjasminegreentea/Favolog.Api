using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using System.Linq;
using System.Security.Claims;

namespace Favolog.Service.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetLoggedInUserId(this HttpContext httpContext)
        {
            if (!httpContext.User.Claims.Any(x => x.Type == ClaimConstants.ObjectId))
            {
                return null;
            }

            Claim objectId = httpContext?.User?.FindFirst(ClaimConstants.ObjectId);

            if (objectId == null)
            {
                return null;
            }

            return objectId.Value;
        }

        public static bool IsAuthorized(this HttpContext httpContext, string externalId)
        {
            var loggedInUserId = httpContext.GetLoggedInUserId();
            return loggedInUserId == externalId;
        }
    }
}
