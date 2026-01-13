namespace Erasmus_SSC.Models
{
    public class UserRole
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = default!;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
