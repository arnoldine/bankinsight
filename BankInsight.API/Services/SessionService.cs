using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BankInsight.API.Services;

public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(Staff staff, string token, string ipAddress, string? userAgent);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> InvalidateSessionAsync(string sessionId);
    Task<bool> InvalidateCurrentSessionAsync(string token);
    Task<bool> InvalidateAllUserSessionsAsync(string staffId);
    Task<List<SessionDto>> GetActiveSessionsAsync();
    Task<List<SessionDto>> GetUserSessionsAsync(string staffId);
    Task<SessionStatsDto> GetSessionStatsAsync();
    Task UpdateLastActivityAsync(string sessionId);
}

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IDeviceSecurityService _deviceSecurityService;

    public SessionService(ApplicationDbContext context, IConfiguration configuration, IDeviceSecurityService deviceSecurityService)
    {
        _context = context;
        _configuration = configuration;
        _deviceSecurityService = deviceSecurityService;
    }

    public async Task<UserSession> CreateSessionAsync(Staff staff, string token, string ipAddress, string? userAgent)
    {
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(12);

        var session = new UserSession
        {
            Id = Guid.NewGuid().ToString(),
            StaffId = staff.Id,
            Token = token,
            RefreshToken = HashToken(refreshToken),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            LastActivity = DateTime.UtcNow,
            IsActive = true
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        try
        {
            await _deviceSecurityService.ObserveConnectionAsync(staff, ipAddress, userAgent);
        }
        catch
        {
            // Device observation should not block session issuance.
        }

        return session;
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var session = await _context.UserSessions
            .Include(s => s.Staff)
            .FirstOrDefaultAsync(s => s.IsActive && (s.RefreshToken == refreshToken || s.RefreshToken == HashToken(refreshToken)));

        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        var staff = session.Staff;
        if (staff == null)
        {
            return null;
        }

        // Generate new tokens
        var newToken = GenerateJwtToken(staff);
        var newRefreshToken = GenerateRefreshToken();
        var newExpiresAt = DateTime.UtcNow.AddHours(12);

        // Update session
        session.Token = newToken;
        session.RefreshToken = HashToken(newRefreshToken);
        session.ExpiresAt = newExpiresAt;
        session.LastActivity = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new RefreshTokenResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = newExpiresAt
        };
    }

    public async Task<bool> InvalidateSessionAsync(string sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session == null)
        {
            return false;
        }

        session.IsActive = false;
        session.LogoutAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> InvalidateCurrentSessionAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

        if (session == null)
        {
            return false;
        }

        session.IsActive = false;
        session.LogoutAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> InvalidateAllUserSessionsAsync(string staffId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.StaffId == staffId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.LogoutAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync()
    {
        return await _context.UserSessions
            .Include(s => s.Staff)
            .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                StaffId = s.StaffId,
                StaffName = s.Staff != null ? s.Staff.Name : "",
                Email = s.Staff != null ? s.Staff.Email : "",
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                LastActivity = s.LastActivity,
                IsActive = s.IsActive
            })
            .OrderByDescending(s => s.LastActivity)
            .ToListAsync();
    }

    public async Task<List<SessionDto>> GetUserSessionsAsync(string staffId)
    {
        return await _context.UserSessions
            .Include(s => s.Staff)
            .Where(s => s.StaffId == staffId)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                StaffId = s.StaffId,
                StaffName = s.Staff != null ? s.Staff.Name : "",
                Email = s.Staff != null ? s.Staff.Email : "",
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                LastActivity = s.LastActivity,
                IsActive = s.IsActive
            })
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<SessionStatsDto> GetSessionStatsAsync()
    {
        var activeSessions = await _context.UserSessions
            .Include(s => s.Staff)
            .Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var sessionsToday = await _context.UserSessions
            .Where(s => s.CreatedAt >= today)
            .CountAsync();

        var sessionsByBranch = activeSessions
            .Where(s => s.Staff != null && s.Staff.BranchId != null)
            .GroupBy(s => s.Staff!.BranchId!)
            .ToDictionary(g => g.Key, g => g.Count());

        return new SessionStatsDto
        {
            TotalActiveSessions = activeSessions.Count,
            TotalSessionsToday = sessionsToday,
            SessionsByBranch = sessionsByBranch
        };
    }

    public async Task UpdateLastActivityAsync(string sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null && session.IsActive)
        {
            session.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private string GenerateJwtToken(Staff staff)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(JwtSecretResolver.ResolveBytes(_configuration));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, staff.Id),
            new Claim(ClaimTypes.Email, staff.Email),
            new Claim(ClaimTypes.Name, staff.Name),
            new Claim("BranchId", staff.BranchId ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "BankInsight",
            audience: jwtSettings["Audience"] ?? "BankInsight",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
