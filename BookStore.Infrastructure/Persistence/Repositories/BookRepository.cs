using BookStore.Application.Abstractions.Persistence;
using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence.Repositories;

public class BookRepository : IBookRepository
{
    private readonly AppDbContext _dbContext;

    public BookRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Book>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Books
            .AsNoTracking()
            .OrderBy(book => book.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Book?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Books
            .FirstOrDefaultAsync(book => book.Id == bookId, cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        await _dbContext.Books.AddAsync(book, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        _dbContext.Books.Update(book);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Book book, CancellationToken cancellationToken = default)
    {
        _dbContext.Books.Remove(book);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
