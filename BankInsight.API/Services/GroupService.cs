using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class GroupService
{
    private readonly ApplicationDbContext _context;

    public GroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GroupDto>> GetGroupsAsync()
    {
        var groups = await _context.Groups.Include(g => g.Members).ToListAsync();
        return groups.Select(g => new GroupDto
        {
            Id = g.Id,
            Name = g.Name,
            Officer = g.AssignedOfficerId ?? g.OfficerId,
            MeetingDay = g.MeetingDayOfWeek ?? g.MeetingDay,
            FormationDate = g.FormationDate,
            Status = g.Status,
            Members = g.Members.Where(m => m.Status == "ACTIVE").Select(m => m.CustomerId).ToList()
        }).ToList();
    }

    public async Task<GroupDto> CreateGroupAsync(CreateGroupRequest request)
    {
        var groupId = $"GRP{(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000).ToString().PadLeft(4, '0')}";
        var group = new Group
        {
            Id = groupId,
            Name = request.Name,
            GroupCode = groupId,
            OfficerId = request.Officer,
            AssignedOfficerId = request.Officer,
            MeetingDay = request.MeetingDay,
            MeetingDayOfWeek = request.MeetingDay,
            FormationDate = request.FormationDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Status = string.IsNullOrEmpty(request.Status) ? "ACTIVE" : request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Officer = group.AssignedOfficerId,
            MeetingDay = group.MeetingDayOfWeek,
            FormationDate = group.FormationDate,
            Status = group.Status,
            Members = new List<string>()
        };
    }

    public async Task<bool> AddMemberAsync(string groupId, string customerId)
    {
        var exists = await _context.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.CustomerId == customerId && gm.Status == "ACTIVE");
        if (exists)
        {
            return false;
        }

        _context.GroupMembers.Add(new GroupMember
        {
            Id = $"GLM-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Random.Shared.Next(100, 999)}",
            GroupId = groupId,
            CustomerId = customerId,
            JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "ACTIVE",
            MemberRole = "MEMBER"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RemoveMemberAsync(string groupId, string customerId)
    {
        var member = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.CustomerId == customerId && gm.Status == "ACTIVE");
        if (member != null)
        {
            member.Status = "EXITED";
            member.ExitDate = DateOnly.FromDateTime(DateTime.UtcNow);
            await _context.SaveChangesAsync();
        }
    }
}
