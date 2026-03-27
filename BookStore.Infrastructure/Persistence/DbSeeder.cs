using BookStore.Domain.Entities;

namespace BookStore.Infrastructure.Persistence;

public static class DbSeeder
{
    public static void Seed(AppDbContext dbContext)
    {
        if (dbContext.Books.Any())
        {
            return;
        }

        var seedBooks = new[]
        {
            new Book("Clean Code", "Robert C. Martin", 99.90m),
            new Book("Domain-Driven Design", "Eric Evans", 149.90m)
        };

        dbContext.Books.AddRange(seedBooks);
        dbContext.SaveChanges();
    }
}
