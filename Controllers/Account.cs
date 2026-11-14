namespace AcxiomCRM.Controllers;

[AllowAnonymous]
public class AccountController(UserManager<AppUser> um, SignInManager<AppUser> sm, AuditService audit, PasswordHistoryService pwdHistory) : Controller {
    public IActionResult Login() => View(new LoginVm());

    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginVm m, string returnUrl) {
        if (!ModelState.IsValid) return View(m);
        var u = await um.FindByEmailAsync(m.Email);
        if (u == null || !u.IsActive) {
            await audit.LogAsync("FailedLogin", "User", null, null, new { m.Email });
            ModelState.AddModelError("", "Invalid credentials.");
            return View(m);
        }
        var r = await sm.PasswordSignInAsync(u, m.Password, m.RememberMe, lockoutOnFailure: true);
        if (r.Succeeded) {
            await audit.LogAsync("Login", "User", u.Id, userId: u.Id);
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/Dashboard");
        }
        await audit.LogAsync(r.IsLockedOut ? "Lockout" : "FailedLogin", "User", u.Id, userId: u.Id);
        ModelState.AddModelError("", r.IsLockedOut ? "Account locked. Try again later." : "Invalid credentials.");
        return View(m);
    }

    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm m) {
        if (!ModelState.IsValid) return View(m);
        var u = new AppUser { UserName = m.Email, Email = m.Email, FullName = m.FullName };
        var r = await um.CreateAsync(u, m.Password);
        if (!r.Succeeded) {
            foreach (var e in r.Errors) ModelState.AddModelError("", e.Description);
            return View(m);
        }
        await pwdHistory.RecordPasswordAsync(u);
        await um.AddToRoleAsync(u, Roles.Sales);
        await audit.LogAsync("Register", "User", u.Id, userId: u.Id);
        await sm.SignInAsync(u, false);
        return Redirect("/Dashboard");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout() {
        await audit.LogAsync("Logout", "User", User.FindFirstValue(ClaimTypes.NameIdentifier));
        await sm.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordVm());

    [HttpPost, Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm m) {
        if (!ModelState.IsValid) return View(m);
        var u = await um.GetUserAsync(User);
        if (await pwdHistory.IsPasswordReusedAsync(u, m.New)) {
            ModelState.AddModelError("New", "You cannot reuse any of your last 5 passwords.");
            return View(m);
        }
        var r = await um.ChangePasswordAsync(u, m.Current, m.New);
        if (!r.Succeeded) {
            foreach (var e in r.Errors) ModelState.AddModelError("", e.Description);
            return View(m);
        }
        await pwdHistory.RecordPasswordAsync(u);
        await audit.LogAsync("PasswordChange", "User", u.Id);
        TempData["ok"] = "Password changed.";
        return Redirect("/Dashboard");
    }

    public IActionResult AccessDenied() => View();
}
