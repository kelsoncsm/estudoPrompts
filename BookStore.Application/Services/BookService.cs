using BookStore.Application.Abstractions.Persistence;
using BookStore.Application.Abstractions.Services;
using BookStore.Application.DTOs.Books;
using BookStore.Application.Exceptions;
using BookStore.Domain.Entities;

namespace BookStore.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<IReadOnlyCollection<BookDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var books = await _bookRepository.GetAllAsync(cancellationToken);
        return books.Select(MapToDto).ToArray();
    }

    public async Task<BookDto> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var book = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (book is null)
        {
            throw new NotFoundException($"Book with id '{bookId}' was not found.");
        }

        return MapToDto(book);
    }

    public async Task<BookDto> CreateAsync(CreateBookDto request, CancellationToken cancellationToken = default)
    {
        var book = new Book(request.Title, request.Author, request.Price);
        await _bookRepository.AddAsync(book, cancellationToken);
        return MapToDto(book);
    }

    public async Task UpdateAsync(Guid bookId, UpdateBookDto request, CancellationToken cancellationToken = default)
    {
        var existingBook = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (existingBook is null)
        {
            throw new NotFoundException($"Book with id '{bookId}' was not found.");
        }

        existingBook.UpdateDetails(request.Title, request.Author, request.Price);
        await _bookRepository.UpdateAsync(existingBook, cancellationToken);
    }

    public async Task DeleteAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var existingBook = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (existingBook is null)
        {
            throw new NotFoundException($"Book with id '{bookId}' was not found.");
        }

        await _bookRepository.DeleteAsync(existingBook, cancellationToken);
    }

    private static BookDto MapToDto(Book book)
    {
        return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Price = book.Price
        };
    }
}
