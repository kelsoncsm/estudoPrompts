using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");

        builder.HasKey(book => book.Id);

        builder.Property(book => book.Id)
            .HasColumnName("id");

        builder.Property(book => book.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(book => book.Author)
            .HasColumnName("author")
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(book => book.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2);
    }
}
