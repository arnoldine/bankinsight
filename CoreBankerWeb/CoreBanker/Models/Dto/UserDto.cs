namespace CoreBanker.Models.Dto
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string[] Permissions { get; set; } = Array.Empty<string>();
        // TODO: Add more user properties as needed
    }
}
