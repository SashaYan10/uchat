namespace uchat.Models;

public class Message
{
    public int Id { get; set; }
    public string SenderUsername { get; set; }
    public int ChatId { get; set; }
    public string Text { get; set; }
    public string Timestamp { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
}