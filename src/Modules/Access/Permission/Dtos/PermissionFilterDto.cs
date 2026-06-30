using Microsoft.AspNetCore.Mvc;

namespace DotNetAdmin.Modules.Access.Permission.Dtos;

public class PermissionFilterDto
{
    [FromQuery(Name = "q_name")]      public string? QName { get; set; }
    [FromQuery(Name = "q_guard")]     public string? QGuard { get; set; }
    [FromQuery(Name = "q_method")]    public string? QMethod { get; set; }
    [FromQuery(Name = "q_status")]    public string? QStatus { get; set; }
    [FromQuery(Name = "q_desc")]      public string? QDesc { get; set; }
    [FromQuery(Name = "q_page")]      public int Page { get; set; } = 1;
    [FromQuery(Name = "q_page_size")] public int PageSize { get; set; } = 10;
}
