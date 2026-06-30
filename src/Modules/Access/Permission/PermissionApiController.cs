using DotNetAdmin.Core.Filters;
using DotNetAdmin.Modules.Access.Permission.Dtos;

namespace DotNetAdmin.Modules.Access.Permission;

[ApiController]
[Authorize(AuthenticationSchemes = "JwtBearer")]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class PermissionApiController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionApiController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("/api/v1/access/permission", Name = "api.v1.access.permission.index")]
    public async Task<IActionResult> Index([FromQuery] PermissionFilterDto filter)
    {
        var result = await _permissionService.GetAllAsync(filter);
        return Ok(new
        {
            success = true,
            datas = result.Data,
            paginate_data = new
            {
                total_data = result.TotalCount,
                page_size = result.PageSize,
                current_page = result.Page,
                total_page = result.TotalPages
            }
        });
    }

    [HttpGet("/api/v1/access/permission/{id}", Name = "api.v1.access.permission.show")]
    public async Task<IActionResult> Show(string id)
    {
        try
        {
            var perm = await _permissionService.GetByIdAsync(id);
            return Ok(new { success = true, data = perm });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }

    [HttpDelete("/api/v1/access/permission/{id}", Name = "api.v1.access.permission.delete")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _permissionService.DeleteAsync(id);
            return Ok(new { success = true, message = "Permission deleted." });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }
}
