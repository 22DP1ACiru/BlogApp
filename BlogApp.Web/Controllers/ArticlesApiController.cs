using AutoMapper;
using BlogApp.BLL.Interfaces;
using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.Web.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ArticlesApiController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly IRankingService _rankingService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<ArticlesApiController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ArticlesApiController(
            IArticleService articleService,
            IRankingService rankingService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<ArticlesApiController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _articleService = articleService;
            _rankingService = rankingService;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // Helper methods for image operations
        private async Task<string?> SaveArticleImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0) return null;

            long maxFileSize = 1024 * 1024; // 1 MB
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif" };

            if (image.Length > maxFileSize)
            {
                _logger.LogWarning("Image upload failed: File size exceeds limit. Size: {FileSize}", image.Length);
                throw new ArgumentException(string.Format(ApiMessages.ImageSizeExceeded, maxFileSize / 1024 / 1024));
            }
            if (!allowedContentTypes.Contains(image.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("Image upload failed: Invalid file type. Type: {ContentType}", image.ContentType);
                throw new ArgumentException(ApiMessages.ImageInvalidType);
            }

            string relativeFolderPath = Path.Combine("images", "articles");
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, relativeFolderPath);

            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                _logger.LogInformation("Article image saved: {FilePath}", filePath);
                return $"/{relativeFolderPath.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving article image file: {FileName}", image.FileName);
                throw new IOException(ApiMessages.ImageSaveError, ex);
            }
        }

        private void DeleteArticleImageFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                if (!imageUrl.StartsWith('/') || !imageUrl.Contains("/images/articles/"))
                {
                    _logger.LogWarning("Skipping file deletion. ImageUrl '{ImageUrl}' is not a managed article image path.", imageUrl);
                    return;
                }
                var relativePath = imageUrl.TrimStart('/');
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Deleted article image file: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Article image file not found for deletion: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting article image file {ImageUrl}", imageUrl);
            }
        }

        // Api actions
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticles()
        {
            try
            {
                var articles = await _articleService.GetAllPublishedArticlesWithAuthorsAsync();
                var articleDtos = _mapper.Map<IEnumerable<ArticleDto>>(articles);

                foreach (var dto in articleDtos)
                {
                    dto.Score = await _rankingService.GetArticleScoreAsync(dto.Id);
                }

                return Ok(articleDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting article list for API.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.ErrorOccurredFetchingList });
            }
        }

        // GET: api/ArticlesApi/{id} (Article Detail)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ArticleDto>> GetArticle(int id)
        {
            try
            {
                var article = await _articleService.GetArticleByIdWithAuthorAsync(id);
                if (article == null)
                {
                    return NotFound(new { message = string.Format(ApiMessages.ArticleNotFoundById, id) });
                }

                bool canView = article.IsPublished;
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!canView && currentUserId != null)
                {
                    canView = await _articleService.CanUserModifyArticleAsync(id, currentUserId);
                }

                if (!canView)
                {
                    _logger.LogWarning("Access denied for article ID {ArticleId} for API. Not published and user (if any) lacks permission.", id);
                    return User.Identity?.IsAuthenticated == true ?
                           StatusCode(StatusCodes.Status403Forbidden, new { message = ApiMessages.ArticleNoPermissionToView }) :
                           NotFound(new { message = ApiMessages.ArticleNotFound });
                }

                var articleDto = _mapper.Map<ArticleDto>(article);
                articleDto.Score = await _rankingService.GetArticleScoreAsync(id); 

                return Ok(articleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting article detail for API (ID: {ArticleId}).", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.ErrorOccurredFetchingDetail });
            }
        }


        // POST: api/ArticlesApi (Article Create)
        [HttpPost]
        [Authorize(Roles = $"{AppRoles.Author},{AppRoles.Administrator}")]
        public async Task<ActionResult<ArticleDto>> CreateArticle([FromForm] CreateArticleDto createArticleDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            string? savedImageUrl = null;
            try
            {
                // Handle image upload if an image is provided in the DTO
                if (createArticleDto.Image != null)
                {
                    savedImageUrl = await SaveArticleImageAsync(createArticleDto.Image);
                }

                var article = _mapper.Map<Article>(createArticleDto);

                article.ImageUrl = savedImageUrl;

                var createdArticle = await _articleService.CreateArticleAsync(article, userId);

                if (createdArticle == null)
                {
                    // If service returned null, something went wrong
                    // In case an image was saved but DB op failed, try to delete the orphaned image
                    DeleteArticleImageFile(savedImageUrl);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.ArticleCreationFailed });
                }

                var createdArticleDto = _mapper.Map<ArticleDto>(createdArticle);
                createdArticleDto.Score = await _rankingService.GetArticleScoreAsync(createdArticle.Id);

                return CreatedAtAction(nameof(GetArticle), new { id = createdArticleDto.Id }, createdArticleDto);
            }
            catch (ArgumentException argEx) // From image validation in SaveArticleImageAsync
            {
                _logger.LogWarning(argEx, "Invalid image file during article creation by User {UserId}.", userId);
                return BadRequest(new { message = argEx.Message });
            }
            catch (IOException ioEx) // From file saving in SaveArticleImageAsync
            {
                _logger.LogError(ioEx, "IO error during image saving for article creation by User {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.ImageSaveError });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error during article creation by User {UserId}.", userId);
                // Attempt to delete uploaded image if an error occurred after it was saved
                DeleteArticleImageFile(savedImageUrl);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.UnexpectedError });
            }
        }


        // PUT: api/ArticlesApi/{id} (Article Update)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(int id, [FromForm] UpdateArticleDto updateArticleDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var articleFromDb = await _articleService.GetArticleByIdWithAuthorAsync(id);
            if (articleFromDb == null)
            {
                return NotFound(new { message = string.Format(ApiMessages.ArticleNotFoundById, id) });
            }

            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                _logger.LogWarning("User {UserId} FORBIDDEN from updating Article {ArticleId}", userId, id);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ApiMessages.ArticleNoPermissionToUpdate });
            }

            string? oldImageUrl = articleFromDb.ImageUrl;
            string? newImageUrl = oldImageUrl;

            try
            {
                // Handle new image upload if provided
                if (updateArticleDto.NewImage != null)
                {
                    newImageUrl = await SaveArticleImageAsync(updateArticleDto.NewImage);
                }

                _mapper.Map(updateArticleDto, articleFromDb);

                articleFromDb.ImageUrl = newImageUrl;

                bool success = await _articleService.UpdateArticleAsync(articleFromDb);

                if (!success)
                {
                    // If update failed and a new image was uploaded, but old one wasn't deleted yet, delete the new one
                    if (newImageUrl != oldImageUrl)
                    {
                        DeleteArticleImageFile(newImageUrl);
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.ArticleUpdateFailed });
                }

                // If update succeeded and a new image was uploaded and it's different, delete the old image
                if (newImageUrl != oldImageUrl && !string.IsNullOrEmpty(oldImageUrl))
                {
                    DeleteArticleImageFile(oldImageUrl);
                }

                return NoContent();
            }
            catch (ArgumentException argEx) // From image validation
            {
                _logger.LogWarning(argEx, "Invalid new image file during article update for ID {ArticleId} by User {UserId}.", id, userId);
                return BadRequest(new { message = argEx.Message });
            }
            catch (IOException ioEx) // From file saving
            {
                _logger.LogError(ioEx, "IO error during new image saving for article update ID {ArticleId} by User {UserId}.", id, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.ImageSaveError });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error during article update for ID {ArticleId} by User {UserId}.", id, userId);
                // If a new image was saved but subsequent error occurred, attempt to delete it
                if (newImageUrl != oldImageUrl) DeleteArticleImageFile(newImageUrl);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.UnexpectedError });
            }
        }


        // DELETE: api/ArticlesApi/{id} (Article Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var article = await _articleService.GetArticleByIdWithAuthorAsync(id);
            if (article == null)
            {
                _logger.LogWarning("Article {ArticleId} NOT FOUND for deletion attempt by User {UserId}.", id, userId);
                return NotFound(new { message = string.Format(ApiMessages.ArticleNotFoundById, id) });
            }

            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                _logger.LogWarning("User {UserId} FORBIDDEN from deleting Article {ArticleId}", userId, id);
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ApiMessages.ArticleNoPermissionToDelete });
            }

            bool success = await _articleService.DeleteArticleAsync(id);
            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ApiMessages.ArticleDeletionFailed });
            }

            return NoContent();
        }
    }
}