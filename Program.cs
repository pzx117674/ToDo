using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

// Helper metody do mapowania między encjami a DTO
static TodoDto MapToTodoDto(Todo todo) => new TodoDto
{
    Id = todo.Id,
    Name = todo.Name,
    IsComplete = todo.IsComplete,
    CategoryId = todo.CategoryId,
    Category = todo.Category != null ? new CategoryDto
    {
        Id = todo.Category.Id,
        Name = todo.Category.Name,
        Description = todo.Category.Description,
        TodoCount = todo.Category.Todos.Count
    } : null
};

static CategoryDto MapToCategoryDto(Category category) => new CategoryDto
{
    Id = category.Id,
    Name = category.Name,
    Description = category.Description,
    TodoCount = category.Todos.Count
};

// ========== TODO ENDPOINTS ==========

// Pobierz wszystkie Todo z kategoriami
app.MapGet("/todoitems", async (TodoDb db) =>
{
    var todos = await db.Todos
        .Include(t => t.Category)
        .ThenInclude(c => c!.Todos)
        .ToListAsync();
    
    return todos.Select(MapToTodoDto).ToList();
});

// Pobierz ukończone Todo
app.MapGet("/todoitems/complete", async (TodoDb db) =>
{
    var todos = await db.Todos
        .Where(t => t.IsComplete)
        .Include(t => t.Category)
        .ThenInclude(c => c!.Todos)
        .ToListAsync();
    
    return todos.Select(MapToTodoDto).ToList();
});

// Pobierz Todo po ID
app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos
        .Include(t => t.Category)
        .ThenInclude(c => c!.Todos)
        .FirstOrDefaultAsync(t => t.Id == id);
    
    return todo != null 
        ? Results.Ok(MapToTodoDto(todo))
        : Results.NotFound();
});

// Utwórz nowe Todo
app.MapPost("/todoitems", async (CreateTodoDto createDto, TodoDb db) =>
{
    var todo = new Todo
    {
        Name = createDto.Name,
        IsComplete = createDto.IsComplete,
        CategoryId = createDto.CategoryId
    };

    // Sprawdź czy kategoria istnieje (jeśli podano CategoryId)
    if (createDto.CategoryId.HasValue)
    {
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == createDto.CategoryId.Value);
        if (!categoryExists)
        {
            return Results.BadRequest($"Kategoria o ID {createDto.CategoryId.Value} nie istnieje.");
        }
    }

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    // Załaduj kategorię dla odpowiedzi
    await db.Entry(todo).Reference(t => t.Category).LoadAsync();
    if (todo.Category != null)
    {
        await db.Entry(todo.Category).Collection(c => c.Todos).LoadAsync();
    }

    return Results.Created($"/todoitems/{todo.Id}", MapToTodoDto(todo));
});

// Aktualizuj Todo
app.MapPut("/todoitems/{id}", async (int id, UpdateTodoDto updateDto, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    // Sprawdź czy kategoria istnieje (jeśli podano CategoryId)
    if (updateDto.CategoryId.HasValue)
    {
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == updateDto.CategoryId.Value);
        if (!categoryExists)
        {
            return Results.BadRequest($"Kategoria o ID {updateDto.CategoryId.Value} nie istnieje.");
        }
    }

    todo.Name = updateDto.Name;
    todo.IsComplete = updateDto.IsComplete;
    todo.CategoryId = updateDto.CategoryId;

    await db.SaveChangesAsync();

    // Załaduj kategorię dla odpowiedzi
    await db.Entry(todo).Reference(t => t.Category).LoadAsync();
    if (todo.Category != null)
    {
        await db.Entry(todo.Category).Collection(c => c.Todos).LoadAsync();
    }

    return Results.Ok(MapToTodoDto(todo));
});

// Usuń Todo
app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

// Pobierz Todo według kategorii
app.MapGet("/todoitems/category/{categoryId}", async (int categoryId, TodoDb db) =>
{
    var todos = await db.Todos
        .Where(t => t.CategoryId == categoryId)
        .Include(t => t.Category)
        .ThenInclude(c => c!.Todos)
        .ToListAsync();
    
    return todos.Select(MapToTodoDto).ToList();
});

// ========== CATEGORY ENDPOINTS ==========

// Pobierz wszystkie kategorie
app.MapGet("/categories", async (TodoDb db) =>
{
    var categories = await db.Categories
        .Include(c => c.Todos)
        .ToListAsync();
    
    return categories.Select(MapToCategoryDto).ToList();
});

// Pobierz kategorię po ID
app.MapGet("/categories/{id}", async (int id, TodoDb db) =>
{
    var category = await db.Categories
        .Include(c => c.Todos)
        .FirstOrDefaultAsync(c => c.Id == id);
    
    return category != null 
        ? Results.Ok(MapToCategoryDto(category))
        : Results.NotFound();
});

// Utwórz nową kategorię
app.MapPost("/categories", async (CreateCategoryDto createDto, TodoDb db) =>
{
    var category = new Category
    {
        Name = createDto.Name,
        Description = createDto.Description
    };

    db.Categories.Add(category);
    await db.SaveChangesAsync();

    // Załaduj Todo dla odpowiedzi
    await db.Entry(category).Collection(c => c.Todos).LoadAsync();

    return Results.Created($"/categories/{category.Id}", MapToCategoryDto(category));
});

// Aktualizuj kategorię
app.MapPut("/categories/{id}", async (int id, UpdateCategoryDto updateDto, TodoDb db) =>
{
    var category = await db.Categories.FindAsync(id);

    if (category is null) return Results.NotFound();

    category.Name = updateDto.Name;
    category.Description = updateDto.Description;

    await db.SaveChangesAsync();

    // Załaduj Todo dla odpowiedzi
    await db.Entry(category).Collection(c => c.Todos).LoadAsync();

    return Results.Ok(MapToCategoryDto(category));
});

// Usuń kategorię
app.MapDelete("/categories/{id}", async (int id, TodoDb db) =>
{
    if (await db.Categories.FindAsync(id) is Category category)
    {
        // Relacja jest skonfigurowana jako SetNull, więc Todo nie zostaną usunięte,
        // tylko ich CategoryId zostanie ustawione na null
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();
