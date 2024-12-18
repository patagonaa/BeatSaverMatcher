using BeatSaverMatcher.Web.Result;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace BeatSaverMatcher.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        private readonly WorkItemStore _itemStore;
        private readonly Counter _startMatchCounter;

        public MatchesController(WorkItemStore itemStore)
        {
            _itemStore = itemStore;
            _startMatchCounter = Metrics.CreateCounter("beatsaver_start_match_count", "number of times match was started");
        }

        [HttpPost("{playlistId}")]
        public ActionResult StartMatch([FromRoute] string playlistId)
        {
            if (!_itemStore.Enqueue(playlistId))
                return Conflict();

            _startMatchCounter.Inc();
            return Ok();
        }

        [HttpGet("{playlistId}")]
        public WorkResultItem GetMatchState([FromRoute] string playlistId)
        {
            return _itemStore.Get(playlistId);
        }
    }
}