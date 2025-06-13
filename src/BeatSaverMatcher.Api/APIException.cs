
using System;
using System.Net;

public class APIException : Exception
{
    public APIException(HttpStatusCode? statusCode = null, string? message = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}