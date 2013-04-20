namespace Auth0.ClaimsProvider
{
    using System.Linq;

    public static class Extensions
    {
        public static string UniqueEmail(this Auth0.User user)
        {
            return user.Identities != null && user.Identities.Count() > 0 ?
                string.Format("{0}_{1}", user.Identities.First().Connection, user.Email) :
                user.Email;
        }
    }
}