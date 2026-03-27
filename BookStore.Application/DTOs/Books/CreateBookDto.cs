using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Books;

public sealed class CreateBookDto
{
    [Required]
    [MaxLength(150)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Author { get; init; } = string.Empty;

    [Range(0.01, 999999.99)]
    public decimal Price { get; init; }
}
