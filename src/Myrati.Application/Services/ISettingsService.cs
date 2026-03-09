using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface ISettingsService
{
    Task<SettingsSnapshotDto> GetAsync(CancellationToken cancellationToken = default);
    Task<SettingsSnapshotDto> UpdateAsync(UpdateSettingsRequest request, CancellationToken cancellationToken = default);
    Task<ApiKeyDto> CreateApiKeyAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default);
    Task<ApiKeyDto> RotateApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default);
    Task<ApiKeyDto> ToggleApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default);
    Task DeleteApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default);
    Task<TeamMemberDto> CreateTeamMemberAsync(CreateTeamMemberRequest request, CancellationToken cancellationToken = default);
    Task<TeamMemberDto> UpdateTeamMemberAsync(string teamMemberId, UpdateTeamMemberRequest request, CancellationToken cancellationToken = default);
    Task DeleteTeamMemberAsync(string teamMemberId, CancellationToken cancellationToken = default);
}
