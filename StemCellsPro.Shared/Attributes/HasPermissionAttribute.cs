using Microsoft.AspNetCore.Authorization;

namespace StemCellsPro.Shared.Attributes;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
        Permission = permission;
    }
}
