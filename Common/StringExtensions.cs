namespace Mutzl.Homeassistant
{
    public static class StringExtensions
    {
        public static string ToSimple(this string name) => 
            name
             .ToLower()
             .Replace(" ", "")
             .Replace(".", "")
             .Replace("-", "")
             .Replace("+", "plus")
             .Replace("ä", "ae")
             .Replace("ö", "oe")
             .Replace("ü", "ue")
             .Replace("ß", "ss")
             ;

        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);
    }
}
