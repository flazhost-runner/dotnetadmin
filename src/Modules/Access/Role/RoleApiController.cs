using DotNetAdmin.Core.Filters;
using DotNetAdmin.Modules.Access.Role.Dtos;

namespace DotNetAdmin.Modules.Access.Role;

[ApiController]
[Authorize(AuthenticationSchemes = "JwtBearer")]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class RoleApiController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleApiController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("/api/v1/access/role", Name = "api.v1.access.role.index")]
    public async Task<IActionResult> Index([FromQuery] RoleFilterDto filter)
    {
        var result = await _roleService.GetAllAsync(filter);
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

    [HttpGet("/api/v1/access/role/{id}", Name = "api.v1.access.role.show")]
    public async Task<IActionResult> Show(string id)
    {
        try
        {
            var role = await _roleService.GetByIdAsync(id);
            return Ok(new { success = true, data = role });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }

    [HttpPost("/api/v1/access/role", Name = "api.v1.access.role.store")]
    public async Task<IActionResult> Store([FromBody] RoleCreateDto dto)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var role = await _roleService.CreateAsync(dto, actorId);
            return CreatedAtRoute("api.v1.access.role.show", new { id = role.Id }, new { success = true, data = role });
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { success = false, message = ex.Message }); }
    }

    [HttpPut("/api/v1/access/role/{id}", Name = "api.v1.access.role.update")]
    public async Task<IActionResult> Update(string id, [FromBody] RoleUpdateDto dto)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var role = await _roleService.UpdateAsync(id, dto, actorId);
            return Ok(new { success = true, data = role });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { success = false, message = ex.Message }); }
    }

    [HttpDelete("/api/v1/access/role/{id}", Name = "api.v1.access.role.delete")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _roleService.DeleteAsync(id);
            return Ok(new { success = true, message = "Role deleted." });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }
}
