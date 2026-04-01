using System;

namespace Kavita.API.Controllers;

/// <summary>
/// Provides HTTP status code constants for API responses
/// </summary>
public static class StatusCodes
{
    /// <summary>
    /// HTTP 200 OK
    /// </summary>
    public const int Status200OK = 200;

    /// <summary>
    /// HTTP 201 Created
    /// </summary>
    public const int Status201Created = 201;

    /// <summary>
    /// HTTP 204 No Content
    /// </summary>
    public const int Status204NoContent = 204;

    /// <summary>
    /// HTTP 400 Bad Request
    /// </summary>
    public const int Status400BadRequest = 400;

    /// <summary>
    /// HTTP 401 Unauthorized
    /// </summary>
    public const int Status401Unauthorized = 401;

    /// <summary>
    /// HTTP 403 Forbidden
    /// </summary>
    public const int Status403Forbidden = 403;

    /// <summary>
    /// HTTP 404 Not Found
    /// </summary>
    public const int Status404NotFound = 404;

    /// <summary>
    /// HTTP 500 Internal Server Error
    /// </summary>
    public const int Status500InternalServerError = 500;

    /// <summary>
    /// HTTP 503 Service Unavailable
    /// </summary>
    public const int Status503ServiceUnavailable = 503;

    /// <summary>
    /// HTTP 504 Gateway Timeout
    /// </summary>
    public const int Status504GatewayTimeout = 504;
}

/// <summary>
/// Marker class for Status200OK
/// </summary>
public class Status200OK;

/// <summary>
/// Marker class for Status400BadRequest
/// </summary>
public class Status400BadRequest;

/// <summary>
/// Marker class for Status404NotFound
/// </summary>
public class Status404NotFound;

/// <summary>
/// Marker class for StatusInternalServerError
/// </summary>
public class StatusInternalServerError;
