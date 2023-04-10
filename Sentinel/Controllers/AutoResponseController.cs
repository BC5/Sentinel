using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Sentinel.Controllers
{
    public class AutoResponseController : Controller
    {
        private SentinelCore _core;

        public AutoResponseController(SentinelCore core)
        {
            _core = core;
        }

        public IActionResult List(ulong server)
        {
            if (server == null || server == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yikes. Looks like you didn't specify a server";
                return View("Error");
            }

            SentinelDatabase data = _core.GetDbContext();
            ServerConfig? scfg = data.GetServerConfigNoCreate(server).Result;

            if(scfg == null) 
            {
                ViewData["ErrCode"] = 404;
                ViewData["ErrDetail"] = "Not a real server. You're going insane.";
                return View("Error");
            }
            ViewData["ServerDetails"] = scfg;
            ViewData["AutoResponses"] = scfg.AutoResponses;
            return View();
        }

        public IActionResult Edit(ulong server, int id)
        {
            if (server == null || server == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yikes. Looks like you didn't specify a server";
                return View("Error");
            }
            if (id == null || id == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yeah, gonna need an ID to do that...";
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

            AutoResponse? ar = scfg.AutoResponses.FirstOrDefault(x => x.ResponseId == id);
            if (ar == null)
            {
                ViewData["ErrCode"] = 404;
                ViewData["ErrDetail"] = "That autoresponse doesn't exist. Take your meds.";
                return View("Error");
            }

            ViewData["ServerDetails"] = scfg;
            ViewData["AutoResponse"] = ar;
            return View();

        }

        public IActionResult Add(ulong server)
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
            ViewData["AutoResponse"] = new AutoResponse();
            return View();
        }

        public IActionResult Remove(ulong server, int id)
        {
            if (server == null || server == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yikes. Looks like you didn't specify a server";
                return View("Error");
            }
            if (id == null || id == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yeah, gonna need an ID to do that...";
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

            AutoResponse? ar = scfg.AutoResponses.FirstOrDefault(x => x.ResponseId == id);
            if (ar == null)
            {
                ViewData["ErrCode"] = 404;
                ViewData["ErrDetail"] = "That autoresponse doesn't exist. Take your meds.";
                return View("Error");
            }

            scfg.AutoResponses.Remove(ar);
            data.SaveChanges();

            return RedirectToAction("List", new { server });
        }

        [HttpPost, ActionName("Edit")]
        public IActionResult EditPost(ulong server, int id, [Bind("Trigger,ResponseText,ResponseEmote,Wildcard,TargetUser,TargetChannel,Chance,RateLimit,ReloadTime")] AutoResponse ar)
        {
            if (server == null || server == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yikes. Looks like you didn't specify a server";
                return View("Error");
            }
            if (id == null || id == 0)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Yeah, gonna need an ID to do that...";
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

            AutoResponse? ar_old = scfg.AutoResponses.FirstOrDefault(x => x.ResponseId == id);
            if (ar_old == null)
            {
                ViewData["ErrCode"] = 404;
                ViewData["ErrDetail"] = "That autoresponse doesn't exist. Take your meds.";
                return View("Error");
            }

            if (ar.Trigger == null || ar.Trigger == "")
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "What do you expect me to do without a trigger?";
                return View("Error");
            }
            ar = Sanitise(ar);
            if (ar.ResponseEmote == null && ar.ResponseText == null)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "What use is an autoresponse with no response?";
                return View("Error");
            }
            if(!(ar.ResponseEmote == null) && InvalidEmote(ar.ResponseEmote))
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Give me a unicode emoji or that stupid discord emote format (like <:thake:1019347821100539916> or whatever)";
                return View("Error");
            }

            scfg.AutoResponses.Remove(ar_old);
            scfg.AutoResponses.Add(ar);

            data.SaveChanges();

            return RedirectToAction("List",new { server });
        }

        [HttpPost, ActionName("Add")]
        public IActionResult AddPost(ulong server, [Bind("Trigger,ResponseText,ResponseEmote,Wildcard,TargetUser,TargetChannel,Chance,RateLimit,ReloadTime")] AutoResponse ar)
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

            if (ar.Trigger == null || ar.Trigger == "")
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "What do you expect me to do without a trigger?";
                return View("Error");
            }
            ar = Sanitise(ar);
            if (ar.ResponseEmote == null && ar.ResponseText == null)
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "What use is an autoresponse with no response?";
                return View("Error");
            }
            if (!(ar.ResponseEmote == null) && InvalidEmote(ar.ResponseEmote))
            {
                ViewData["ErrCode"] = 400;
                ViewData["ErrDetail"] = "Give me a unicode emoji or that stupid discord emote format (like <:thake:1019347821100539916> or whatever)";
                return View("Error");
            }
            scfg.AutoResponses.Add(ar);

            data.SaveChanges();

            return RedirectToAction("List", new { server });
        }

        public static bool InvalidEmote(string emote)
        {
            bool isEmote = Discord.Emote.TryParse(emote, out _);
            bool isEmoji = Discord.Emoji.TryParse(emote, out _);
            return !(isEmote || isEmoji);
        }

        public static AutoResponse Sanitise(AutoResponse ar)
        {
            if (ar.ResponseText == "") ar.ResponseText = null;
            if (ar.ResponseEmote == "") ar.ResponseEmote = null;
            if (ar.TargetUser == 0) ar.TargetUser = null;
            if (ar.TargetChannel == 0) ar.TargetChannel = null;
            ar.Trigger = ar.Trigger.ToUpper();
            return ar;
        }
    }
}
