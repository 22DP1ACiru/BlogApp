using BlogApp.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApp.DAL.Interfaces
{
    public interface IArticleRepository : IRepository<Article>
    {
        Task<IEnumerable<Article>> GetAllPublishedArticlesAsync();
        Task<IEnumerable<Article>> GetArticlesWithAuthorsAsync();
        Task<Article?> GetArticleWithAuthorAsync(int id);
        Task<IEnumerable<Article>> GetArticlesByAuthorIdAsync(string authorId);
        Task<IEnumerable<Article>> GetLatestPublishedArticlesAsync(int count);
        Task<IEnumerable<Article>> GetTopRankedArticlesAsync(int count);
        Task<IEnumerable<Article>> GetLastCommentedArticlesAsync(int count);
        Task<IEnumerable<Article>> SearchPublishedArticlesAsync(string searchTerm);
    }
}