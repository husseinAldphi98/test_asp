using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserSystem.Data;
using UserSystem.Dto.user;
using UserSystem.Models;
using UserSystem.Repository.IRepository;

namespace UserSystem.Repository
{
    public class UserRepo : Repository<User>, IUserRepo
    {
        private readonly ApplicationDbContext _context;
        private readonly string _secretKey;
        private readonly int _expireTokenHours;
        private readonly int _refreshTokenExpire;

        public UserRepo(ApplicationDbContext context, IConfiguration configuration) : base(context)
        {
            _context = context;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _expireTokenHours = configuration.GetValue<int>("Jwt:ExpireHours");
            _refreshTokenExpire = configuration.GetValue<int>("Jwt:RefreshExpireDays");
        }

        public async Task<bool> IsPasswordCorrect(RegisterationRequestDto registerationRequestDto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == registerationRequestDto.UserName);
                return user != null && BCrypt.Net.BCrypt.Verify(registerationRequestDto.Password, user.Password);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                return false;
            }
        }

        public async Task<bool> IsUniqueUser(string userName)
        {
            try
            {
                return await _context.Users.AllAsync(u => u.UserName != userName);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                return false;
            }
        }

        public async Task<UserDto> Register(RegisterationRequestDto registerationRequestDto)
        {
            try
            {


                var user = new User
                {
                    UserName = registerationRequestDto.UserName,
                    Password = BCrypt.Net.BCrypt.HashPassword(registerationRequestDto.Password), // Hash the password
                    FullName = registerationRequestDto.FullName,
                    Role = registerationRequestDto.Role // Set the role
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return new UserDto { Id = user.Id, UserName = user.UserName, Role = user.Role, Password = user.Password }; // Return UserDto with role
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                throw; // Re-throwing the exception to be handled by the caller
            }
        }

        public async Task<bool> UserExistsAsync(Guid Id)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Id == Id);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(Guid id, UserUpdateDto userUpdateDto)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    return false;
                }

                existingUser.UserName = userUpdateDto.UserName;
                if (!string.IsNullOrEmpty(userUpdateDto.Password))
                {
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(userUpdateDto.Password);
                }
                existingUser.FullName = userUpdateDto.FullName;
                existingUser.Role = userUpdateDto.Role;

                _context.Entry(existingUser).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                return false;
            }
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
        {
            try
            {
                // Find the user by username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == loginRequestDto.UserName.ToLower());

                // If user is not found or password does not match, return null
                if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.Password))
                {
                    return null;
                }

                // Create the JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        // Add other claims as needed
                    }),
                    Expires = DateTime.UtcNow.AddHours(_expireTokenHours), // Token expiration time
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var refreshTokenDescriptor = new SecurityTokenDescriptor
                {/*
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        // Add other claims as needed
                    }),*/
                    Expires = DateTime.UtcNow.AddDays(_refreshTokenExpire), // Token expiration time
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                var Rtoken = tokenHandler.CreateToken(refreshTokenDescriptor);
                var RtokenString = tokenHandler.WriteToken(Rtoken);

                return new LoginResponseDto
                {
                    User = user.UserName, // Map User to UserDto
                    Roles = user.Role,
                    AccessToken = tokenString,
                    RefreshToken = RtokenString,
                };
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                return null;
            }
        }
        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                // Validate the token
                var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // Eliminate the default clock skew
                }, out SecurityToken validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken &&
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Check if the refresh token has expired
                    if (jwtToken.ValidTo < DateTime.UtcNow)
                    {
                        return null; // Token has expired
                    }

                    // Retrieve user from claims
                    var userName = principal.Identity.Name;
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == userName.ToLower());

                    if (user == null)
                    {
                        return null;
                    }

                    // Generate new access and refresh tokens
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                    new Claim(ClaimTypes.Name, user.UserName),
                            // Add other claims as needed
                        }),
                        Expires = DateTime.UtcNow.AddHours(_expireTokenHours),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var newAccessToken = tokenHandler.CreateToken(tokenDescriptor);
                    var accessTokenString = tokenHandler.WriteToken(newAccessToken);

                    var refreshTokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                    new Claim(ClaimTypes.Name, user.UserName),
                            // Add other claims as needed
                        }),
                        Expires = DateTime.UtcNow.AddDays(_refreshTokenExpire),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var newRefreshToken = tokenHandler.CreateToken(refreshTokenDescriptor);
                    var refreshTokenString = tokenHandler.WriteToken(newRefreshToken);

                    return new LoginResponseDto
                    {
                        User = user.UserName,
                        Roles = user.Role,
                        AccessToken = accessTokenString,
                        RefreshToken = refreshTokenString,
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                // Logger.LogError(ex, "An error occurred during token refresh.");
                return null;
            }
        }

    }


}
