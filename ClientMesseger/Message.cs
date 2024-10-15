namespace ClientMesseger
{
    public record Message
    {
        public required string Content { get; set; }
        public required DateTime Time { get; set; }

        public void Deconstruct(out DateTime time, out string content)
        {
            time = Time;
            content = Content;
        }

    }
}
