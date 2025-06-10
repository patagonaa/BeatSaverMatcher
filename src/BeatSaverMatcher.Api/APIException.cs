
using System;

public class APIException : Exception
{
    public APIException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}