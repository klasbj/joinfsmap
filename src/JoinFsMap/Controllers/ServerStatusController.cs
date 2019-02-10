using JoinFsMap.Services;
using Microsoft.AspNetCore.Mvc;

namespace JoinFsMap.Controllers {
    [Route("api/[controller]")]
    public class ServerStatusController : Controller {
        private IStatusContext contextManager;

        public ServerStatusController(IStatusContext contextManager) {
            this.contextManager = contextManager;
        }

        [HttpGet]
        public Context Get() {
            return contextManager.GetContext();
        }
    }
}