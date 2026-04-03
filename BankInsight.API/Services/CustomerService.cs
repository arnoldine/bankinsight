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
    private static readonly string[] AllowedMediaTypes = ["PROFILE_PHOTO", "SIGNATURE", "ID_CARD"];
    private static readonly string[] AllowedMediaSides = ["FRONT", "BACK"];
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
        return await ScopedCustomers().AsNoTracking().ToListAsync();
    }

    public async Task<PagedResultDto<CustomerListItemDto>> GetCustomersPageAsync(int pageNumber, int pageSize, string? search)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);

        var query = ScopedCustomers().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Id.Contains(term) ||
                c.Name.Contains(term) ||
                (c.Email != null && c.Email.Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.GhanaCard != null && c.GhanaCard.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                Type = c.Type,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                DigitalAddress = c.DigitalAddress,
                KycLevel = c.KycLevel,
                RiskRating = c.RiskRating,
                GhanaCard = c.GhanaCard,
                Status = "ACTIVE",
                CreatedAt = c.CreatedAt.ToString("O")
            })
            .ToListAsync();

        return new PagedResultDto<CustomerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };
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

        var mediaAssets = await _context.CustomerMediaAssets
            .Where(asset => asset.CustomerId == id)
            .OrderByDescending(asset => asset.UploadedAt)
            .ToListAsync();

        return MapCustomerProfile(customer, notes, documents, mediaAssets);
    }

    public async Task<CustomerKycReadinessDto?> GetCustomerKycReadinessAsync(string customerId)
    {
        var customer = await GetCustomerByIdAsync(customerId);
        if (customer == null)
        {
            return null;
        }

        var mediaAssets = await _context.CustomerMediaAssets
            .Where(asset => asset.CustomerId == customerId)
            .OrderByDescending(asset => asset.UploadedAt)
            .ToListAsync();

        return BuildKycReadiness(customer, mediaAssets);
    }

    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var yearMonth = DateTime.UtcNow.ToString("yyMM");
        var prefix = $"CIF-{yearMonth}";
        var seq = await _sequenceService.GetNextSequenceAsync(prefix);
        var id = $"{prefix}-{seq:D5}";
        var resolvedName = ResolveCustomerName(request);

        var customer = new Customer
        {
            Id = id,
            Type = request.Type,
            Name = resolvedName,
            GhanaCard = string.IsNullOrWhiteSpace(request.GhanaCard) ? request.IdNumber : request.GhanaCard,
            DigitalAddress = string.IsNullOrWhiteSpace(request.DigitalAddress) ? request.Address : request.DigitalAddress,
            KycLevel = request.KycLevel ?? "Tier 1",
            Phone = request.Phone,
            Email = request.Email,
            RiskRating = request.RiskRating ?? "Low",
            BranchId = !string.IsNullOrEmpty(request.BranchId) ? request.BranchId : (!string.IsNullOrEmpty(_currentUser.BranchId) ? _currentUser.BranchId : "BR001"),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
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

    public async Task<CustomerMediaDto?> UploadCustomerMediaAsync(string id, UploadCustomerMediaRequest request)
    {
        var customer = await GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return null;
        }

        var mediaType = (request.MediaType ?? string.Empty).Trim().ToUpperInvariant();
        if (!AllowedMediaTypes.Contains(mediaType))
        {
            throw new InvalidOperationException("Unsupported media type.");
        }

        var mediaSide = string.IsNullOrWhiteSpace(request.MediaSide)
            ? null
            : request.MediaSide.Trim().ToUpperInvariant();

        if (mediaType == "ID_CARD")
        {
            if (string.IsNullOrWhiteSpace(mediaSide) || !AllowedMediaSides.Contains(mediaSide))
            {
                throw new InvalidOperationException("ID card uploads must specify FRONT or BACK.");
            }
        }
        else
        {
            mediaSide = null;
        }

        var dataUrl = request.DataUrl?.Trim() ?? string.Empty;
        if (!dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only image uploads are supported.");
        }

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "image/png" : request.ContentType.Trim();
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Content type must be an image type.");
        }

        var asset = new CustomerMediaAsset
        {
            Id = $"MED-{Guid.NewGuid():N}",
            CustomerId = id,
            MediaType = mediaType,
            MediaSide = mediaSide,
            FileName = request.FileName.Trim(),
            ContentType = contentType,
            DataUrl = dataUrl,
            Status = "PENDING",
            FileSizeBytes = EstimateBase64Bytes(dataUrl),
            UploadedBy = string.IsNullOrWhiteSpace(_currentUser.Email) ? (_currentUser.UserId ?? "System") : _currentUser.Email,
            UploadedAt = DateTime.UtcNow
        };

        _context.CustomerMediaAssets.Add(asset);
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "UPLOAD_MEDIA",
            EntityType = "CUSTOMER",
            EntityId = id,
            Description = $"{asset.MediaType}{(asset.MediaSide is null ? string.Empty : $" {asset.MediaSide}")}: {asset.FileName}",
            PayloadJson = JsonSerializer.Serialize(MapMedia(asset)),
            Status = "SUCCESS",
            IsSuccess = true,
            UserId = string.IsNullOrWhiteSpace(_currentUser.UserId) ? null : _currentUser.UserId,
            CreatedBy = string.IsNullOrWhiteSpace(_currentUser.UserId) ? null : _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return MapMedia(asset);
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

    private static CustomerProfileResponse MapCustomerProfile(Customer customer, List<CustomerNoteDto> notes, List<CustomerDocumentDto> documents, List<CustomerMediaAsset> mediaAssets)
    {
        var mappedMedia = mediaAssets.Select(MapMedia).ToList();
        var readiness = BuildKycReadiness(customer, mediaAssets);
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
            MediaAssets = mappedMedia,
            ProfilePhoto = SelectLatest(mappedMedia, "PROFILE_PHOTO"),
            Signature = SelectLatest(mappedMedia, "SIGNATURE"),
            IdCardFront = SelectLatest(mappedMedia, "ID_CARD", "FRONT"),
            IdCardBack = SelectLatest(mappedMedia, "ID_CARD", "BACK"),
            KycReadiness = readiness
        };
    }

    private static string ResolveCustomerName(CreateCustomerRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            return request.Name.Trim();
        }

        var combined = string.Join(" ", new[] { request.FirstName, request.OtherName, request.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));

        return combined;
    }

    private static CustomerMediaDto MapMedia(CustomerMediaAsset asset)
    {
        return new CustomerMediaDto
        {
            Id = asset.Id,
            MediaType = asset.MediaType,
            MediaSide = asset.MediaSide,
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            PreviewUrl = asset.DataUrl,
            Status = string.IsNullOrWhiteSpace(asset.Status) ? "PENDING" : asset.Status,
            FileSizeBytes = asset.FileSizeBytes,
            UploadedBy = asset.UploadedBy,
            UploadedAt = asset.UploadedAt.ToString("O")
        };
    }

    private static CustomerMediaDto? SelectLatest(List<CustomerMediaDto> media, string mediaType, string? mediaSide = null)
    {
        return media.FirstOrDefault(item =>
            string.Equals(item.MediaType, mediaType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.MediaSide ?? string.Empty, mediaSide ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    private static long? EstimateBase64Bytes(string dataUrl)
    {
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex < 0 || commaIndex == dataUrl.Length - 1)
        {
            return null;
        }

        var base64 = dataUrl[(commaIndex + 1)..];
        var padding = base64.EndsWith("==", StringComparison.Ordinal) ? 2 : base64.EndsWith("=", StringComparison.Ordinal) ? 1 : 0;
        return (base64.Length * 3L / 4L) - padding;
    }

    private static CustomerKycReadinessDto BuildKycReadiness(Customer customer, List<CustomerMediaAsset> mediaAssets)
    {
        var latestBySlot = mediaAssets
            .GroupBy(asset => $"{asset.MediaType}:{asset.MediaSide ?? string.Empty}")
            .ToDictionary(group => group.Key, group => group.OrderByDescending(asset => asset.UploadedAt).First());

        var checklist = new List<CustomerKycChecklistItemDto>();

        void AddItem(string key, string label, bool isSatisfied, string successDetail, string missingDetail)
        {
            checklist.Add(new CustomerKycChecklistItemDto
            {
                Key = key,
                Label = label,
                IsSatisfied = isSatisfied,
                Detail = isSatisfied ? successDetail : missingDetail
            });
        }

        var isCorporate = string.Equals(customer.Type, "CORPORATE", StringComparison.OrdinalIgnoreCase);
        var identityPresent = isCorporate
            ? !string.IsNullOrWhiteSpace(customer.BusinessRegNo) || !string.IsNullOrWhiteSpace(customer.Tin)
            : !string.IsNullOrWhiteSpace(customer.GhanaCard);

        AddItem(
            "identity",
            isCorporate ? "Registration or TIN captured" : "Ghana Card captured",
            identityPresent,
            "Identity details captured.",
            isCorporate ? "Business registration number or TIN is required." : "Ghana Card is required.");

        AddMediaRequirement("profile-photo", "Verified profile photo", "PROFILE_PHOTO", null);
        AddMediaRequirement("signature", "Verified signature", "SIGNATURE", null);

        if (!isCorporate)
        {
            AddMediaRequirement("id-front", "Verified ID card front", "ID_CARD", "FRONT");
            AddMediaRequirement("id-back", "Verified ID card back", "ID_CARD", "BACK");
        }

        var missingRequirements = checklist.Where(item => !item.IsSatisfied).Select(item => item.Label).ToList();
        var isReady = missingRequirements.Count == 0;

        return new CustomerKycReadinessDto
        {
            IsReadyForAccountOpening = isReady,
            IsReadyForLoanOrigination = isReady,
            MissingRequirements = missingRequirements,
            Checklist = checklist
        };

        void AddMediaRequirement(string key, string label, string mediaType, string? mediaSide)
        {
            var slotKey = $"{mediaType}:{mediaSide ?? string.Empty}";
            var asset = latestBySlot.GetValueOrDefault(slotKey);
            var isVerified = asset is not null && string.Equals(asset.Status, "VERIFIED", StringComparison.OrdinalIgnoreCase);

            AddItem(
                key,
                label,
                isVerified,
                $"{label} is verified.",
                asset is null ? $"{label} has not been uploaded." : $"{label} is not yet verified.");
        }
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
