using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;

namespace CodeChallenge.Api.Logic;

public class MessageLogic : IMessageLogic
{
    private readonly IMessageRepository _repository;

    public MessageLogic(IMessageRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> CreateMessageAsync(Guid organizationId, CreateMessageRequest request)
    {
        var errors = ValidateTitleAndContent(request.Title, request.Content);
        if (errors.Count > 0)
        {
            return new ValidationError(errors);
        }

        var existing = await _repository.GetByTitleAsync(organizationId, request.Title.Trim());
        if (existing != null)
        {
            return new Conflict("Title must be unique per organization.");
        }

        var message = new Message
        {
            OrganizationId = organizationId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            IsActive = true
        };

        var created = await _repository.CreateAsync(message);
        return new Created<Message>(created);
    }

    public async Task<Result> UpdateMessageAsync(Guid organizationId, Guid id, UpdateMessageRequest request)
    {
        var existing = await _repository.GetByIdAsync(organizationId, id);
        if (existing == null)
        {
            return new NotFound("Message not found.");
        }

        if (!existing.IsActive)
        {
            return new ValidationError(new Dictionary<string, string[]>
            {
                ["IsActive"] = new[] { "Only active messages can be updated." }
            });
        }

        var errors = ValidateTitleAndContent(request.Title, request.Content);
        if (errors.Count > 0)
        {
            return new ValidationError(errors);
        }

        var duplicate = await _repository.GetByTitleAsync(organizationId, request.Title.Trim());
        if (duplicate != null && duplicate.Id != existing.Id)
        {
            return new Conflict("Title must be unique per organization.");
        }

        existing.Title = request.Title.Trim();
        existing.Content = request.Content.Trim();
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        if (updated == null)
        {
            return new NotFound("Message not found.");
        }

        return new Updated();
    }

    public async Task<Result> DeleteMessageAsync(Guid organizationId, Guid id)
    {
        var existing = await _repository.GetByIdAsync(organizationId, id);
        if (existing == null)
        {
            return new NotFound("Message not found.");
        }

        if (!existing.IsActive)
        {
            return new ValidationError(new Dictionary<string, string[]>
            {
                ["IsActive"] = new[] { "Only active messages can be deleted." }
            });
        }

        var deleted = await _repository.DeleteAsync(organizationId, id);
        if (!deleted)
        {
            return new NotFound("Message not found.");
        }

        return new Deleted();
    }

    public Task<Message?> GetMessageAsync(Guid organizationId, Guid id)
    {
        return _repository.GetByIdAsync(organizationId, id);
    }

    public Task<IEnumerable<Message>> GetAllMessagesAsync(Guid organizationId)
    {
        return _repository.GetAllByOrganizationAsync(organizationId);
    }

    private static Dictionary<string, string[]> ValidateTitleAndContent(string title, string content)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        var normalizedTitle = title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            errors["Title"] = new[] { "Title is required." };
        }
        else if (normalizedTitle.Length < 3 || normalizedTitle.Length > 200)
        {
            errors["Title"] = new[] { "Title must be between 3 and 200 characters." };
        }

        var normalizedContent = content?.Trim() ?? string.Empty;
        if (normalizedContent.Length < 10 || normalizedContent.Length > 1000)
        {
            errors["Content"] = new[] { "Content must be between 10 and 1000 characters." };
        }

        return errors;
    }
}
