using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Models;
using Node.Web.Models.Api.Swiping;
using Node.Web.Models.Swiping;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>API equivalent of <see cref="Node.Web.Controllers.SwipeController"/>: the discover stack for the MAUI app.</summary>
[ApiController]
[Route("api/swipes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SwipesController : ControllerBase
{
    private readonly ISwipeService _swipeService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SwipesController(ISwipeService swipeService, UserManager<ApplicationUser> userManager)
    {
        _swipeService = swipeService;
        _userManager = userManager;
    }

    /// <summary>The next candidate the user hasn't rated yet, or 204 when the stack is empty.</summary>
    [HttpGet("next")]
    public async Task<ActionResult<SwipeCardViewModel>> GetNext()
    {
        var kandidaat = await _swipeService.GetVolgendeKandidaatAsync(_userManager.GetUserId(User)!);
        if (kandidaat is null)
        {
            return NoContent();
        }

        return Ok(kandidaat);
    }

    /// <summary>Registers a like or pass; reports whether it resulted in a mutual match.</summary>
    [HttpPost]
    public async Task<ActionResult<SwipeResultDto>> Beoordeel(SwipeRequest request)
    {
        var (isMatch, matchId) = await _swipeService.BeoordeelAsync(
            _userManager.GetUserId(User)!, request.TargetUserId, request.IsLike);

        return Ok(new SwipeResultDto { IsMatch = isMatch, MatchId = matchId });
    }
}
