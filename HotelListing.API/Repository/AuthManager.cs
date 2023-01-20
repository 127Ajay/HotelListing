using AutoMapper;
using HotelListing.API.Contracts;
using HotelListing.API.Controllers;
using HotelListing.API.Data;
using HotelListing.API.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelListing.API.Repository
{
    public class AuthManager : IAuthManager
    {
        private readonly IMapper _mapper;
        private readonly ILogger<AuthManager> _logger;
        private readonly UserManager<ApiUser> _userManager;
        private readonly IConfiguration _configuration;
        private ApiUser _user;

        private static string _loginProvider = "HotelListingAPI";
        private static string _refreshToken = "RefreshToken";

        public AuthManager(IMapper mapper, UserManager<ApiUser> userManager, IConfiguration configuration, ILogger<AuthManager> logger)
        {
            _mapper = mapper;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDTO> Login(LoginDTO loginDTO)
        {
            _user = await _userManager.FindByEmailAsync(loginDTO.Email);
            bool validPassowrd = await _userManager.CheckPasswordAsync(_user, loginDTO.Password);
            
            if(_user == null || !validPassowrd)
            {
                _logger.LogWarning($"Incorrect Email or Password");
                return null;
            }
            
            var token = await GenerateToken();
            _logger.LogInformation($"Token Generated successfully with Email {loginDTO.Email} and Tokene :{token}");
            return new AuthResponseDTO { Token = token, UserID = _user.Id, RefreshToken = await CreateRefreshToken() };
        }

        public async Task<IEnumerable<IdentityError>> Register(APIUserDTO userDTO)
        {
            _user = _mapper.Map<ApiUser>(userDTO);

            _user.UserName = userDTO.Email;

            var result = await _userManager.CreateAsync(_user, userDTO.Password);

            if(result.Succeeded)
            {
                _logger.LogInformation($"Adding Roles to User: {_user.Email}");
                await _userManager.AddToRoleAsync(_user, "User");
            }

            return result.Errors;
        }

        #region JWT
        public async Task<string> CreateRefreshToken()
        {
            await _userManager.RemoveAuthenticationTokenAsync(_user, _loginProvider, _refreshToken);

            var newRefreshToken = await _userManager.GenerateUserTokenAsync(_user, _loginProvider, _refreshToken);
            var results = await _userManager.SetAuthenticationTokenAsync(_user, _loginProvider, _refreshToken, newRefreshToken);

            return newRefreshToken;
        }

        public async Task<AuthResponseDTO> VerifyRefreshToken(AuthResponseDTO request)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(request.Token);
            var userName = tokenContent.Claims.ToList().FirstOrDefault(x=> x.Type == JwtRegisteredClaimNames.Email)?.Value;

            _user = await _userManager.FindByNameAsync(userName);

            if (_user == null || _user.Id != request.UserID ) { return null; }

            var isValidRefreshToken = await _userManager.VerifyUserTokenAsync(_user, _loginProvider, _refreshToken, request.RefreshToken);

            if (isValidRefreshToken)
            {
                var token = await GenerateToken();
                return new AuthResponseDTO
                {
                    Token = token,
                    UserID = _user.Id,
                    RefreshToken = await CreateRefreshToken()
                };
            }

            await _userManager.UpdateSecurityStampAsync(_user);
            return null;
        }

        private async Task<string> GenerateToken()
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWTSetting:Key"]));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(_user);
            var roleClaims = roles.Select(x => new Claim(ClaimTypes.Role, x)).ToList();
            var userClaims = await _userManager.GetClaimsAsync(_user);

            var claims = new List<Claim> { 
                new Claim(JwtRegisteredClaimNames.Sub, _user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, _user.Email),
                new Claim("uid", _user.Id),
            }.Union(userClaims).Union(roleClaims);

            var token = new JwtSecurityToken(
                    issuer: _configuration["JWTSetting:Issuer"],
                    audience: _configuration["JWTSetting:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["JWTSetting:DurationInMinutes"])),
                    signingCredentials: credentials
                );            
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion
    }
}
