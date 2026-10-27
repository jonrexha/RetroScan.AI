namespace RetroScan.Api.Owners;

// There are no accounts: each browser gets a random owner id in a cookie, and every query is scoped to it.
public sealed class OwnerCookieMiddleware(RequestDelegate next)
{
    public const string CookieName = "retroscan_owner";

    public Task InvokeAsync(HttpContext context)
    {
        if (!Guid.TryParse(context.Request.Cookies[CookieName], out var ownerId) || ownerId == Guid.Empty)
        {
            ownerId = Guid.NewGuid();
            context.Response.Cookies.Append(CookieName, ownerId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(365),
                IsEssential = true,
            });
        }

        context.Items[CookieName] = ownerId;
        return next(context);
    }
}

public static class OwnerHttpContextExtensions
{
    public static Guid GetOwnerId(this HttpContext context) =>
        context.Items[OwnerCookieMiddleware.CookieName] as Guid?
        ?? throw new InvalidOperationException($"{nameof(OwnerCookieMiddleware)} did not run for {context.Request.Path}.");
}
