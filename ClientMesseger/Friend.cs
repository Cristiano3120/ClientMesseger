namespace ClientMesseger
{
    public sealed class Friend
    {
        public required string Username { get; set; }
        public required RelationshipStateEnum Status { get; set; }
        public string ProfilPic { get; set; }
    }
}
