using BookStore.Application.Abstractions.Services;
using BookStore.Application.DTOs.Books;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<BookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<BookDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var books = await _bookService.GetAllAsync(cancellationToken);
        return Ok(books);
    }

    [HttpGet("{bookId:guid}")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> GetByIdAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await _bookService.GetByIdAsync(bookId, cancellationToken);
        return Ok(book);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookDto>> CreateAsync(
        [FromBody] CreateBookDto request,
        CancellationToken cancellationToken)
    {
        var createdBook = await _bookService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { bookId = createdBook.Id }, createdBook);
    }

    [HttpPut("{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        Guid bookId,
        [FromBody] UpdateBookDto request,
        CancellationToken cancellationToken)
    {
        await _bookService.UpdateAsync(bookId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid bookId, CancellationToken cancellationToken)
    {
        await _bookService.DeleteAsync(bookId, cancellationToken);
        return NoContent();
    }
}
