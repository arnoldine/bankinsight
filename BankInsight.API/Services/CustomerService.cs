using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class CustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly ISequenceGeneratorService _sequenceService;
    private readonly Security.ICurrentUserContext _currentUser;

    public CustomerService(ApplicationDbContext context, ISequenceGeneratorService sequenceService, Security.ICurrentUserContext currentUser)
    {
        _context = context;
        _sequenceService = sequenceService;
        _currentUser = currentUser;
    }

    public async Task<List<Customer>> GetCustomersAsync()
    {
        return await ScopedCustomers().ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(string id)
    {
        return await ScopedCustomers().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CustomerProfileResponse?> GetCustomerProfileAsync(string id)
    {
        var customer = await GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return null;
        }

        var activity = await _context.AuditLogs
            .Where(log => log.EntityType == "CUSTOMER" && log.EntityId == id)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();

        var notes = activity
            .Where(log => string.Equals(log.Action, "ADD_NOTE", StringComparison.OrdinalIgnoreCase))
            .Select(MapNote)
            .Where(note => note != null)
            .Cast<CustomerNoteDto>()
            .ToList();

        var documents = activity
            .Where(log => string.Equals(log.Action, "ADD_DOCUMENT", StringComparison.OrdinalIgnoreCase))
            .Select(MapDocument)
            .Where(document => document != null)
            .Cast<CustomerDocumentDto>()
            .ToList();

        return MapCustomerProfile(customer, notes, documents);
    }

    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var yearMonth = DateTime.UtcNow.ToString("yyMM");
        var prefix = $"CIF-{yearMonth}";
        var seq = await _sequenceService.GetNextSequenceAsync(prefix);
        var id = $"{prefix}-{seq:D5}";

        var customer = new Customer
        {
            Id = id,
            Type = request.Type,
            Name = request.Name,
            GhanaCard = request.GhanaCard,
            DigitalAddress = request.DigitalAddress,
            KycLevel = request.KycLevel ?? "Tier 1",
            Phone = request.Phone,
            Email = request.Email,
            RiskRating = request.RiskRating ?? "Low",
            BranchId = !string.IsNullOrEmpty(_currentUser.BranchId) ? _currentUser.BranchId : "BR001",
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return customer;
    }

    public async Task<Customer?> UpdateCustomerAsync(string id, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return null;

        customer.Name = request.Name;
        customer.DigitalAddress = request.DigitalAddress;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.RiskRating = request.RiskRating ?? customer.RiskRating;

        await _context.SaveChangesAsync();

        return customer;
    }

    public async Task<CustomerNoteDto?> AddCustomerNoteAsync(string id, CreateCustomerNoteRequest request)
    {
        var customer = await GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return null;
        }

        var note = new CustomerNoteDto
        {
            Id = $"NOTE-{Guid.NewGuid():N}",
            Author = string.IsNullOrWhiteSpace(_currentUser.Email) ? (_currentUser.UserId ?? "System") : _currentUser.Email,
            Text = request.Text.Trim(),
            Date = DateTime.UtcNow.ToString("O"),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "GENERAL" : request.Category.Trim().ToUpperInvariant()
        };

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "ADD_NOTE",
            EntityType = "CUSTOMER",
            EntityId = id,
            Description = note.Text,
            PayloadJson = JsonSerializer.Serialize(note),
            Status = "SUCCESS",
            IsSuccess = true,
            UserId = string.IsNullOrWhiteSpace(_currentUser.UserId) ? null : _currentUser.UserId,
            CreatedBy = string.IsNullOrWhiteSpace(_currentUser.UserId) ? null : _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return note;
    }

    public async Task<CustomerDocumentDto?> AddCustomerDocumentAsync(string id, CreateCustomerDocumentRequest request)
    {
        var customer = await GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return null;
        }

        var document = new CustomerDocumentDto
        {
            Id = $"DOC-{Guid.NewGuid():N}",
            Type = request.Type.Trim(),
            Name = request.Name.Trim(),
            Status = "PENDING",
            UploadDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "ADD_DOCUMENT",
            EntityType = "CUSTOMER",
            EntityId = id,
            Description = document.Name,
            PayloadJson = JsonSerializer.Serialize(document),
            Status = "SUCCESS",
            IsSuccess = true,
            UserId = string.IsNullOrWhiteSpace(_currentUser.UserId) ? null : _currentUser.UserId,
            CreatedBy = string.IsNullOrWhiteSpace(_currentUser.UserId) ? null : _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return document;
    }

    private IQueryable<Customer> ScopedCustomers()
    {
        var query = _context.Customers.AsQueryable();
        if (_currentUser.ScopeType == AccessScopeType.BranchOnly && !string.IsNullOrEmpty(_currentUser.BranchId))
        {
            query = query.Where(c => c.BranchId == _currentUser.BranchId);
        }

        return query;
    }

    private static CustomerProfileResponse MapCustomerProfile(Customer customer, List<CustomerNoteDto> notes, List<CustomerDocumentDto> documents)
    {
        return new CustomerProfileResponse
        {
            Id = customer.Id,
            Type = customer.Type,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            DigitalAddress = customer.DigitalAddress,
            KycLevel = customer.KycLevel,
            RiskRating = customer.RiskRating,
            GhanaCard = customer.GhanaCard,
            Employer = customer.Employer,
            MaritalStatus = customer.MaritalStatus,
            SpouseName = customer.SpouseName,
            DateOfBirth = customer.DateOfBirth?.ToString("yyyy-MM-dd"),
            Gender = customer.Gender,
            Nationality = customer.Nationality,
            Tin = customer.Tin,
            Sector = customer.Sector,
            BusinessRegNo = customer.BusinessRegNo,
            CreatedAt = customer.CreatedAt.ToString("O"),
            Notes = notes,
            Documents = documents,
        };
    }

    private static CustomerNoteDto? MapNote(AuditLog log)
    {
        if (!string.IsNullOrWhiteSpace(log.PayloadJson))
        {
            try
            {
                var note = JsonSerializer.Deserialize<CustomerNoteDto>(log.PayloadJson);
                if (note != null)
                {
                    return note;
                }
            }
            catch (JsonException)
            {
            }
        }

        if (string.IsNullOrWhiteSpace(log.Description))
        {
            return null;
        }

        return new CustomerNoteDto
        {
            Id = $"AUD-{log.Id}",
            Author = log.CreatedBy ?? log.UserId ?? "System",
            Text = log.Description,
            Date = log.CreatedAt.ToString("O"),
            Category = "GENERAL"
        };
    }

    private static CustomerDocumentDto? MapDocument(AuditLog log)
    {
        if (!string.IsNullOrWhiteSpace(log.PayloadJson))
        {
            try
            {
                var document = JsonSerializer.Deserialize<CustomerDocumentDto>(log.PayloadJson);
                if (document != null)
                {
                    return document;
                }
            }
            catch (JsonException)
            {
            }
        }

        if (string.IsNullOrWhiteSpace(log.Description))
        {
            return null;
        }

        return new CustomerDocumentDto
        {
            Id = $"AUD-{log.Id}",
            Type = "Document",
            Name = log.Description,
            Status = "PENDING",
            UploadDate = log.CreatedAt.ToString("yyyy-MM-dd")
        };
    }
}
