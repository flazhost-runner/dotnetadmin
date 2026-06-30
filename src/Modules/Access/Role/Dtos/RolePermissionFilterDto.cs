using Microsoft.AspNetCore.Mvc;

namespace DotNetAdmin.Modules.Access.Role.Dtos;

public class RolePermissionFilterDto
{
    [FromQuery(Name = "q_name")]      public string? QName { get; set; }
    [FromQuery(Name = "q_status")]    public string? QStatus { get; set; }
    [FromQuery(Name = "q_desc")]      public string? QDesc { get; set; }
    public int Page { get; set; } = 1;
    [FromQuery(Name = "q_page_size")] public int PageSize { get; set; } = 10;
}
