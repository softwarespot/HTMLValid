namespace HTMLValid
{
    /// <summary>
    ///     HTML validation results structure
    /// </summary>
    struct HtmlValidResults
    {
        /// <summary>
        ///     Number of errors found in the content
        /// </summary>
        public readonly int Errors;

        /// <summary>
        ///     Number of warnings found in the content
        /// </summary>
        public readonly int Warnings;

        /// <summary>
        ///     File type of validated content
        /// </summary>
        public readonly HtmlValidFileType FileType;

        /// <summary>
        ///     Did a valid connection take place between the end user and validation service?
        /// </summary>
        public readonly bool IsConnected;

        /// <summary>
        ///     Validation status
        /// </summary>
        public readonly HtmlValidStatus Status;

        /// <summary>
        ///     Constructor for creating a HtmlValidResults structure
        /// </summary>
        /// <param name="isConnected"></param>
        /// <param name="status"></param>
        /// <param name="fileType"></param>
        /// <param name="errors"></param>
        /// <param name="warnings"></param>
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
