namespace RentalApp.Application.Exceptions;

public class AppUnauthorizedException(string message) : Exception(message);
