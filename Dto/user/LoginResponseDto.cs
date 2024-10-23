namespace UserSystem.Dto.user
{
    public class LoginResponseDto
    {
        public string User { get; set; }
        public string Roles { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; internal set; }
    }
}
