﻿using BlogApp.DAL.Data;
using BlogApp.DAL.Interfaces;
using BlogApp.DAL.Repositories;

namespace BlogApp.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IArticleRepository? _articleRepository;
        private IArticleVoteRepository? _articleVoteRepository;
        private ICommentRepository? _commentRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IArticleRepository Articles => _articleRepository ??= new ArticleRepository(_context);
        public IArticleVoteRepository ArticleVotes => _articleVoteRepository ??= new ArticleVoteRepository(_context);
        public ICommentRepository Comments => _commentRepository ??= new CommentRepository(_context);

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Disposes the database context.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
            System.GC.SuppressFinalize(this);
        }
    }
}