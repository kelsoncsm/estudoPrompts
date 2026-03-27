using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

public class Book
{
    private Book()
    {
    }

    public Book(string title, string author, decimal price)
    {
        Id = Guid.NewGuid();
        SetDetails(title, author, price);
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    public void UpdateDetails(string title, string author, decimal price)
    {
        SetDetails(title, author, price);
    }

    private void SetDetails(string title, string author, decimal price)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            throw new DomainValidationException("Author is required.");
        }

        if (title.Trim().Length > 150)
        {
            throw new DomainValidationException("Title must have at most 150 characters.");
        }

        if (author.Trim().Length > 120)
        {
            throw new DomainValidationException("Author must have at most 120 characters.");
        }

        if (price <= 0)
        {
            throw new DomainValidationException("Price must be greater than zero.");
        }

        Title = title.Trim();
        Author = author.Trim();
        Price = price;
    }
}
