namespace TicketTracker.Application.Common.Exceptions;

/// <summary>A referenced record was not found (maps to HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>The request conflicts with the current state (maps to HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Server-side validation failed (maps to HTTP 400).</summary>
public class ValidationException(string message) : Exception(message);
