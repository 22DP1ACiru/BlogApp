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
    }
}