namespace Amber.WebApi;

public static class Constants
{
    public const string RouteTemplate = "api/v{version:apiVersion}/[controller]";

    public static class Roles
    {
        /// <summary>
        /// This role indicates that the user has verified their email.
        /// </summary>
        public const string VerifiedUser = "VerifiedUser";
    }

    public static class Policies
    {
        /// <summary>
        /// The name of the policy used for rate limiting for the auth controller.
        /// </summary>
        public const string AuthRateLimitingPolicyName = "AuthRateLimitingPolicyName";
    }
}
