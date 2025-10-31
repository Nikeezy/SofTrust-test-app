namespace backend.Services;

public class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    public string SecretKey { get; set; } = string.Empty;
}
