using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Models;
using Node.Web.Models.Matches;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>API equivalent of <see cref="Node.Web.Controllers.MatchController"/>: match overview and chat handoff for the MAUI app.</summary>
[ApiController]
[Route("api/matches")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _matchService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MatchesController(IMatchService matchService, UserManager<ApplicationUser> userManager)
    {
        _matchService = matchService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<List<MatchOverviewViewModel>>> GetMatches()
    {
        var matches = await _matchService.GetMatchesVoorGebruikerAsync(_userManager.GetUserId(User)!);
        return Ok(matches);
    }

    /// <summary>Match data plus a short-lived GetStream token so the app can connect directly to the chat.</summary>
    [HttpGet("{id}/chat")]
    public async Task<ActionResult<ChatViewModel>> GetChat(int id)
    {
        var chat = await _matchService.GetChatAsync(id, _userManager.GetUserId(User)!);
        if (chat is null)
        {
            // Bestaat niet of de gebruiker is geen deelnemer: niets prijsgeven.
            return NotFound();
        }

        return Ok(chat);
    }
}
