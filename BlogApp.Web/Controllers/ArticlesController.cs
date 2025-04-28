using BlogApp.BLL.Interfaces;
using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.Web.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IRankingService _rankingService;
        private readonly ICommentService _commentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(
            IArticleService articleService,
            IRankingService rankingService,
            ICommentService commentService,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ArticlesController> logger)
        {
            _articleService = articleService;
            _rankingService = rankingService;
            _commentService = commentService;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: /Articles or /Articles/Index (Public List)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var articles = await _articleService.GetAllPublishedArticlesWithAuthorsAsync();

            var viewModels = articles.Select(a => new ArticleViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content?.Length > 200 ? a.Content.Substring(0, 200) + "..." : a.Content,
                ImageUrl = a.ImageUrl,
                PublishedDate = a.PublishedDate,
                AuthorName = a.Author?.UserName ?? "Unknown",
                AuthorProfilePictureUrl = a.Author?.ProfilePictureUrl,
                IsPublished = a.IsPublished
            }).ToList();

            return View(viewModels);
        }

        // GET: /Articles/Details/5 (Public Detail View)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var article = await _articleService.GetArticleByIdWithAuthorAsync(id);

            if (article == null)
            {
                _logger.LogWarning("Article with ID {ArticleId} not found.", id);
                return NotFound();
            }

            // Check if published or user has rights
            bool canView = article.IsPublished;
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get current user ID (null if anonymous)
            bool canModify = false;
            bool canRank = false;
            bool canComment = false;

            if (!canView && currentUserId != null) // Check modify rights only if not published and user is logged in
            {
                canView = await _articleService.CanUserModifyArticleAsync(id, currentUserId);
            }

            if (!canView)
            {
                _logger.LogWarning("Access denied for article ID {ArticleId}. Not published and user (if any) lacks permission.", id);
                return (User.Identity?.IsAuthenticated == true) ? Forbid() : NotFound();
            }

            int score = await _rankingService.GetArticleScoreAsync(id);
            int? currentUserVote = await _rankingService.GetUserVoteForArticleAsync(id, currentUserId);
            var comments = await _commentService.GetCommentsByArticleIdAsync(id);
            var commentViewModels = new List<CommentViewModel>();

            if (currentUserId != null)
            {
                canModify = await _articleService.CanUserModifyArticleAsync(id, currentUserId);
                canRank = await _rankingService.CanUserRankAsync(currentUserId);
                canComment = await _commentService.CanUserCommentAsync(currentUserId);
                currentUserVote = await _rankingService.GetUserVoteForArticleAsync(id, currentUserId);

                foreach (var comment in comments)
                {
                    commentViewModels.Add(new CommentViewModel
                    {
                        Id = comment.Id,
                        Content = comment.Content,
                        CreatedDate = comment.CreatedDate,
                        LastUpdatedDate = comment.LastUpdatedDate,
                        AuthorUsername = comment.User?.UserName ?? "Unknown",
                        AuthorProfilePictureUrl = comment.User?.ProfilePictureUrl,
                        AuthorId = comment.UserId,
                        CanModify = await _commentService.CanUserModifyCommentAsync(comment.Id, currentUserId)
                    });
                }
            }
            else
            {
                foreach (var comment in comments)
                {
                    commentViewModels.Add(new CommentViewModel
                    {
                        Id = comment.Id,
                        Content = comment.Content,
                        CreatedDate = comment.CreatedDate,
                        LastUpdatedDate = comment.LastUpdatedDate,
                        AuthorUsername = comment.User?.UserName ?? "Unknown",
                        AuthorProfilePictureUrl = comment.User?.ProfilePictureUrl,
                        AuthorId = comment.UserId,
                        CanModify = false
                    });
                }
            }

            var viewModel = new ArticleViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                ImageUrl = article.ImageUrl,
                PublishedDate = article.PublishedDate,
                AuthorName = article.Author?.UserName ?? "Unknown",
                AuthorProfilePictureUrl = article.Author?.ProfilePictureUrl,
                IsPublished = article.IsPublished,
                CanModify = canModify,
                Score = score,
                CurrentUserVote = currentUserVote,
                Comments = commentViewModels
            };

            ViewBag.CanRank = canRank;
            ViewBag.CanComment = canComment;

            return View(viewModel);
        }

        // Vote action
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Ranker},{AppRoles.Administrator}")]
        public async Task<IActionResult> Vote(int articleId, int voteValue)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            if (voteValue != 1 && voteValue != -1)
            {
                TempData["ErrorMessage"] = "Invalid vote value.";
                return RedirectToAction(nameof(Details), new { id = articleId });
            }

            bool success = await _rankingService.VoteAsync(articleId, userId, voteValue);

            if (!success)
            {
                TempData["ErrorMessage"] = "Could not process your vote at this time.";
            }

            return RedirectToAction(nameof(Details), new { id = articleId });
        }

        // GET: /Articles/MyArticles (Logged-in user's articles)
        [HttpGet]
        [Authorize(Roles = $"{AppRoles.Author},{AppRoles.Administrator}")]  
        public async Task<IActionResult> MyArticles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var articles = await _articleService.GetArticlesByAuthorIdAsync(userId);

            var viewModels = articles.Select(a => new ArticleViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content?.Length > 100 ? a.Content.Substring(0, 100) + "..." : a.Content,
                ImageUrl = a.ImageUrl,
                PublishedDate = a.PublishedDate,
                AuthorName = a.Author?.UserName ?? "Unknown",
                IsPublished = a.IsPublished,
                CanModify = true
            }).ToList();

            return View(viewModels);
        }


        // GET: /Articles/Create
        [HttpGet]
        [Authorize(Roles = $"{AppRoles.Author},{AppRoles.Administrator}")]
        public IActionResult Create()
        {
            return View(new CreateArticleViewModel());
        }

        // POST: /Articles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Author},{AppRoles.Administrator}")]
        public async Task<IActionResult> Create(CreateArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? imageUrl = null;
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                {
                    _logger.LogError("User ID not found in Create POST despite Authorize attribute.");
                    ModelState.AddModelError("", "Unable to identify the current user.");
                    return View(model);
                }

                try
                {
                    if (model.Image != null)
                    {
                        imageUrl = await SaveArticleImageAsync(model.Image);
                    }

                    var article = new Article
                    {
                        Title = model.Title,
                        Content = model.Content,
                        ImageUrl = imageUrl,
                        IsPublished = (model.SubmitAction == "Publish")
                    };

                    var createdArticle = await _articleService.CreateArticleAsync(article, userId);

                    if (createdArticle != null)
                    {
                        _logger.LogInformation("Article {ArticleId} created successfully by User {UserId}.", createdArticle.Id, userId);
                        TempData["StatusMessage"] = "Article created successfully!";
                        return RedirectToAction(nameof(Details), new { id = createdArticle.Id });
                    }
                    else
                    {
                        _logger.LogWarning("Article creation failed in service layer for Title: {Title} by User {UserId}", model.Title, userId);
                        ModelState.AddModelError("", "Unable to create the article. Please try again.");
                        DeleteArticleImageFile(imageUrl);
                    }
                }
                catch (ArgumentException argEx)
                {
                    _logger.LogWarning(argEx, "Invalid image file provided during article creation by User {UserId}.", userId);
                    ModelState.AddModelError(nameof(model.Image), argEx.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during article creation process for Title: {Title} by User {UserId}", model.Title, userId);
                    ModelState.AddModelError("", "An unexpected error occurred while creating the article.");
                    DeleteArticleImageFile(imageUrl); // Attempt cleanup on general error
                }
            }

            return View(model);
        }

        // GET: /Articles/Edit/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            // Check if user is allowed to edit THIS article
            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                _logger.LogWarning("Authorization failed for User {UserId} attempting to edit Article {ArticleId}", userId, id);
                return Forbid();
            }

            var article = await _articleService.GetArticleByIdWithAuthorAsync(id); // Get article details
            if (article == null)
            {
                _logger.LogWarning("Article {ArticleId} not found for editing by User {UserId}", id, userId);
                return NotFound();
            }

            var viewModel = new EditArticleViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                ExistingImageUrl = article.ImageUrl,
                IsPublished = article.IsPublished
            };

            return View(viewModel);
        }

        // POST: /Articles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, EditArticleViewModel model)
        {
            // Ensure the ID from the route matches the ID in the model
            if (id != model.Id)
            {
                _logger.LogWarning("ID mismatch in Edit POST. Route ID: {RouteId}, Model ID: {ModelId}", id, model.Id);
                return BadRequest("Invalid request.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            // Re-verify authorization on POST
            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                _logger.LogWarning("Authorization failed for User {UserId} attempting to POST edit Article {ArticleId}", userId, id);
                return Forbid();
            }


            if (ModelState.IsValid)
            {
                string? newImageUrl = model.ExistingImageUrl;
                bool deleteOldImage = false;
                string? oldImageUrl = null;

                try
                {
                    // Fetch the existing article to get its current state (including old ImageUrl)
                    var existingArticle = await _articleService.GetArticleByIdWithAuthorAsync(id);
                    if (existingArticle == null) return NotFound();
                    oldImageUrl = existingArticle.ImageUrl;

                    if (model.NewImage != null && model.NewImage.Length > 0)
                    {
                        newImageUrl = await SaveArticleImageAsync(model.NewImage);
                        deleteOldImage = true; // Mark old image for deletion if new one saved successfully
                    }

                    var articleToUpdate = new Article
                    {
                        Id = model.Id,
                        Title = model.Title,
                        Content = model.Content,
                        ImageUrl = newImageUrl,
                        IsPublished = model.IsPublished
                    };

                    bool success = await _articleService.UpdateArticleAsync(articleToUpdate);

                    if (success)
                    {
                        _logger.LogInformation("Article {ArticleId} updated successfully by User {UserId}", id, userId);
                        // Delete the OLD image file only AFTER successful DB update and if new image was saved
                        if (deleteOldImage)
                        {
                            DeleteArticleImageFile(oldImageUrl);
                        }
                        TempData["StatusMessage"] = "Article updated successfully!";
                        return RedirectToAction(nameof(Details), new { id = model.Id });
                    }
                    else
                    {
                        _logger.LogWarning("Article update failed in service layer for Article {ArticleId} by User {UserId}", id, userId);
                        ModelState.AddModelError("", "Unable to update the article. Please try again.");

                        // If a new image was saved but DB update failed, attempt to delete the newly saved one
                        if (newImageUrl != model.ExistingImageUrl) DeleteArticleImageFile(newImageUrl);
                    }
                }
                catch (ArgumentException argEx) // From image saving
                {
                    _logger.LogWarning(argEx, "Invalid image file provided during article update for Article {ArticleId} by User {UserId}", id, userId);
                    ModelState.AddModelError(nameof(model.NewImage), argEx.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during article update process for Article {ArticleId} by User {UserId}", id, userId);
                    ModelState.AddModelError("", "An unexpected error occurred while updating the article.");
                    // If a new image was saved but subsequent error occurred, attempt to delete it
                    if (newImageUrl != model.ExistingImageUrl && newImageUrl != oldImageUrl) DeleteArticleImageFile(newImageUrl);
                }
            }

            // If we got this far, something failed, redisplay form
            if (string.IsNullOrEmpty(model.ExistingImageUrl))
            {
                var articleForView = await _articleService.GetArticleByIdWithAuthorAsync(id);
                model.ExistingImageUrl = articleForView?.ImageUrl;
            }
            return View(model);
        }

        // GET: /Articles/Delete/5 (Confirmation Page)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            // Verify authorization
            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                _logger.LogWarning("Authorization failed for User {UserId} attempting to view delete confirmation for Article {ArticleId}", userId, id);
                return Forbid();
            }

            var article = await _articleService.GetArticleByIdWithAuthorAsync(id); // Get details to display
            if (article == null)
            {
                _logger.LogWarning("Article {ArticleId} not found for delete confirmation by User {UserId}", id, userId);
                return NotFound();
            }

            var viewModel = new ArticleViewModel
            {
                Id = article.Id,
                Title = article.Title,
                AuthorName = article.Author?.UserName ?? "Unknown"
            };

            return View(viewModel);
        }

        // POST: /Articles/Delete/5 (Execute Deletion)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            // Re-verify authorization before deleting
            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                _logger.LogWarning("Authorization failed for User {UserId} attempting to POST delete Article {ArticleId}", userId, id);
                return Forbid();
            }

            bool success = await _articleService.DeleteArticleAsync(id);

            if (success)
            {
                _logger.LogInformation("Article {ArticleId} deleted successfully by User {UserId}", id, userId);
                TempData["StatusMessage"] = "Article deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                _logger.LogError("Article deletion failed in service layer for Article {ArticleId} by User {UserId}", id, userId);
                TempData["ErrorMessage"] = "Failed to delete the article. It might have already been deleted or an error occurred.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }


        // --- Private Helper Methods ---

        private async Task<string?> SaveArticleImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0) return null;

            long maxFileSize = 1024 * 1024; // 1 MB limit
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif" };

            if (image.Length > maxFileSize) throw new ArgumentException($"File size exceeds limit of {maxFileSize / 1024 / 1024} MB.");
            if (!allowedContentTypes.Contains(image.ContentType.ToLowerInvariant())) throw new ArgumentException("Invalid file type. Only JPG, PNG, GIF allowed.");

            string relativeFolderPath = Path.Combine("images", "articles");
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, relativeFolderPath);

            Directory.CreateDirectory(uploadsFolder); // Create if doesn't exist

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName); // Use extension from original file
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                return $"/{relativeFolderPath.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving article image file: {FileName}", image.FileName);
                throw new IOException("Error saving image file.", ex);
            }
        }

        private void DeleteArticleImageFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                // Check if it's a relative path we manage
                if (!imageUrl.StartsWith('/') || !imageUrl.Contains("/images/articles/"))
                {
                    _logger.LogWarning("Skipping file deletion. ImageUrl '{ImageUrl}' is not a managed article image path.", imageUrl);
                    return;
                }
                var relativePath = imageUrl.TrimStart('/');
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)); // Convert URL slashes to OS path slashes

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Deleted article image file: {FilePath}", filePath);
                }
                else { _logger.LogWarning("Article image file not found for deletion: {FilePath}", filePath); }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error deleting article image file {ImageUrl}", imageUrl); }
        }

    }
}