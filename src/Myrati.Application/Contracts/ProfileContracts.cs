namespace Myrati.Application.Contracts;

public sealed record ProfileInfoDto(
    string Id,
    string Name,
    string Email,
    string Phone,
    string Role,
    string Department,
    string Location);

public sealed record ProfileSessionDto(string Id, string Location, string LastActive, bool Current);

public sealed record ProfileActivityDto(string Action, string Date);

public sealed record ProfileSnapshotDto(
    ProfileInfoDto Profile,
    IReadOnlyCollection<ProfileSessionDto> ActiveSessions,
    IReadOnlyCollection<ProfileActivityDto> ActivityLog);

public sealed record UpdateProfileRequest(
    string Name,
    string Email,
    string Phone,
    string Department,
    string Location);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
