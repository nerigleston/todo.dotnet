namespace ToDoList.Utils
{
    public static class Helpers
    {
        public static string RemoveHtmlTags(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
