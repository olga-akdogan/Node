using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Api.Swiping;
using Node.Web.Models.Swiping;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>
/// API equivalent of <see cref="Node.Web.Controllers.SwipeController"/>: the discover
/// stack for the MAUI app. Members only, same as the web page.
/// </summary>
[ApiController]
[Route("api/swipes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = DbSeeder.RoleMember)]
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
        var candidate = await _swipeService.GetNextCandidateAsync(_userManager.GetUserId(User)!);
        if (candidate is null)
        {
            return NoContent();
        }

        return Ok(candidate);
    }

    /// <summary>Registers a like or pass; reports whether it resulted in a mutual match.</summary>
    [HttpPost]
    public async Task<ActionResult<SwipeResultDto>> Rate(SwipeRequest request)
    {
        var (isMatch, matchId) = await _swipeService.RateAsync(
            _userManager.GetUserId(User)!, request.TargetUserId, request.IsLike);

        return Ok(new SwipeResultDto { IsMatch = isMatch, MatchId = matchId });
    }
}
