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

    public static class Sync
    {
        /// <summary>
        /// Largest push request accepted. Must stay above the app's own push batch cap
        /// (30 MiB), which a single oversized cell may still exceed.
        /// </summary>
        public const long MaxPushRequestBytes = 64 * 1024 * 1024;
    }
}
