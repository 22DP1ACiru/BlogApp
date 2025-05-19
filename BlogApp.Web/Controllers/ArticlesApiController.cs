using AutoMapper;
using BlogApp.BLL.Interfaces;
using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesApiController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly IRankingService _rankingService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<ArticlesApiController> _logger;

        public ArticlesApiController(
            IArticleService articleService,
            IRankingService rankingService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ILogger<ArticlesApiController> logger)
        {
            _articleService = articleService;
            _rankingService = rankingService;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

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
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching articles.");
            }
        }

        // GET: api/ArticlesApi/{id} (Article Detail)
        [HttpGet("{id}")]
        [AllowAnonymous] // Publicly accessible
        public async Task<ActionResult<ArticleDto>> GetArticle(int id)
        {
            try
            {
                var article = await _articleService.GetArticleByIdWithAuthorAsync(id);
                if (article == null)
                {
                    return NotFound(new { message = $"Article with ID {id} not found." });
                }

                bool canView = article.IsPublished;
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!canView && currentUserId != null)
                {
                    canView = await _articleService.CanUserModifyArticleAsync(id, currentUserId);
                }

                if (!canView)
                {
                    return User.Identity?.IsAuthenticated == true ? Forbid() : NotFound(new { message = "Article not found or access denied." });
                }

                var articleDto = _mapper.Map<ArticleDto>(article);
                articleDto.Score = await _rankingService.GetArticleScoreAsync(id); 

                return Ok(articleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting article detail for API (ID: {ArticleId}).", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching the article.");
            }
        }


        // POST: api/ArticlesApi (Article Create)
        [HttpPost]
        [Authorize(Roles = $"{AppRoles.Author},{AppRoles.Administrator}")]
        public async Task<ActionResult<ArticleDto>> CreateArticle([FromBody] CreateArticleDto createArticleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var article = _mapper.Map<Article>(createArticleDto);
            // article.ImageUrl can be handled here if CreateArticleDto includes it and it's just a URL.
            // If file upload is needed, it's more complex:
            // - Use [FromForm] and include IFormFile in ViewModel/DTO
            // - Call your SaveArticleImageAsync logic here
            // - For this example, assume ImageUrl is simple or handled separately.

            var createdArticle = await _articleService.CreateArticleAsync(article, userId);

            if (createdArticle == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Article creation failed." });
            }

            var createdArticleDto = _mapper.Map<ArticleDto>(createdArticle);
            createdArticleDto.Score = await _rankingService.GetArticleScoreAsync(createdArticle.Id);

            return CreatedAtAction(nameof(GetArticle), new { id = createdArticleDto.Id }, createdArticleDto);
        }


        // PUT: api/ArticlesApi/{id} (Article Update)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateArticle(int id, [FromBody] UpdateArticleDto updateArticleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                return Forbid();
            }

            var articleFromDb = await _articleService.GetArticleByIdWithAuthorAsync(id); // Get existing for comparison/update
            if (articleFromDb == null)
            {
                return NotFound(new { message = $"Article with ID {id} not found for update." });
            }

            _mapper.Map(updateArticleDto, articleFromDb);
            // Handle ImageUrl separately if needed based on DTO structure and image strategy
            // articleFromDb.ImageUrl = updateArticleDto.ImageUrl;

            bool success = await _articleService.UpdateArticleAsync(articleFromDb);

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Article update failed." });
            }

            return NoContent();
        }


        // DELETE: api/ArticlesApi/{id} (Article Delete)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (!await _articleService.CanUserModifyArticleAsync(id, userId))
            {
                return Forbid();
            }

            var article = await _articleService.GetArticleByIdWithAuthorAsync(id);
            if (article == null)
            {
                return NotFound(new { message = $"Article with ID {id} not found for deletion." });
            }

            bool success = await _articleService.DeleteArticleAsync(id);

            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Article deletion failed." });
            }

            return NoContent();
        }
    }
}