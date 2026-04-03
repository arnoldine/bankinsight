using System.Globalization;
using System.Linq;

namespace CoreBanker.Services
{
    public class ClientService : ApiClientBase
    {
        public ClientService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<PagedResult<ClientDto>> GetClientPageAsync(int pageNumber = 1, int pageSize = 50, string? search = null, CancellationToken cancellationToken = default)
        {
            var query = BuildPagedQuery("/api/customers/paged", pageNumber, pageSize, search);
            var result = await GetAsync<PagedResultApiModel<ClientApiModel>>(query, cancellationToken);

            return new PagedResult<ClientDto>
            {
                Items = (result?.Items ?? new List<ClientApiModel>()).ConvertAll(MapClient),
                TotalCount = result?.TotalCount ?? 0,
                PageNumber = result?.PageNumber ?? pageNumber,
                PageSize = result?.PageSize ?? pageSize
            };
        }

        public async Task<List<ClientDto>> SearchClientsAsync(string? search, int limit = 20, CancellationToken cancellationToken = default)
        {
            var page = await GetClientPageAsync(1, limit, search, cancellationToken);
            return page.Items;
        }

        public async Task<List<ClientDto>> GetClientsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<ClientApiModel>>("/api/customers", cancellationToken);
            return (result ?? new List<ClientApiModel>()).ConvertAll(MapClient);
        }

        public async Task<ClientProfileDto?> GetClientProfileAsync(string id, CancellationToken cancellationToken = default)
        {
            var profile = await GetAsync<ClientProfileApiModel>($"/api/customers/{id}/profile", cancellationToken);
            return profile is null ? null : MapClientProfile(profile);
        }

        public async Task<ClientKycStatusDto?> GetClientKycAsync(string id, CancellationToken cancellationToken = default)
        {
            var kyc = await GetAsync<ClientKycStatusApiModel>($"/api/customers/{id}/kyc", cancellationToken);
            return kyc is null ? null : MapKyc(kyc);
        }

        public async Task<ClientDto?> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
        {
            var created = await PostAsync<CreateClientRequest, ClientApiModel>("/api/customers", request, cancellationToken);
            return created is null ? null : MapClient(created);
        }

        public async Task<ClientDto?> UpdateClientAsync(string id, UpdateClientRequest request, CancellationToken cancellationToken = default)
        {
            var updated = await PutAsync<UpdateClientRequest, ClientApiModel>($"/api/customers/{id}", request, cancellationToken);
            return updated is null ? null : MapClient(updated);
        }

        public async Task<ClientNoteDto?> AddClientNoteAsync(string id, CreateClientNoteRequest request, CancellationToken cancellationToken = default)
        {
            var note = await PostAsync<CreateClientNoteRequest, ClientNoteApiModel>($"/api/customers/{id}/notes", request, cancellationToken);
            return note is null ? null : MapNote(note);
        }

        public async Task<ClientDocumentDto?> AddClientDocumentAsync(string id, CreateClientDocumentRequest request, CancellationToken cancellationToken = default)
        {
            var document = await PostAsync<CreateClientDocumentRequest, ClientDocumentApiModel>($"/api/customers/{id}/documents", request, cancellationToken);
            return document is null ? null : MapDocument(document);
        }

        public async Task<ClientMediaDto?> UploadClientMediaAsync(string id, UploadClientMediaRequest request, CancellationToken cancellationToken = default)
        {
            var media = await PostAsync<UploadClientMediaRequest, ClientMediaApiModel>($"/api/customers/{id}/media", request, cancellationToken);
            return media is null ? null : MapMedia(media);
        }

        public async Task<ClientMediaDto?> UpdateClientMediaStatusAsync(string id, string mediaId, UpdateClientMediaStatusRequest request, CancellationToken cancellationToken = default)
        {
            var media = await PutAsync<UpdateClientMediaStatusRequest, ClientMediaApiModel>($"/api/customers/{id}/media/{mediaId}/status", request, cancellationToken);
            return media is null ? null : MapMedia(media);
        }

