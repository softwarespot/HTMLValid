namespace HTMLValid
{
    /// <summary>
    ///     Exit codes used to determine the type of error occurred
    /// </summary>
    public enum ExitCode : sbyte
    {
        UserClose = -4,
        W3CConnection = -3,
        InvalidPath = -2,
        EmptyPath = -1,
        W3CValid = 0,
        W3CInvalid = 1
    }
}
