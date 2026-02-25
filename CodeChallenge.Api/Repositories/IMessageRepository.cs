using CodeChallenge.Api.Models;

namespace CodeChallenge.Api.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid organizationId, Guid id);
    Task<IEnumerable<Message>> GetAllByOrganizationAsync(Guid organizationId);
    Task<Message?> GetByTitleAsync(Guid organizationId, string title);
    Task<Message> CreateAsync(Message message);
    Task<Message?> UpdateAsync(Message message);
    Task<bool> DeleteAsync(Guid organizationId, Guid id);
}
