namespace BlogApp.Core.Constants
{
    /// <summary>
    /// Defines the application role names as constants.
    /// Using constants helps avoid "magic strings" for role names throughout the application.
    /// </summary>
    public static class AppRoles
    {
        /// <summary>
        /// Administrator role with full access to the system.
        /// </summary>
        public const string Administrator = "Administrator";

        /// <summary>
        /// Author role, allowing users to create, edit, and manage their own articles.
        /// </summary>
        public const string Author = "Author";

        /// <summary>
        /// Ranker role, allowing users to vote (rank) on articles.
        /// </summary>
        public const string Ranker = "Ranker";

        /// <summary>
        /// Commenter role, allowing users to post comments on articles.
        /// </summary>
        public const string Commenter = "Commenter";
    }
}