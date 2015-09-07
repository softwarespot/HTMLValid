namespace HTMLValid
{
    struct HtmlValidResults
    {
        public readonly int Errors;

        public readonly int Warnings;

        public readonly HtmlValidFileType FileType;

        public readonly bool IsConnected;

        public readonly HtmlValidStatus Status;

        public HtmlValidResults(bool isConnected, HtmlValidStatus status, HtmlValidFileType fileType, int errors, int warnings) // Constructor
        {
            IsConnected = isConnected;
            FileType = fileType;
            Status = status;
            Errors = errors;
            Warnings = warnings;
        }
    }
}