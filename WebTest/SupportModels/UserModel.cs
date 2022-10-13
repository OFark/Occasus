namespace WebTest.SupportModels
{
    public record UserModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
