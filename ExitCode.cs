namespace HTMLValid
{
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