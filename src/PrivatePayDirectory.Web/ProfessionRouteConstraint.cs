using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Web;

/// <summary>
/// Route constraint "profession": matches only known profession slugs (e.g. "therapists"),
/// so the per-profession directory route doesn't swallow every other top-level URL.
/// </summary>
public class ProfessionRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) && Taxonomy.FindBySlug(value?.ToString()) != null;
}
