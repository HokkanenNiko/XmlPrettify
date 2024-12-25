namespace XmlPrettify.Components.Models
{
    public class MonacoValidationError
    {
        public string Message { get; set; } = string.Empty;
        public int StartLineNumber { get; set; }
        public int StartColumn { get; set; }
        public int EndLineNumber { get; set; }
        public int EndColumn { get; set; }
    }
}
