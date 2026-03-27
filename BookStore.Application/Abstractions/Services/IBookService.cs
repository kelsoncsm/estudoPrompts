using BookStore.Application.DTOs.Books;

namespace BookStore.Application.Abstractions.Services;

public interface IBookService
{
    Task<IReadOnlyCollection<BookDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BookDto> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<BookDto> CreateAsync(CreateBookDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid bookId, UpdateBookDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid bookId, CancellationToken cancellationToken = default);
}
