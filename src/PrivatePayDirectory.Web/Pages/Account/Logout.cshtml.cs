using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PrivatePayDirectory.Web.Pages.Account;

public class LogoutModel : PageModel
{
    // POST is handled by the minimal API in Program.cs
    public IActionResult OnGet() => RedirectToPage("/Index");
}
