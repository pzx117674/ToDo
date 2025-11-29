public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Navigation property - kolekcja Todo należących do tej kategorii
    public ICollection<Todo> Todos { get; set; } = new List<Todo>();
}

