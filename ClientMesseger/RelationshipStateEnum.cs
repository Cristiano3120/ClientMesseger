namespace ClientMesseger
{
    public enum RelationshipStateEnum
    {
        Accepted = 0,
        Blocked = 1,
        Decline = 2,
        Unblocked = 3,
        Delete = 4,
        Pending = 5,
    }

    public static class RelationshipStateEnumExtensions
    {
        public static string ToVerb(this RelationshipStateEnum stateEnum)
        {
            return stateEnum switch 
            {
                RelationshipStateEnum.Accepted => "Accepting",
                RelationshipStateEnum.Blocked => "Blocking",
                RelationshipStateEnum.Decline => "Declining",
                RelationshipStateEnum.Unblocked => "Unblocking",
                RelationshipStateEnum.Delete => "Deleting",
                _ => throw new NotSupportedException($"The used Enum isn´t supported by the {"ToVerb"} method")
            };
        }
    }
}
