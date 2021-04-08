using Favolog.Service.Models;
using Favolog.Service.Repository;
using System.Linq;
using System.Text.RegularExpressions;

namespace Favolog.Service.Extensions
{
    public static class UserExtensions
    {
        public static void GenerateUsername(this User user, IFavologRepository repository)
        {
            var usernameRegex = new Regex(@"^[a-zA-Z0-9_]*$");
            string username = string.Empty;

            //generate username using display name
            if (!string.IsNullOrEmpty(user.DisplayName))
            {
                username = user.DisplayName.Replace(" ", string.Empty).Replace("'", string.Empty).Replace("-", string.Empty);
            }
            // or generate username using email
            else if (!string.IsNullOrEmpty(user.EmailAddress))
            {                
                username = user.EmailAddress.Substring(0, user.EmailAddress.IndexOf('@'));
            }
            else
            // or generate default username            
            {
                username = "user";
            }

            var existingCount = repository.Get<User>().Where(u => u.Username == username).Count();
            if (existingCount > 0)
            {
                username = $"{username}{existingCount + 1}";
            }
                        
            user.Username = username;
        }
    }
}
