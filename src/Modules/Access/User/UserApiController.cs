using DotNetAdmin.Core.Filters;
using DotNetAdmin.Modules.Access.User.Dtos;

namespace DotNetAdmin.Modules.Access.User;

[ApiController]
[Authorize(AuthenticationSchemes = "JwtBearer")]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class UserApiController : ControllerBase
{
    private readonly IUserService _userService;

    public UserApiController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("/api/v1/access/user", Name = "api.v1.access.user.index")]
    public async Task<IActionResult> Index([FromQuery] UserFilterDto filter)
    {
        var result = await _userService.GetAllAsync(filter);
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

    [HttpGet("/api/v1/access/user/{id}", Name = "api.v1.access.user.show")]
    public async Task<IActionResult> Show(string id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(new { success = true, data = user });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }

    [HttpPost("/api/v1/access/user", Name = "api.v1.access.user.store")]
    public async Task<IActionResult> Store([FromBody] UserCreateDto dto)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var user = await _userService.CreateAsync(dto, actorId);
            return CreatedAtRoute("api.v1.access.user.show", new { id = user.Id }, new { success = true, data = user });
        }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, errors = ex.Errors }); }
    }

    [HttpPut("/api/v1/access/user/{id}", Name = "api.v1.access.user.update")]
    public async Task<IActionResult> Update(string id, [FromBody] UserUpdateDto dto)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var user = await _userService.UpdateAsync(id, dto, actorId);
            return Ok(new { success = true, data = user });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (AppException ex) { return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, errors = ex.Errors }); }
    }

    [HttpDelete("/api/v1/access/user/{id}", Name = "api.v1.access.user.delete")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            return Ok(new { success = true, message = "User deleted." });
        }
        catch (NotFoundAppException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }

    [HttpPost("/api/v1/access/user/delete_selected", Name = "api.v1.access.user.delete_selected")]
    public async Task<IActionResult> DeleteSelected([FromBody] DeleteSelectedRequest req)
    {
        await _userService.DeleteSelectedAsync(req.Selected ?? []);
        return Ok(new { success = true, message = "Users deleted." });
    }

    public record DeleteSelectedRequest(List<string>? Selected);
}
