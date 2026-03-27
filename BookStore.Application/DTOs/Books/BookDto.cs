namespace BookStore.Application.DTOs.Books;

public sealed class BookDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
