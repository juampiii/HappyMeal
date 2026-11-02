using System.Security.Claims;
using Happymeal.Application.Common.Interfaces;

namespace Happymeal.API.Services;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Id
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return ICurrentUser.System;
            }

            return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub")
                ?? ICurrentUser.Anonymous;
        }
    }
}
