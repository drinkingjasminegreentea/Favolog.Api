using Favolog.Service.Extensions;
using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Favolog.Service.AuthorizationPolicies
{
    public class UserAccessRequirementHandler : AuthorizationHandler<UserAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        const string userIdClaim = "user_id";
        const string nameClaim = "name";
        const string pictureClaim = "picture";
        const string emailAddressClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

        /// <summary>
        /// This handler contains temporary code to link Azure AD B2C user ids to Firebase ones by email address
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public UserAccessRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // self healing mechanism to register users in the system
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserAccessRequirement requirement)
        {
            var claimsPrincipal = context?.User;
            if (claimsPrincipal == null)
                return Task.CompletedTask;

            string userId = claimsPrincipal.FindFirstValue(userIdClaim);            
            if (userId == null)
                return Task.CompletedTask;

            var repository = _httpContextAccessor.HttpContext.RequestServices.GetService(typeof(IFavologRepository)) as IFavologRepository;

            var user = repository.Get<User>().Where(u => u.ExternalId == userId).SingleOrDefault();
            
            if (user != null)
            {
                AddInternalIdClaim(claimsPrincipal, user);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userEmail = claimsPrincipal.FindFirstValue(emailAddressClaim);
            if (userEmail != null)
            {
                user = repository.Get<User>().Where(u => u.EmailAddress == userEmail).FirstOrDefault();
                if (user != null)
                {
                    user.ExternalId = userId;
                    repository.SaveChanges();

                    AddInternalIdClaim(claimsPrincipal, user);
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            user = new User
            {
                ExternalId = userId,
                EmailAddress = userEmail,
                DisplayName = claimsPrincipal.FindFirstValue(nameClaim),
                ProfileImage = claimsPrincipal.FindFirstValue(pictureClaim)
            };

            user.GenerateUsername(repository);

            try
            {
                repository.Attach(user);
                repository.SaveChanges();
            }
            //sometimes user record gets created by another request before we get to this line, so checking for duplicate records
            catch(Exception e)
            {
                user = repository.Get<User>().Where(u => u.ExternalId == userId).SingleOrDefault();
                if (user == null)
                    throw e;
            }            

            AddInternalIdClaim(claimsPrincipal, user);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // This is used later in the request to easily retrieve internal user id value
        private static void AddInternalIdClaim(ClaimsPrincipal claimsPrincipal, User user)
        {
            ((ClaimsIdentity)claimsPrincipal.Identity).AddClaim(new Claim("internalId", user.Id.Value.ToString()));
        }
    }
}
