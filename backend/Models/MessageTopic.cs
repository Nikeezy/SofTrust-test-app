using System.Collections.Generic;

namespace backend.Models;

public class MessageTopic
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
