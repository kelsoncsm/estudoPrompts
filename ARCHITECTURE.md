# BookStore - Arquitetura e Fluxo do CRUD

## Objetivo
Este documento explica a arquitetura criada para a solução `BookStore`, o papel de cada camada, como elas se relacionam e como o CRUD de livros funciona na prática.

A solução foi organizada para manter:
- baixo acoplamento
- responsabilidades bem definidas
- facilidade de manutenção
- regras de negócio isoladas
- infraestrutura trocável sem impactar a regra da aplicação

## Visão geral da solução
A solução está dividida em 4 projetos:

```text
BookStore.sln
+-- BookStore.Api
+-- BookStore.Application
+-- BookStore.Domain
+-- BookStore.Infrastructure
```

### Papel de cada projeto
- `BookStore.Api`: camada de entrada HTTP. Recebe requisições, envia respostas e configura o pipeline da aplicação.
- `BookStore.Application`: camada de casos de uso. Orquestra o fluxo da aplicação, define contratos, DTOs e serviços.
- `BookStore.Domain`: camada central do negócio. Contém entidades e regras de domínio.
- `BookStore.Infrastructure`: camada de persistência e integrações técnicas. Implementa EF Core, repositórios, migrations e injeção de dependência.

## Dependência entre camadas
A direção da dependência segue esta ideia:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application -> Domain
```

O ponto importante é:
- `Domain` néo depende de ninguém.
- `Application` depende do `Domain`.
- `Infrastructure` depende de `Application` e `Domain`.
- `Api` depende de `Application` e `Infrastructure`.

Isso evita que regra de negócio fique presa a banco de dados, controller ou framework web.

## Estrutura detalhada

### 1. Domain
Caminho principal:
- [Book.cs](d:\source\repos\estudoPrompts\BookStore.Domain\Entities\Book.cs)
- [DomainValidationException.cs](d:\source\repos\estudoPrompts\BookStore.Domain\Exceptions\DomainValidationException.cs)

Responsabilidade:
- representar o conceito de Livro
- proteger a consistência dos dados
- centralizar validações de domínio

Exemplo:
```csharp
public class Book
{
    public Book(string title, string author, decimal price)
    {
        Id = Guid.NewGuid();
        SetDetails(title, author, price);
    }

    public void UpdateDetails(string title, string author, decimal price)
    {
        SetDetails(title, author, price);
    }
}
```

### O que essa entidade faz
- garante que título não seja vazio
- garante que autor não seja vazio
- garante que título e autor respeitem tamanho máximo
- garante que preço seja maior que zero

Isso é importante porque a entidade continua protegida mesmo que a chamada venha de outro lugar além da API.

## 2. Application
Caminhos principais:
- [IBookService.cs](d:\source\repos\estudoPrompts\BookStore.Application\Abstractions\Services\IBookService.cs)
- [IBookRepository.cs](d:\source\repos\estudoPrompts\BookStore.Application\Abstractions\Persistence\IBookRepository.cs)
- [BookService.cs](d:\source\repos\estudoPrompts\BookStore.Application\Services\BookService.cs)
- [BookDto.cs](d:\source\repos\estudoPrompts\BookStore.Application\DTOs\Books\BookDto.cs)
- [CreateBookDto.cs](d:\source\repos\estudoPrompts\BookStore.Application\DTOs\Books\CreateBookDto.cs)
- [UpdateBookDto.cs](d:\source\repos\estudoPrompts\BookStore.Application\DTOs\Books\UpdateBookDto.cs)
- [NotFoundException.cs](d:\source\repos\estudoPrompts\BookStore.Application\Exceptions\NotFoundException.cs)

Responsabilidade:
- definir contratos da aplicação
- coordenar o fluxo do CRUD
- converter entidade em DTO
- desacoplar regra de negócio da persistência

### DTOs
Os DTOs existem para transportar dados entre a API e a aplicação.

Temos 3 DTOs principais:
- `CreateBookDto`: usado no `POST`
- `UpdateBookDto`: usado no `PUT`
- `BookDto`: usado nas respostas

Exemplo:
```csharp
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
```

Essas validações protegem a entrada HTTP. Já a entidade Book protege o domínio.

### Serviéo de aplicação
O serviço de aplicação é o ponto central do CRUD.

Exemplo:
```csharp
public async Task<BookDto> CreateAsync(CreateBookDto request, CancellationToken cancellationToken = default)
{
    var book = new Book(request.Title, request.Author, request.Price);
    await _bookRepository.AddAsync(book, cancellationToken);
    return MapToDto(book);
}
```

O `BookService`:
recebe o DTO
cria ou altera a entidade
usa o repositório via interface
devolve DTOs para a API
lança exceções de negócio quando necessário

## 3. Infrastructure
Caminhos principais:
- [DependencyInjection.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\DependencyInjection.cs)
- [AppDbContext.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\Persistence\AppDbContext.cs)
- [BookConfiguration.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\Persistence\Configurations\BookConfiguration.cs)
- [BookRepository.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\Persistence\Repositories\BookRepository.cs)
- [DbSeeder.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\Persistence\DbSeeder.cs)
- [20260327171851_InitialCreate.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\Persistence\Migrations\20260327171851_InitialCreate.cs)

Responsabilidade:
- configurar EF Core
- acessar PostgreSQL
- implementar repositórios
- manter migrations
- registrar dependências no container

### DbContext
O `AppDbContext` representa a sesséo com o banco.

Exemplo:
```csharp
public class AppDbContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### Configuração da entidade
O mapeamento da entidade fica separado da entidade em si.

