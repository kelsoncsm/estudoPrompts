# BookStore - Arquitetura e Fluxo do CRUD

## Objetivo
Este documento explica a arquitetura criada para a solu��o `BookStore`, o papel de cada camada, como elas se relacionam e como o CRUD de livros funciona na pr�tica.

A solu��o foi organizada para manter:
- baixo acoplamento
- responsabilidades bem definidas
- facilidade de manuten��o
- regras de neg�cio isoladas
- infraestrutura troc�vel sem impactar a regra da aplica��o

## Vis�o geral da solu��o
A solu��o est� dividida em 4 projetos:

```text
BookStore.sln
+-- BookStore.Api
+-- BookStore.Application
+-- BookStore.Domain
+-- BookStore.Infrastructure
```

### Papel de cada projeto
- `BookStore.Api`: camada de entrada HTTP. Recebe requisi��es, envia respostas e configura o pipeline da aplica��o.
- `BookStore.Application`: camada de casos de uso. Orquestra o fluxo da aplica��o, define contratos, DTOs e servi�os.
- `BookStore.Domain`: camada central do neg�cio. Cont�m entidades e regras de dom�nio.
- `BookStore.Infrastructure`: camada de persist�ncia e integra��es t�cnicas. Implementa EF Core, reposit�rios, migrations e inje��o de depend�ncia.

## Depend�ncia entre camadas
A dire��o da depend�ncia segue esta ideia:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application -> Domain
```

O ponto importante �:
- `Domain` n�o depende de ningu�m.
- `Application` depende do `Domain`.
- `Infrastructure` depende de `Application` e `Domain`.
- `Api` depende de `Application` e `Infrastructure`.

Isso evita que regra de neg�cio fique presa a banco de dados, controller ou framework web.

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

### Servi�o de aplicação
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
O `AppDbContext` representa a sess�o com o banco.

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
A infraestrutura exp�e uma extensão para registrar tudo de uma vez.

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
O `Program.cs` faz a composi��o da aplica��o.

Funções principais:

adiciona controllers
configura resposta padronizada para erro de validação
registra a infraestrutura
adiciona middleware de exceções
aplica migrations automaticamente
faz seed inicial de dados

## Tratamento de erros
Foram criados dois n�veis de tratamento:

### 1. Erro de valida��o de entrada
Se o JSON enviado para a API estiver inv�lido, o ASP.NET devolve `400 Bad Request` com `ValidationProblemDetails`.

Exemplo de entrada inv�lida:
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

### 2. Erro de dom�nio e erro de aplica��o
Se o problema for de regra de neg�cio ou recurso inexistente, o middleware global converte a exce��o para HTTP.

Mapeamento:
- `DomainValidationException` -> `400`
- `NotFoundException` -> `404`
- qualquer outra exce��o -> `500`

## Fluxo completo do CRUD

## CREATE - Criar livro
### Requisi��o
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
5. A entidade valida seus pr�prios dados.
6. O servi�o chama `IBookRepository.AddAsync`.
7. O `BookRepository` usa EF Core para persistir.
8. O servi�o converte a entidade para `BookDto`.
9. O controller retorna `201 Created`.

### C�digo simplificado
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
### Requisi��o
`GET /api/books`

### Passo a passo interno
1. O controller chama `GetAllAsync`.
2. O servi�o consulta o reposit�rio.
3. O reposit�rio consulta o banco com `AsNoTracking()`.
4. O servi�o converte entidades para `BookDto`.
5. O controller responde `200 OK`.

### C�digo simplificado
```csharp
var books = await _bookRepository.GetAllAsync(cancellationToken);
return books.Select(MapToDto).ToArray();
```

## READ BY ID - Buscar livro por id
### Requisi��o
`GET /api/books/{bookId}`

### Passo a passo interno
1. O controller recebe `bookId`.
2. O servi�o consulta o reposit�rio.
3. Se n�o encontrar, lan�a `NotFoundException`.
4. O middleware global converte isso para `404`.
5. Se encontrar, retorna `200 OK`.

### C�digo simplificado
```csharp
var book = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
if (book is null)
{
    throw new NotFoundException($"Book with id '{bookId}' was not found.");
}
```

## UPDATE - Atualizar livro
### Requisi��o
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
3. O servi�o busca o livro no reposit�rio.
4. Se n�o existir, lan�a `NotFoundException`.
5. Se existir, chama `existingBook.UpdateDetails(...)`.
6. A entidade reaplica valida��es de dom�nio.
7. O reposit�rio persiste a atualiza��o.
8. O controller retorna `204 No Content`.

### C�digo simplificado
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
### Requisi��o
`DELETE /api/books/{bookId}`

### Passo a passo interno
1. O controller recebe o `bookId`.
2. O servi�o busca o livro.
3. Se n�o existir, lan�a `NotFoundException`.
4. Se existir, chama o reposit�rio para remo��o.
5. O controller retorna `204 No Content`.

### C�digo simplificado
```csharp
var existingBook = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
if (existingBook is null)
{
    throw new NotFoundException(...);
}

await _bookRepository.DeleteAsync(existingBook, cancellationToken);
```

## Por que essa arquitetura � boa para estudo
Essa estrutura � �til porque mostra separa��o real de responsabilidades:

- a API n�o conhece EF Core
- a regra de neg�cio n�o conhece controller
- o reposit�rio n�o define regra de neg�cio
- o dom�nio n�o depende de infraestrutura
- a troca de banco afeta principalmente a infraestrutura

## Benef�cios pr�ticos
- mais f�cil testar o `BookService`
- mais f�cil trocar `PostgreSQL` por outro banco
- mais f�cil localizar responsabilidades
- menor risco de duplicar regra em v�rios lugares
- controller mais limpo
- tratamento de erro padronizado

## Limita��es atuais
A arquitetura est� boa para estudo e projetos pequenos/m�dios, mas ainda pode evoluir.

Poss�veis melhorias:
- usar `FluentValidation`
- usar `MediatR` para separar comandos e queries
- criar respostas padronizadas de sucesso e erro
- adicionar testes unit�rios e testes de integra��o
- usar transa��es em cen�rios mais complexos
- introduzir logs estruturados por caso de uso

## Resumo final
### Domain
Respons�vel por:
- entidade `Book`
- valida��es centrais do neg�cio

### Application
Respons�vel por:
- contratos
- DTOs
- casos de uso
- fluxo do CRUD

### Infrastructure
Respons�vel por:
- EF Core
- PostgreSQL
- reposit�rios
- migrations
- seed
- registro de depend�ncias

### Api
Respons�vel por:
- endpoints HTTP
- bind de request/response
- valida��o autom�tica de entrada
- middleware global de exce��es
- inicializa��o da aplica��o

## Arquivos mais importantes para estudar primeiro
1. [Book.cs](d:\source\repos\estudoPrompts\BookStore.Domain\Entities\Book.cs)
2. [BookService.cs](d:\source\repos\estudoPrompts\BookStore.Application\Services\BookService.cs)
3. [BookRepository.cs](d:\source\repos\estudoPrompts\BookStore.Infrastructure\Persistence\Repositories\BookRepository.cs)
4. [BooksController.cs](d:\source\repos\estudoPrompts\BookStore.Api\Controllers\BooksController.cs)
5. [Program.cs](d:\source\repos\estudoPrompts\BookStore.Api\Program.cs)
6. [ExceptionHandlingMiddleware.cs](d:\source\repos\estudoPrompts\BookStore.Api\Middlewares\ExceptionHandlingMiddleware.cs)
