namespace Application.Common.Services.SecurityManager;

public interface ISecurityService
{
    Task<LoginResultDto> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
        );

    Task<LogoutResultDto> LogoutAsync(
        string userId,
        CancellationToken cancellationToken = default
        );

    Task<RegisterResultDto> RegisterAsync(
        string email,
        string password,
        string confirmPassword,
        string firstName,
        string lastName,
        string companyName = "",
        CancellationToken cancellationToken = default
        );

    Task<string> ConfirmEmailAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default
        );

    Task<string> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default
        );

    Task<string> ForgotPasswordConfirmationAsync(
        string email,
        string newPassword,
        string code,
        CancellationToken cancellationToken = default
        );

    Task<RefreshTokenResultDto> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken
        );

    Task<List<GetMyProfileListResultDto>> GetMyProfileListAsync(
        string userId,
        CancellationToken cancellationToken
        );

    Task UpdateMyProfileAsync(
        string userId,
        string firstName,
        string lastName,
        string companyName,
        CancellationToken cancellationToken
        );

    Task ChangePasswordAsync(
        string userId,
        string oldPassword,
        string newPassword,
        string confirmNewPassword,
        CancellationToken cancellationToken
        );

    Task<List<GetRoleListResultDto>> GetRoleListAsync(
        CancellationToken cancellationToken
        );

    Task<List<GetUserListResultDto>> GetUserListAsync(
        CancellationToken cancellationToken
        );

    Task<CreateUserResultDto> CreateUserAsync(
        string email,
        string password,
        string confirmPassword,
        string firstName,
        string lastName,
        bool emailConfirmed = true,
        bool isBlocked = false,
        bool isDeleted = false,
        string createdById = "",
        CancellationToken cancellationToken = default
        );

    Task<UpdateUserResultDto> UpdateUserAsync(
        string userId,
        string firstName,
        string lastName,
        bool emailConfirmed = true,
        bool isBlocked = false,
        bool isDeleted = false,
        string updatedById = "",
        CancellationToken cancellationToken = default
        );

    Task<DeleteUserResultDto> DeleteUserAsync(
        string userId,
        string deletedById = "",
        CancellationToken cancellationToken = default
        );

    Task UpdatePasswordUserAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken
        );

    Task<List<string>> GetUserRolesAsync(
        string userId,
        CancellationToken cancellationToken = default
        );

    Task<List<string>> UpdateUserRoleAsync(
        string userId,
        string roleName,
        bool accessGranted,
        CancellationToken cancellationToken = default
        );

    Task<List<string>> UpdateAllUserRolesAsync(
        string userId,
        bool accessGranted,
        CancellationToken cancellationToken = default
        );

    Task ChangeAvatarAsync(
        string userId,
        string avatar,
        CancellationToken cancellationToken
        );
}
