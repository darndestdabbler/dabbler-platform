namespace AcmeCoffeeOnline.Shared.Models;

public class OrderNote
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public DateTime NoteDate { get; set; }
    public string Author { get; set; } = string.Empty;

    public Order Order { get; set; } = null!;
}
