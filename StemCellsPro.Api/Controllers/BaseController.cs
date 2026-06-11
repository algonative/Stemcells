using Microsoft.AspNetCore.Mvc;

namespace StemCellsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    // Common controller logic can go here
}
