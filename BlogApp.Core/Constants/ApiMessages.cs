namespace BlogApp.Core.Constants
{
    public static class ApiMessages
    {
        // General Errors
        public const string ErrorOccurredFetchingList = "An error occurred while fetching the list.";
        public const string ErrorOccurredFetchingDetail = "An error occurred while fetching the details.";
        public const string ErrorOccurredCreating = "An error occurred while creating the item.";
        public const string ErrorOccurredUpdating = "An error occurred while updating the item.";
        public const string ErrorOccurredDeleting = "An error occurred while deleting the item.";
        public const string UnexpectedError = "An unexpected error occurred. Please try again later.";

        // Article Specific
        public const string ArticleNotFound = "Article not found or access denied.";
        public const string ArticleNotFoundById = "Article with ID {0} not found.";
        public const string ArticleCreationFailed = "Article creation failed in the service layer.";
        public const string ArticleUpdateFailed = "Article update failed.";
        public const string ArticleDeletionFailed = "Article deletion failed.";
        public const string ArticleNoPermissionToView = "You do not have permission to view this article.";
        public const string ArticleNoPermissionToUpdate = "You do not have permission to update this article.";
        public const string ArticleNoPermissionToDelete = "You do not have permission to delete this article.";

        // Image Specific
        public const string ImageInvalidType = "Invalid file type. Only JPG, PNG, GIF allowed.";
        public const string ImageSizeExceeded = "File size exceeds limit of {0} MB.";
        public const string ImageSaveError = "Error saving image file. Please try again.";

        // Auth/User Specific
        public const string UserUnauthorized = "User is not authorized.";
    }
}