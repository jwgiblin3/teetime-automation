using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.DTOs;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Services;

/// <summary>
/// Implementation of authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the AuthService.
    /// </summary>
    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger, IAuditService auditService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Registers a new user with email and password.
    /// </summary>
    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Email and password are required");
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new ArgumentException("Passwords do not match");
            }

            if (request.Password.Length < 8)
            {
                throw new ArgumentException("Password must be at least 8 characters");
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already registered");
            }

            var user = new User
            {
                Email = request.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _auditService.LogEventAsync(user.UserId, null, AuditEventType.UserRegistered,
                $"User {user.Email} registered successfully");

            _logger.LogInformation("User {Email} registered successfully", user.Email);

            var userProfile = new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            return new LoginResponse
            {
                AccessToken = GenerateJwtToken(user.UserId),
                TokenType = "Bearer",
                ExpiresIn = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440") * 60,
                User = userProfile
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            throw;
        }
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Email and password are required");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email.ToLower().Trim());
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("Login attempt with invalid email: {Email}", request.Email);
                throw new InvalidOperationException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                throw new InvalidOperationException("User account is not active");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for user: {Email}", user.Email);
                throw new InvalidOperationException("Invalid email or password");
            }

            await _auditService.LogEventAsync(user.UserId, null, AuditEventType.UserLogin,
                $"User {user.Email} logged in successfully");

            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            var userProfile = new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            return new LoginResponse
            {
                AccessToken = GenerateJwtToken(user.UserId),
                TokenType = "Bearer",
                ExpiresIn = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440") * 60,
                User = userProfile
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }

    /// <summary>
    /// Authenticates a user using Google OAuth ID token.
    /// </summary>
    public async Task<LoginResponse> GoogleAuthAsync(GoogleAuthRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                throw new ArgumentException("ID token is required");
            }

            // In a real implementation, you would validate the ID token with Google
            // This is a simplified version that assumes the token is valid
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token;

            try
            {
                token = handler.ReadJwtToken(request.IdToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid ID token format");
                throw new InvalidOperationException("Invalid ID token");
            }

            var googleId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var givenName = token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? string.Empty;
            var familyName = token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                throw new InvalidOperationException("Invalid token claims");
            }

            var user = _context.Users.FirstOrDefault(u => u.GoogleOAuthId == googleId);

            if (user == null)
            {
                user = _context.Users.FirstOrDefault(u => u.Email == email.ToLower().Trim());

                if (user == null)
                {
                    user = new User
                    {
                        Email = email.ToLower().Trim(),
                        FirstName = givenName,
                        LastName = familyName,
                        GoogleOAuthId = googleId,
                        PhoneNumber = request.PhoneNumber,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    await _auditService.LogEventAsync(user.UserId, null, AuditEventType.UserRegistered,
                        $"User {user.Email} registered via Google OAuth");

                    _logger.LogInformation("New user {Email} created via Google OAuth", user.Email);
                }
                else
                {
                    user.GoogleOAuthId = googleId;
                    user.UpdatedAt = DateTime.UtcNow;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }

            if (!user.IsActive)
            {
                throw new InvalidOperationException("User account is not active");
            }

            await _auditService.LogEventAsync(user.UserId, null, AuditEventType.UserLogin,
                $"User {user.Email} logged in via Google OAuth");

            _logger.LogInformation("User {Email} authenticated via Google OAuth", user.Email);

            var userProfile = new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            return new LoginResponse
            {
                AccessToken = GenerateJwtToken(user.UserId),
                TokenType = "Bearer",
                ExpiresIn = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440") * 60,
                User = userProfile
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google authentication");
            throw;
        }
    }

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    public string GenerateJwtToken(int userId)
    {
        try
        {
            var secret = _configuration["Jwt:Secret"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("sub", userId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token");
            throw;
        }
    }

    /// <summary>
    /// Gets the current authenticated user ID from claims.
    /// </summary>
    public int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