Exemplo:
```csharp
builder.ToTable("books");

builder.Property(book => book.Title)
    .HasColumnName("title")
    .IsRequired()
    .HasMaxLength(150);
```
Essa separação evita misturar regra de negócio com detalhes do banco.

### repositório
O repositório implementa a interface definida na camada `Application`.

Exemplo:
```csharp
public async Task<IReadOnlyCollection<Book>> GetAllAsync(CancellationToken cancellationToken = default)
{
    return await _dbContext.Books
        .AsNoTracking()
        .OrderBy(book => book.Title)
        .ToListAsync(cancellationToken);
}
```

Funções do repositório:
- consultar dados
- inserir dados
- atualizar dados
- remover dados
- esconder detalhes do EF Core do restante da aplicação

### Dependency Injection
A infraestrutura expée uma extensão para registrar tudo de uma vez.

Exemplo:
```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

services.AddScoped<IBookRepository, BookRepository>();
services.AddScoped<IBookService, BookService>();
```

## 4. Api
Caminhos principais:
- [Program.cs](d:\source\repos\estudoPrompts\BookStore.Api\Program.cs)
- [BooksController.cs](d:\source\repos\estudoPrompts\BookStore.Api\Controllers\BooksController.cs)
- [ExceptionHandlingMiddleware.cs](d:\source\repos\estudoPrompts\BookStore.Api\Middlewares\ExceptionHandlingMiddleware.cs)

Responsabilidade:
expor endpoints HTTP
receber JSON
devolver respostas HTTP
configurar middleware, validação automática e tratamento global de erros

### Program.cs
O `Program.cs` faz a composiééo da aplicação.

Funções principais:

adiciona controllers
configura resposta padronizada para erro de validação
registra a infraestrutura
adiciona middleware de exceções
aplica migrations automaticamente
faz seed inicial de dados

## Tratamento de erros
Foram criados dois néveis de tratamento:

### 1. Erro de validaééo de entrada
Se o JSON enviado para a API estiver invélido, o ASP.NET devolve `400 Bad Request` com `ValidationProblemDetails`.

Exemplo de entrada invélida:
```json
{
  "title": "",
  "author": "Autor X",
  "price": 0
}
```

Exemplo de resposta:
```json
{
  "title": "Validation error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Title": ["The Title field is required."],
    "Price": ["The field Price must be between 0.01 and 999999.99."]
  }
}
```

### 2. Erro de domínio e erro de aplicação
Se o problema for de regra de negócio ou recurso inexistente, o middleware global converte a exceééo para HTTP.

Mapeamento:
- `DomainValidationException` -> `400`
- `NotFoundException` -> `404`
- qualquer outra exceééo -> `500`

## Fluxo completo do CRUD

## CREATE - Criar livro
### Requisiééo
`POST /api/books`

Payload:
```json
{
  "title": "Clean Architecture",
  "author": "Robert C. Martin",
  "price": 120.00
}
```

### Passo a passo interno
1. O `BooksController` recebe o request.
2. O ASP.NET valida `CreateBookDto`.
3. O controller chama `IBookService.CreateAsync`.
4. O `BookService` cria a entidade `Book`.
5. A entidade valida seus préprios dados.
6. O serviéo chama `IBookRepository.AddAsync`.
7. O `BookRepository` usa EF Core para persistir.
8. O serviéo converte a entidade para `BookDto`.
9. O controller retorna `201 Created`.

### Cédigo simplificado
Controller:
```csharp
var createdBook = await _bookService.CreateAsync(request, cancellationToken);
return CreatedAtAction(nameof(GetByIdAsync), new { bookId = createdBook.Id }, createdBook);
```

Service:
```csharp
var book = new Book(request.Title, request.Author, request.Price);
await _bookRepository.AddAsync(book, cancellationToken);
return MapToDto(book);
```

Repository:
```csharp
await _dbContext.Books.AddAsync(book, cancellationToken);
await _dbContext.SaveChangesAsync(cancellationToken);
```

