// DTOs do uniknięcia cykli w serializacji JSON

public class TodoDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public int? CategoryId { get; set; }
    public CategoryDto? Category { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TodoCount { get; set; } // Liczba Todo w kategorii (bez pełnej listy, aby uniknąć cyklu)
}

public class CreateTodoDto
{
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public int? CategoryId { get; set; }
}

public class UpdateTodoDto
{
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public int? CategoryId { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

