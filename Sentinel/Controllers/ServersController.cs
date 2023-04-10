using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sentinel.Controllers
{
    public class ServersController : Controller
    {
        private SentinelCore _core;

        public ServersController(SentinelCore core)
        {
            _core = core;
        }

        public IActionResult Index()
        {
            SentinelDatabase db = _core.GetDbContext();
            List<ServerConfig> servers = db.Servers.ToList();

            ViewData["ServerList"] = servers;

            return View();
        }

        public IActionResult Info(ulong server)
        {
            if (server == null || server == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yikes. Looks like you didn't specify a server";
                return View("Error");
            }

            SentinelDatabase data = _core.GetDbContext();
            ServerConfig? scfg = data.GetServerConfigNoCreate(server).Result;

            if (scfg == null)
            {
                ViewData["ErrCode"] = 404;
                ViewData["ErrDetail"] = "Not a real server. You're going insane.";
                return View("Error");
            }
            ViewData["ServerDetails"] = scfg;
            ViewData["AutoResponses"] = scfg.AutoResponses;
            return View();
        }
    }
}
