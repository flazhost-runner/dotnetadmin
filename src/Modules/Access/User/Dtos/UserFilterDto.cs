namespace DotNetAdmin.Modules.Access.User.Dtos;

public class UserFilterDto
{
    public string? q_name   { get; set; }
    public string? q_email  { get; set; }
    public string? q_code   { get; set; }
    public string? q_status { get; set; }
    public string? q_role   { get; set; }
    public int Page          { get; set; } = 1;
    public int q_page_size   { get; set; } = 10;

    // Legacy: keep Search/Status so existing pagination links still bind
    public string? Search  { get; set; }
    public string? Status  { get; set; }

    public int PageSize => q_page_size > 0 ? q_page_size : 10;
}
