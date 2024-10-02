using System.Collections;

namespace ClientMesseger
{
    /// <summary>
    /// This class presents basic infos about a User.
    /// It implements IEnumerable (string)
    /// </summary>
    public sealed class User : IEnumerable<string>
    {
        public required string Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        public IEnumerator<string> GetEnumerator()
        {
            yield return Email;
            yield return Username;
            yield return Password;
            yield return FirstName;
            yield return LastName;
            yield return Day.ToString();
            yield return Month.ToString();
            yield return Year.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
