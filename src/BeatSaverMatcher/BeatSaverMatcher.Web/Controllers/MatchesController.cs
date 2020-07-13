using BeatSaverMatcher.Common.Models;
using BeatSaverMatcher.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        private readonly MatchingService _matchingService;

        public MatchesController(MatchingService matchingService)
        {
            _matchingService = matchingService;
        }

        [HttpGet("{playlistId}")]
        public async Task<IList<SongMatch>> GetMatches([FromRoute] string playlistId)
        {
            return await _matchingService.GetMatches(playlistId);
        }
    }
}