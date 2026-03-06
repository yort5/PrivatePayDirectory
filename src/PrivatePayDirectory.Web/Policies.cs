namespace PrivatePayDirectory.Web;

public static class Policies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireTherapist = "RequireTherapist";
    public const string RequireAuthenticated = "RequireAuthenticated";
}