        private static ClientDto MapClient(ClientApiModel client)
        {
            return new ClientDto
            {
                Id = client.Id ?? string.Empty,
                Name = client.Name ?? client.Id ?? string.Empty,
                Email = client.Email ?? string.Empty,
                Phone = client.Phone ?? string.Empty,
                Type = NormalizeClientType(client.Type),
                KycLevel = NormalizeKycLevel(client.KycLevel),
                RiskRating = NormalizeRiskRating(client.RiskRating),
                Status = NormalizeStatus(client.Status),
                GhanaCard = client.GhanaCard ?? string.Empty,
                DigitalAddress = client.DigitalAddress ?? string.Empty,
                CreatedAt = ParseDate(client.CreatedAt)
            };
        }

        private static ClientProfileDto MapClientProfile(ClientProfileApiModel profile)
        {
            return new ClientProfileDto
            {
                Id = profile.Id ?? string.Empty,
                Name = profile.Name ?? string.Empty,
                Type = NormalizeClientType(profile.Type),
                Email = profile.Email ?? string.Empty,
                Phone = profile.Phone ?? string.Empty,
                GhanaCard = profile.GhanaCard ?? string.Empty,
                DigitalAddress = profile.DigitalAddress ?? string.Empty,
                KycLevel = NormalizeKycLevel(profile.KycLevel),
                RiskRating = NormalizeRiskRating(profile.RiskRating),
                Employer = profile.Employer ?? string.Empty,
                MaritalStatus = profile.MaritalStatus ?? string.Empty,
                SpouseName = profile.SpouseName ?? string.Empty,
                DateOfBirth = ParseDate(profile.DateOfBirth),
                Gender = profile.Gender ?? string.Empty,
                Nationality = profile.Nationality ?? string.Empty,
                Tin = profile.Tin ?? string.Empty,
                Sector = profile.Sector ?? string.Empty,
                BusinessRegNo = profile.BusinessRegNo ?? string.Empty,
                CreatedAt = ParseDate(profile.CreatedAt),
                Notes = (profile.Notes ?? new List<ClientNoteApiModel>()).ConvertAll(MapNote),
                Documents = (profile.Documents ?? new List<ClientDocumentApiModel>()).ConvertAll(MapDocument),
                MediaAssets = (profile.MediaAssets ?? new List<ClientMediaApiModel>()).ConvertAll(MapMedia),
                ProfilePhoto = profile.ProfilePhoto is null ? null : MapMedia(profile.ProfilePhoto),
                Signature = profile.Signature is null ? null : MapMedia(profile.Signature),
                IdCardFront = profile.IdCardFront is null ? null : MapMedia(profile.IdCardFront),
                IdCardBack = profile.IdCardBack is null ? null : MapMedia(profile.IdCardBack),
                KycReadiness = profile.KycReadiness is null ? null : MapReadiness(profile.KycReadiness)
            };
        }

        private static ClientKycStatusDto MapKyc(ClientKycStatusApiModel kyc)
        {
            return new ClientKycStatusDto
            {
                CustomerId = kyc.CustomerId ?? string.Empty,
                KycLevel = NormalizeKycLevel(kyc.KycLevel),
                TransactionLimit = kyc.TransactionLimit,
                DailyLimit = kyc.DailyLimit,
                RemainingDailyLimit = kyc.RemainingDailyLimit,
                IsUnlimited = kyc.IsUnlimited,
                GhanaCardMatchesProfile = kyc.GhanaCardMatchesProfile,
                TodayPostedTotal = kyc.TodayPostedTotal
            };
        }

        private static ClientNoteDto MapNote(ClientNoteApiModel note)
        {
            return new ClientNoteDto
            {
                Id = note.Id ?? string.Empty,
                Author = note.Author ?? "System",
                Text = note.Text ?? string.Empty,
                Category = string.IsNullOrWhiteSpace(note.Category) ? "GENERAL" : note.Category.Trim().ToUpperInvariant(),
                Date = ParseDate(note.Date)
            };
        }

        private static ClientDocumentDto MapDocument(ClientDocumentApiModel document)
        {
            return new ClientDocumentDto
            {
                Id = document.Id ?? string.Empty,
                Type = document.Type ?? string.Empty,
                Name = document.Name ?? string.Empty,
                Status = string.IsNullOrWhiteSpace(document.Status) ? "PENDING" : document.Status.Trim().ToUpperInvariant(),
                UploadDate = ParseDate(document.UploadDate)
            };
        }

