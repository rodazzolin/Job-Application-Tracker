using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Controllers;

// The public landing page at "/". No [Authorize], so anyone can see it, signed in or not;
// the view only swaps its buttons depending on whether you're logged in.
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
