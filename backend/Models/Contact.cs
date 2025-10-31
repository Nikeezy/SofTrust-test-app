using System.Collections.Generic;

namespace backend.Models;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
