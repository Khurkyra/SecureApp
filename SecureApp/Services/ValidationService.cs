using System.Text.RegularExpressions;

namespace SecureApp.Services
{
    public class ValidationService
    {
        public bool IsUsernameValid(string username)
        {
            return Regex.IsMatch(username, @"^[a-zA-Z0-9]+$");
        }

        public bool IsPasswordStrong(string password)
        {
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");
            return passwordRegex.IsMatch(password);
        }
    }
}
