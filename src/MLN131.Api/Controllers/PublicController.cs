using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicController : ControllerBase
{
    [HttpGet("home")]
    [AllowAnonymous]
    public ActionResult<object> Home()
    {
        return Ok(new
        {
            name = "MLN131",
            description = "Trang web học tập chủ đề Cơ cấu xã hội - giai cấp và liên minh giai cấp, tầng lớp trong thời kỳ quá độ lên CNXH.",
        });
    }
}

