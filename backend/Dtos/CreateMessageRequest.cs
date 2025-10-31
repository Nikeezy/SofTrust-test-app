namespace backend.Dtos;

public class CreateMessageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TopicId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string RecaptchaToken { get; set; } = string.Empty;
}
