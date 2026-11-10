using Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers.Base;
using WebApplication1.DTOs;

namespace WebApplication1.Controllers;

[Route("watch-statuses")]
public class WatchStatusesController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<WatchingTypeDto>> GetAll() =>
        Ok(WatchStatusSlug.All
            .Select(option => new WatchingTypeDto
            {
                Id = option.Slug,
                Slug = option.Slug,
                Title = option.Title
            })
            .ToList());
}
