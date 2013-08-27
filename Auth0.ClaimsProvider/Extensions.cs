namespace Auth0.ClaimsProvider
{
    using System.Linq;

    public static class Extensions
    {
        public static string UniqueEmail(this Auth0.User user)
        {
            return user.Email != null ? user.Email : user.UserId.Split('|')[1];
        }
    }
}