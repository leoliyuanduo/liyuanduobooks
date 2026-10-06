using BupaBooks.Application.Domain;
using BupaBooks.Application.Paging;

namespace BupaBooks.Application.Books.GetBooks;

/// <summary>Input to the "get books" use case. Already validated by the caller.</summary>
public sealed record GetBooksQuery(BookCriteria Criteria, PageRequest Page);