        private static ClientMediaDto MapMedia(ClientMediaApiModel media)
        {
            return new ClientMediaDto
            {
                Id = media.Id ?? string.Empty,
                MediaType = media.MediaType ?? string.Empty,
                MediaSide = media.MediaSide ?? string.Empty,
                FileName = media.FileName ?? string.Empty,
                ContentType = media.ContentType ?? "image/png",
                PreviewUrl = media.PreviewUrl ?? string.Empty,
                Status = string.IsNullOrWhiteSpace(media.Status) ? "PENDING" : media.Status.Trim().ToUpperInvariant(),
                FileSizeBytes = media.FileSizeBytes,
                UploadedBy = media.UploadedBy ?? string.Empty,
                UploadedAt = ParseDate(media.UploadedAt)
            };
        }

        private static ClientKycReadinessDto MapReadiness(ClientKycReadinessApiModel readiness)
        {
            return new ClientKycReadinessDto
            {
                IsReadyForAccountOpening = readiness.IsReadyForAccountOpening,
                IsReadyForLoanOrigination = readiness.IsReadyForLoanOrigination,
                MissingRequirements = readiness.MissingRequirements ?? new List<string>(),
                Checklist = (readiness.Checklist ?? new List<ClientKycChecklistItemApiModel>()).ConvertAll(item => new ClientKycChecklistItemDto
                {
                    Key = item.Key ?? string.Empty,
                    Label = item.Label ?? string.Empty,
                    IsSatisfied = item.IsSatisfied,
                    Detail = item.Detail ?? string.Empty
                })
            };
        }

        private static string NormalizeClientType(string? value)
        {
            return string.Equals(value, "CORPORATE", StringComparison.OrdinalIgnoreCase) ? "CORPORATE" : "INDIVIDUAL";
        }

