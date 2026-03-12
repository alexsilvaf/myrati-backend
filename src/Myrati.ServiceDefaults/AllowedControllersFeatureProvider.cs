using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Myrati.ServiceDefaults;

internal sealed class AllowedControllersFeatureProvider(IEnumerable<Type> allowedControllerTypes)
    : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly HashSet<TypeInfo> _allowedControllerTypes = allowedControllerTypes
        .Select(type => type.GetTypeInfo())
        .ToHashSet();

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        for (var index = feature.Controllers.Count - 1; index >= 0; index--)
        {
            if (_allowedControllerTypes.Contains(feature.Controllers[index]))
            {
                continue;
            }

            feature.Controllers.RemoveAt(index);
        }
    }
}
