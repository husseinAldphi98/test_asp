using UserSystem.Dto.user;
using UserSystem.Models;

namespace UserSystem.Repository.IRepository
{
    public interface IUserRepo : IRepository<User>
    {
        Task<UserDto> Register(RegisterationRequestDto registerationRequestDto);
        Task<LoginResponseDto>? Login(LoginRequestDto loginRequestDto);
        Task<bool> IsPasswordCorrect(RegisterationRequestDto registerationRequestDto);
        Task<bool> IsUniqueUser(string UserName);

        Task<bool> UserExistsAsync(Guid Id);

        Task<bool> UpdateUserAsync(Guid id, UserUpdateDto userUpdateDto);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);


    }

}
