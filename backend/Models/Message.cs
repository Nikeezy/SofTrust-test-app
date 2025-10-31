using System;

namespace backend.Models;

public class Message
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public int TopicId { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Contact Contact { get; set; } = null!;
    public MessageTopic Topic { get; set; } = null!;
}
