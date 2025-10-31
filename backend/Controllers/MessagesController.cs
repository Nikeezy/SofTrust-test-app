using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

    private readonly AppDbContext _db;
    private readonly IRecaptchaService _recaptcha;

    public MessagesController(AppDbContext db, IRecaptchaService recaptcha)
    {
        _db = db;
        _recaptcha = recaptcha;
    }

    [HttpGet("/api/topics")]
    public async Task<ActionResult<TopicDto[]>> GetTopics(CancellationToken cancellationToken)
    {
        var items = await _db.MessageTopics
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new TopicDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToArrayAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MessageResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var message = await _db.Messages
            .AsNoTracking()
            .Include(x => x.Contact)
            .Include(x => x.Topic)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (message == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(message));
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponseDto>> Create(CreateMessageRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError != null)
        {
            return BadRequest(new { error = validationError });
        }

        var captchaOk = await _recaptcha.VerifyAsync(request.RecaptchaToken, cancellationToken);
        if (!captchaOk)
        {
            return BadRequest(new { error = "Не удалось подтвердить CAPTCHA." });
        }

        var topic = await _db.MessageTopics.FirstOrDefaultAsync(x => x.Id == request.TopicId, cancellationToken);
        if (topic == null)
        {
            return BadRequest(new { error = "Выбранная тема не найдена." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var phone = NormalizePhone(request.Phone);

        var contact = await _db.Contacts.FirstOrDefaultAsync(
            x => x.Email == email && x.Phone == phone,
            cancellationToken);

        if (contact == null)
        {
            contact = new Contact
            {
                Name = request.Name.Trim(),
                Email = email,
                Phone = phone
            };
            _db.Contacts.Add(contact);
        }
        else
        {
            contact.Name = request.Name.Trim();
        }

        var message = new Message
        {
            Contact = contact,
            Topic = topic,
            Text = request.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(message).Reference(x => x.Contact).LoadAsync(cancellationToken);
        await _db.Entry(message).Reference(x => x.Topic).LoadAsync(cancellationToken);

        var dto = MapToDto(message);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    private static string? Validate(CreateMessageRequest request)
    {
        if (request == null)
        {
            return "Пустой запрос.";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Имя нужно заполнить.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email обязателен.";
        }

        if (!EmailRegex.IsMatch(request.Email.Trim()))
        {
            return "Email указан неверно.";
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return "Телефон обязателен.";
        }

        var digits = new string(request.Phone.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || digits[0] != '7')
        {
            return "Телефон должен быть в формате +7XXXXXXXXXX.";
        }

        if (request.TopicId <= 0)
        {
            return "Выберите тему.";
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return "Текст сообщения обязателен.";
        }

        if (string.IsNullOrWhiteSpace(request.RecaptchaToken))
        {
            return "Подтвердите, что вы не робот.";
        }

        return null;
    }

    private static string NormalizePhone(string input)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return "+" + digits;
    }

    private static MessageResponseDto MapToDto(Message message)
    {
        var digits = message.Contact.Phone;
        var phone = digits.Length == 12
            ? $"+{digits[1]} ({digits.Substring(2, 3)}) {digits.Substring(5, 3)}-{digits.Substring(8, 2)}-{digits.Substring(10, 2)}"
            : digits;

        return new MessageResponseDto
        {
            Id = message.Id,
            Name = message.Contact.Name,
            Email = message.Contact.Email,
            Phone = phone,
            TopicId = message.Topic.Id,
            TopicName = message.Topic.Name,
            Text = message.Text,
            CreatedAt = message.CreatedAt
        };
    }
}
