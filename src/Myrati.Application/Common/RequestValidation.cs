using System.Globalization;
using FluentValidation;

namespace Myrati.Application.Common;

public static class RequestValidation
{
    public static Task ValidateRequestAsync<T>(
        this IValidator<T> validator,
        T request,
        CancellationToken cancellationToken = default) =>
        validator.ValidateAndThrowAsync(request, cancellationToken);

    public static DateOnly ParseIsoDate(string value, string fieldName)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure(
                fieldName,
                $"{fieldName} must be a valid date in yyyy-MM-dd format.")
        };

        throw new ValidationException(failures);
    }

    public static string ToIsoDate(this DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
