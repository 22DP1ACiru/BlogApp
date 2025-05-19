namespace BlogApp.Web.DTOs
{
    public class TokenResponseDto
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public string Username { get; set; }
        public IList<string> Roles { get; set; }
    }
}