        private static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? "ACTIVE").Trim().ToUpperInvariant();
            if (normalized is "INACTIVE" or "SUSPENDED")
            {
                return "Inactive";
            }

            return "Active";
        }

        private static string NormalizeKycLevel(string? value)
        {
            var normalized = (value ?? "Tier 1").Trim().ToUpperInvariant().Replace("_", string.Empty).Replace(" ", string.Empty);
            return normalized switch
            {
                "TIER3" => "Tier 3",
                "TIER2" => "Tier 2",
                _ => "Tier 1"
            };
        }

        private static string NormalizeRiskRating(string? value)
        {
            var normalized = (value ?? "Low").Trim().ToUpperInvariant();
            return normalized switch
            {
                "HIGH" => "High",
                "MEDIUM" => "Medium",
                _ => "Low"
            };
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : null;
        }

        private static string BuildPagedQuery(string basePath, int pageNumber, int pageSize, string? search)
        {
            var parameters = new List<string>
            {
                $"pageNumber={Math.Max(1, pageNumber)}",
                $"pageSize={Math.Clamp(pageSize, 1, 200)}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");
            }

            return $"{basePath}?{string.Join("&", parameters)}";
        }

        private sealed class PagedResultApiModel<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }

        private class ClientApiModel
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? DigitalAddress { get; set; }
            public string? KycLevel { get; set; }
            public string? RiskRating { get; set; }
            public string? GhanaCard { get; set; }
            public string? Status { get; set; }
            public string? CreatedAt { get; set; }
        }

        private sealed class ClientProfileApiModel : ClientApiModel
        {
            public string? Employer { get; set; }
            public string? MaritalStatus { get; set; }
            public string? SpouseName { get; set; }
            public string? DateOfBirth { get; set; }
            public string? Gender { get; set; }
            public string? Nationality { get; set; }
            public string? Tin { get; set; }
            public string? Sector { get; set; }
            public string? BusinessRegNo { get; set; }
            public List<ClientNoteApiModel>? Notes { get; set; }
            public List<ClientDocumentApiModel>? Documents { get; set; }
            public List<ClientMediaApiModel>? MediaAssets { get; set; }
            public ClientMediaApiModel? ProfilePhoto { get; set; }
            public ClientMediaApiModel? Signature { get; set; }
            public ClientMediaApiModel? IdCardFront { get; set; }
            public ClientMediaApiModel? IdCardBack { get; set; }
            public ClientKycReadinessApiModel? KycReadiness { get; set; }
        }

        private sealed class ClientKycStatusApiModel
        {
            public string? CustomerId { get; set; }
            public string? KycLevel { get; set; }
            public decimal TransactionLimit { get; set; }
            public decimal DailyLimit { get; set; }
            public decimal RemainingDailyLimit { get; set; }
            public bool IsUnlimited { get; set; }
            public bool GhanaCardMatchesProfile { get; set; }
            public decimal TodayPostedTotal { get; set; }
        }

        private sealed class ClientNoteApiModel
        {
            public string? Id { get; set; }
            public string? Author { get; set; }
            public string? Text { get; set; }
            public string? Date { get; set; }
            public string? Category { get; set; }
        }

        private sealed class ClientDocumentApiModel
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public string? Name { get; set; }
            public string? Status { get; set; }
            public string? UploadDate { get; set; }
        }

        private sealed class ClientMediaApiModel
        {
            public string? Id { get; set; }
            public string? MediaType { get; set; }
            public string? MediaSide { get; set; }
            public string? FileName { get; set; }
            public string? ContentType { get; set; }
            public string? PreviewUrl { get; set; }
            public string? Status { get; set; }
            public long? FileSizeBytes { get; set; }
            public string? UploadedBy { get; set; }
            public string? UploadedAt { get; set; }
        }

        private sealed class ClientKycReadinessApiModel
        {
            public bool IsReadyForAccountOpening { get; set; }
            public bool IsReadyForLoanOrigination { get; set; }
            public List<string>? MissingRequirements { get; set; }
            public List<ClientKycChecklistItemApiModel>? Checklist { get; set; }
        }

        private sealed class ClientKycChecklistItemApiModel
        {
            public string? Key { get; set; }
            public string? Label { get; set; }
            public bool IsSatisfied { get; set; }
            public string? Detail { get; set; }
        }
    }

    public class ClientDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Type { get; set; } = "INDIVIDUAL";
        public string KycLevel { get; set; } = "Tier 1";
        public string RiskRating { get; set; } = "Low";
        public string Status { get; set; } = "Active";
        public string GhanaCard { get; set; } = string.Empty;
        public string DigitalAddress { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class ClientProfileDto : ClientDto
    {
        public string Employer { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string SpouseName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Tin { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string BusinessRegNo { get; set; } = string.Empty;
        public List<ClientNoteDto> Notes { get; set; } = new();
        public List<ClientDocumentDto> Documents { get; set; } = new();
        public List<ClientMediaDto> MediaAssets { get; set; } = new();
        public ClientMediaDto? ProfilePhoto { get; set; }
        public ClientMediaDto? Signature { get; set; }
        public ClientMediaDto? IdCardFront { get; set; }
        public ClientMediaDto? IdCardBack { get; set; }
        public ClientKycReadinessDto? KycReadiness { get; set; }
    }

    public class ClientKycStatusDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string KycLevel { get; set; } = "Tier 1";
        public decimal TransactionLimit { get; set; }
        public decimal DailyLimit { get; set; }
        public decimal RemainingDailyLimit { get; set; }
        public bool IsUnlimited { get; set; }
        public bool GhanaCardMatchesProfile { get; set; }
        public decimal TodayPostedTotal { get; set; }
    }

    public class ClientNoteDto
    {
        public string Id { get; set; } = string.Empty;
        public string Author { get; set; } = "System";
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = "GENERAL";
        public DateTime? Date { get; set; }
    }

    public class ClientDocumentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public DateTime? UploadDate { get; set; }
    }

    public class ClientMediaDto
    {
        public string Id { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string MediaSide { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "image/png";
        public string PreviewUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public long? FileSizeBytes { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime? UploadedAt { get; set; }
    }

    public class ClientKycReadinessDto
    {
        public bool IsReadyForAccountOpening { get; set; }
        public bool IsReadyForLoanOrigination { get; set; }
        public List<string> MissingRequirements { get; set; } = new();
        public List<ClientKycChecklistItemDto> Checklist { get; set; } = new();
    }

    public class ClientKycChecklistItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsSatisfied { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public class CreateClientRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "INDIVIDUAL";
        public string? GhanaCard { get; set; }
        public string? DigitalAddress { get; set; }
        public string? KycLevel { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? RiskRating { get; set; }
    }

    public class UpdateClientRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? DigitalAddress { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? RiskRating { get; set; }
    }

    public class CreateClientNoteRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? Category { get; set; }
    }

    public class CreateClientDocumentRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UploadClientMediaRequest
    {
        public string MediaType { get; set; } = string.Empty;
        public string? MediaSide { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "image/png";
        public string DataUrl { get; set; } = string.Empty;
    }

    public class UpdateClientMediaStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
