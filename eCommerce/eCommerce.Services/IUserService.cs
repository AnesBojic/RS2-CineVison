using eCommerce.Model.Access;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IUserService : IBaseCRUDService<UserResponse, UserSearch, UserInsertRequest, UserUpdateRequest>
    {
        Task<UserSensitveResponse?> GetByUsernameAsync(string username);
        Task<UserResponse?> GetWithRoleByIdAsync(int id);
        Task ChangePasswordAsync(UserPasswordChangeRequest request);
        Task<UserResponse> GetProfileAsync(int userId);
        Task<UserResponse> UpdateProfileAsync(int userId, UserProfileUpdateRequest request);
    }
}
