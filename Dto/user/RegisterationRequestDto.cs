namespace UserSystem.Dto.user
{
    public class RegisterationRequestDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string? Image { get; set; }
    }
}