## READ - Listar livros
### Requisiééo
`GET /api/books`

### Passo a passo interno
1. O controller chama `GetAllAsync`.
2. O serviéo consulta o repositério.
3. O repositério consulta o banco com `AsNoTracking()`.
4. O serviéo converte entidades para `BookDto`.
5. O controller responde `200 OK`.

### Cédigo simplificado
```csharp
var books = await _bookRepository.GetAllAsync(cancellationToken);
return books.Select(MapToDto).ToArray();
```

## READ BY ID - Buscar livro por id
### Requisiééo
`GET /api/books/{bookId}`

### Passo a passo interno
1. O controller recebe `bookId`.
2. O serviéo consulta o repositério.
3. Se néo encontrar, lanéa `NotFoundException`.
4. O middleware global converte isso para `404`.
5. Se encontrar, retorna `200 OK`.

### Cédigo simplificado
```csharp
var book = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
if (book is null)
{
    throw new NotFoundException($"Book with id '{bookId}' was not found.");
}
```

## UPDATE - Atualizar livro
### Requisiééo
`PUT /api/books/{bookId}`

Payload:
```json
{
  "title": "Clean Architecture - Updated",
  "author": "Robert C. Martin",
  "price": 135.50
}
```

### Passo a passo interno
1. O controller recebe `bookId` e o body.
2. O ASP.NET valida `UpdateBookDto`.
3. O serviéo busca o livro no repositério.
4. Se néo existir, lanéa `NotFoundException`.
5. Se existir, chama `existingBook.UpdateDetails(...)`.
6. A entidade reaplica validaéées de domínio.
7. O repositério persiste a atualizaééo.
8. O controller retorna `204 No Content`.

### Cédigo simplificado
```csharp
var existingBook = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
if (existingBook is null)
{
    throw new NotFoundException(...);
}

existingBook.UpdateDetails(request.Title, request.Author, request.Price);
await _bookRepository.UpdateAsync(existingBook, cancellationToken);
```

## DELETE - Remover livro
### Requisiééo
`DELETE /api/books/{bookId}`

### Passo a passo interno
1. O controller recebe o `bookId`.
2. O serviéo busca o livro.
3. Se néo existir, lanéa `NotFoundException`.
4. Se existir, chama o repositério para remoééo.
5. O controller retorna `204 No Content`.

### Cédigo simplificado
```csharp
var existingBook = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
if (existingBook is null)
{
    throw new NotFoundException(...);
}

await _bookRepository.DeleteAsync(existingBook, cancellationToken);
```

## Por que essa arquitetura é boa para estudo
Essa estrutura é étil porque mostra separaééo real de responsabilidades:

- a API néo conhece EF Core
- a regra de negócio néo conhece controller
- o repositério néo define regra de negócio
- o domínio néo depende de infraestrutura
- a troca de banco afeta principalmente a infraestrutura

## Benefécios préticos
- mais fécil testar o `BookService`
- mais fécil trocar `PostgreSQL` por outro banco
- mais fécil localizar responsabilidades
- menor risco de duplicar regra em vérios lugares
- controller mais limpo
- tratamento de erro padronizado

## Limitaéées atuais
A arquitetura está boa para estudo e projetos pequenos/médios, mas ainda pode evoluir.

Posséveis melhorias:
- usar `FluentValidation`
- usar `MediatR` para separar comandos e queries
- criar respostas padronizadas de sucesso e erro
- adicionar testes unitérios e testes de integraééo
- usar transaéées em cenérios mais complexos
- introduzir logs estruturados por caso de uso

## Resumo final
### Domain
Responsével por:
- entidade `Book`
- validaéées centrais do negócio

### Application
Responsével por:
- contratos
- DTOs
- casos de uso
- fluxo do CRUD

### Infrastructure
Responsével por:
- EF Core
- PostgreSQL
- repositórios
- migrations
- seed
- registro de dependências

### Api
Responsével por:
- endpoints HTTP
- bind de request/response
- validaééo automética de entrada
- middleware global de exceéées
- inicializaééo da aplicação

## Arquivos mais importantes para estudar primeiro
1. [Book.cs](d:\source\repos\estudoPrompts\BookStore.Domain\Entities\Book.cs)
2. [BookService.cs](d:\source\repos\estudoPrompts\BookStore.Application\Services\BookService.cs)
3. [BookRepository.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\Persistence\Repositories\BookRepository.cs)
4. [BooksController.cs](d:\source\repos\estudoPrompts\BookStore.Api\Controllers\BooksController.cs)
5. [Program.cs](d:\source\repos\estudoPrompts\BookStore.Api\Program.cs)
6. [ExceptionHandlingMiddleware.cs](d:\source\repos\estudoPrompts\BookStore.Api\Middlewares\ExceptionHandlingMiddleware.cs)
