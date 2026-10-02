namespace Featurama.Maui.Exceptions;

public class FeaturamaForbiddenException : FeaturamaApiException
{
    public FeaturamaForbiddenException(string? responseBody = null)
        : base(403, "This operation is not permitted.", responseBody) { }
}
