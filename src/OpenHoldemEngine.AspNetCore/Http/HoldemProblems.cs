using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OpenHoldemEngine.AspNetCore.Http;

/// <summary>
/// Maps <see cref="HoldemErrorCode"/> values to HTTP status codes and ProblemDetails responses.
/// </summary>
internal static class HoldemProblems
{
    /// <summary>
    /// Returns the HTTP status code corresponding to the given error code.
    /// </summary>
    /// <param name="code">The error code to map.</param>
    /// <param name="ifMatchPresent">Whether the request carried an If-Match header. Used to distinguish between 412 and 409 for Conflict errors.</param>
    /// <returns>The HTTP status code (404, 409, 412, or 422).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the code is not a defined member of <see cref="HoldemErrorCode"/>.</exception>
    internal static int StatusCodeFor(HoldemErrorCode code, bool ifMatchPresent) => code switch
    {
        HoldemErrorCode.GameAlreadyExists => 409,
        HoldemErrorCode.CurrentHandEnded => 409,
        HoldemErrorCode.PreviousHandNotEnded => 409,
        HoldemErrorCode.InvalidAction => 422,
        HoldemErrorCode.UnsupportedGameOptions => 422,
        HoldemErrorCode.InvalidGameOptions => 422,
        HoldemErrorCode.InvalidSeat => 422,
        HoldemErrorCode.GameAlreadyStarted => 409,
        HoldemErrorCode.GameNotStarted => 409,
        HoldemErrorCode.GameFinished => 409,
        HoldemErrorCode.Conflict => ifMatchPresent ? 412 : 409,
        HoldemErrorCode.GameNotFound => 404,
        HoldemErrorCode.IllegalAction => 422,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    /// <summary>
    /// Converts a list of <see cref="HoldemError"/> to an HTTP problem response.
    /// </summary>
    /// <param name="errors">The list of errors from the engine result. Must contain at least one error.</param>
    /// <param name="ifMatchPresent">Whether the request carried an If-Match header. Used to distinguish between 412 and 409 for Conflict errors.</param>
    /// <returns>A ProblemHttpResult with the appropriate status code and error details.</returns>
    internal static ProblemHttpResult ToProblem(IReadOnlyList<HoldemError> errors, bool ifMatchPresent)
    {
        var error = errors[0];
        return TypedResults.Problem(
            detail: error.Message,
            statusCode: StatusCodeFor(error.Code, ifMatchPresent),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code.ToString() });
    }
}

