using BeatSaverMatcher.Web.Result;
using Microsoft.AspNetCore.Mvc;

namespace BeatSaverMatcher.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        private readonly WorkItemStore _itemStore;

        public MatchesController(WorkItemStore itemStore)
        {
            _itemStore = itemStore;
        }

        [HttpPost("{playlistId}")]
        public void StartMatch([FromRoute] string playlistId)
        {
            if (!_itemStore.Enqueue(new WorkResultItem(playlistId)))
                Conflict();
        }

        [HttpGet("{playlistId}")]
        public WorkResultItem GetMatchState([FromRoute] string playlistId)
        {
            return _itemStore.Get(playlistId);
        }
    }
}