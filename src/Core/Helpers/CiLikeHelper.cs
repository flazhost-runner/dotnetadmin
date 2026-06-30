namespace DotNetAdmin.Core.Helpers;

public static class CiLikeHelper
{
    /// <summary>
    /// Cross-dialect case-insensitive LIKE: LOWER(column) LIKE LOWER('%term%').
    /// Works on SQLite, MySQL (utf8_general_ci), and PostgreSQL via ToLower().Contains().
    /// EF Core translates ToLower().Contains() → LOWER(col) LIKE '%term%' for all supported providers.
    /// </summary>
    public static IQueryable<T> WhereCiLike<T>(
        this IQueryable<T> query,
        Expression<Func<T, string?>> property,
        string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var lower = searchTerm.ToLowerInvariant();
        var parameter = property.Parameters[0];
        var memberAccess = property.Body;

        // Handle nullable: coalesce to empty string
        var coalesced = Expression.Coalesce(memberAccess, Expression.Constant(string.Empty));

        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        var toLowerCall = Expression.Call(coalesced, toLowerMethod);
        var containsCall = Expression.Call(toLowerCall, containsMethod, Expression.Constant(lower));
        var lambda = Expression.Lambda<Func<T, bool>>(containsCall, parameter);

        return query.Where(lambda);
    }
}
