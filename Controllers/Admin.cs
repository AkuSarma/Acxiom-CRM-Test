using System.Security.Cryptography;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController(UserManager<AppUser> um, AuditService audit, PasswordHistoryService pwdHistory) : Controller {
    public async Task<IActionResult> Index() {
        var users = await um.Users.OrderBy(u => u.Email).ToListAsync();
        var nameMap = users.ToDictionary(u => u.Id, u => u.FullName);
        var managers = new List<AppUser>();
        var rows = new List<UserRow>();

        foreach (var u in users) {
            var role = (await um.GetRolesAsync(u)).FirstOrDefault();
            if (role == Roles.Manager) managers.Add(u);
            rows.Add(new UserRow {
                User = u,
                Role = role,
                Locked = await um.IsLockedOutAsync(u),
                ManagerName = u.ManagerId != null && nameMap.TryGetValue(u.ManagerId, out var mn) ? mn : "-"
            });
        }
        ViewBag.Managers = new SelectList(managers, "Id", "FullName");
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> SetRole(string id, string role) {
        var u = await um.FindByIdAsync(id);
        if (u == null || !Roles.All.Contains(role)) return BadRequest();
        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier) && role != Roles.Admin) {
            TempData["err"] = "You cannot demote yourself.";
            return RedirectToAction(nameof(Index));
        }
        var old = await um.GetRolesAsync(u);
        await um.RemoveFromRolesAsync(u, old);
        await um.AddToRoleAsync(u, role);
        await um.UpdateSecurityStampAsync(u);
        await audit.LogAsync("RoleChange", "User", id, new { Role = old.FirstOrDefault() }, new { Role = role });
        TempData["ok"] = "Role updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetTeam(string id, string managerId, string teamName) {
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        if (managerId == id) {
            TempData["err"] = "A user cannot be their own manager.";
            return RedirectToAction(nameof(Index));
        }
        var oldTeam = new { u.ManagerId, u.TeamName };
        u.ManagerId = string.IsNullOrWhiteSpace(managerId) ? null : managerId;
        u.TeamName = string.IsNullOrWhiteSpace(teamName) ? null : teamName.Trim();
        await um.UpdateAsync(u);
        await audit.LogAsync("TeamUpdate", "User", id, oldTeam, new { u.ManagerId, u.TeamName });
        TempData["ok"] = "Team & manager assignment updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id) {
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier)) {
            TempData["err"] = "You cannot deactivate yourself.";
            return RedirectToAction(nameof(Index));
        }
        u.IsActive = !u.IsActive;
        await um.UpdateAsync(u);
        await audit.LogAsync(u.IsActive ? "Activate" : "Deactivate", "User", id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(string id) {
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        await um.SetLockoutEndDateAsync(u, null);
        await um.ResetAccessFailedCountAsync(u);
        await audit.LogAsync("Unlock", "User", id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string id) {
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        var temp = Convert.ToBase64String(RandomNumberGenerator.GetBytes(9)).Replace('+', 'x').Replace('/', 'y') + "aA1!";
        var r = await um.ResetPasswordAsync(u, await um.GeneratePasswordResetTokenAsync(u), temp);
        if (!r.Succeeded) {
            TempData["err"] = "Reset failed.";
            return RedirectToAction(nameof(Index));
        }
        await pwdHistory.RecordPasswordAsync(u);
        await audit.LogAsync("PasswordReset", "User", id);
        TempData["ok"] = $"Temporary password for {u.Email}: {temp} (shown once)";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Admin")]
public class RolesController(AppDb db, UserManager<AppUser> um, AuditService audit) : Controller {
    public async Task<IActionResult> Index() {
        var summaries = new List<RoleSummaryVm>();
        foreach (var r in Roles.All) {
            var inRole = await um.GetUsersInRoleAsync(r);
            summaries.Add(new RoleSummaryVm {
                RoleName = r,
                UserCount = inRole.Count,
                Users = inRole.Select(u => $"{u.FullName} ({u.Email})").OrderBy(x => x).ToList()
            });
        }
        var perms = await db.RolePermissions.OrderBy(p => p.RoleName).ThenBy(p => p.Id).ToListAsync();
        return View(new RolesPermissionsVm { Roles = summaries, Permissions = perms });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePermission(int id, bool canView, bool canCreate, bool canEdit, bool canDelete, bool canAssign) {
        var p = await db.RolePermissions.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        var old = new { p.CanView, p.CanCreate, p.CanEdit, p.CanDelete, p.CanAssign };
        p.CanView = canView;
        p.CanCreate = canCreate;
        p.CanEdit = canEdit;
        p.CanDelete = canDelete;
        p.CanAssign = canAssign;
        await db.SaveChangesAsync();
        await audit.LogAsync("PermissionUpdate", "RolePermission", $"{p.RoleName}:{p.Module}", old, new { p.CanView, p.CanCreate, p.CanEdit, p.CanDelete, p.CanAssign });
        TempData["ok"] = $"Updated permissions for {p.RoleName} on {p.Module}.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Admin")]
public class AuditController(AppDb db) : Controller {
    public async Task<IActionResult> Index(string user, string entity, string action, DateTime? from, DateTime? to) {
        var q = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(user)) q = q.Where(a => a.UserId == user);
        if (!string.IsNullOrEmpty(entity)) q = q.Where(a => a.EntityName == entity);
        if (!string.IsNullOrEmpty(action)) q = q.Where(a => a.Action == action);
        if (from.HasValue) q = q.Where(a => a.CreatedDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(a => a.CreatedDate < to.Value.Date.AddDays(1));
        ViewBag.UserNames = await db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        return View(await q.OrderByDescending(a => a.Id).Take(200).ToListAsync());
    }
}

public class DashboardController(AppDb db) : Controller {
    public async Task<IActionResult> Index(DateTime? from, DateTime? to) {
        var c = db.Customers.ForUser(db, User, from, to);
        var l = db.Leads.ForUser(db, User, from, to);
        var o = db.Opportunities.ForUser(db, User, from, to);
        var open = o.Where(x => x.Stage != "Won" && x.Stage != "Lost");

        var vm = new DashVm {
            ScopeLabel = User.IsInRole(Roles.Admin) ? "Organization-Wide Scope"
                       : User.IsInRole(Roles.Manager) ? "Manager Team Scope"
                       : "Assigned Personal Scope"
        };
        vm.Cards["Total Customers"] = (await c.CountAsync()).ToString();
        vm.Cards["Total Leads"] = (await l.CountAsync()).ToString();
        vm.Cards["Open Leads"] = (await l.CountAsync(x => x.Status != "Converted" && x.Status != "Lost" && x.Status != "Unqualified")).ToString();
        vm.Cards["Total Opportunities"] = (await o.CountAsync()).ToString();
        vm.Cards["Open Opportunities"] = (await open.CountAsync()).ToString();
        vm.Cards["Won Opportunities"] = (await o.CountAsync(x => x.Stage == "Won")).ToString();
        vm.Cards["Lost Opportunities"] = (await o.CountAsync(x => x.Stage == "Lost")).ToString();
        vm.Cards["Total Pipeline Value"] = (await open.SumAsync(x => x.Amount)).ToString("N0");

        foreach (var s in new[] { "New", "Contacted", "Qualified", "Lost", "Converted" })
            vm.LeadStatus[s] = await l.CountAsync(x => x.Status == s);
        foreach (var s in new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" })
            vm.Stages[s] = await o.CountAsync(x => x.Stage == s);

        var wonList = await o.Where(x => x.Stage == "Won").Select(x => new { x.ExpectedCloseDate, x.Amount }).ToListAsync();
        foreach (var g in wonList.GroupBy(x => x.ExpectedCloseDate.ToString("yyyy-MM")).OrderBy(g => g.Key))
            vm.Monthly[g.Key] = g.Sum(x => x.Amount);

        return View(vm);
    }
}

public class ReportsController(AppDb db) : Controller {
    public async Task<IActionResult> Index(
        string name = "customers",
        DateTime? from = null,
        DateTime? to = null,
        string sortBy = null,
        string sortDir = "asc",
        int page = 1,
        bool csv = false) {

        var names = await db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        string Nm(string id) => id != null && names.TryGetValue(id, out var n) ? n : "Unassigned";
        string D(DateTime d) => d.ToString("yyyy-MM-dd");
        bool mgr = !User.IsInRole(Roles.Sales);
        var rows = new List<Dictionary<string, object>>();

        switch (name) {
            case "customers":
                foreach (var x in await db.Customers.ForUser(db, User, from, to).ToListAsync())
                    rows.Add(new() { ["Name"] = x.CustomerName, ["Email"] = x.Email, ["Status"] = x.Status, ["Owner"] = Nm(x.AssignedTo), ["Created"] = D(x.CreatedDate) });
                break;
            case "leads":
                foreach (var x in await db.Leads.ForUser(db, User, from, to).ToListAsync())
                    rows.Add(new() { ["Name"] = x.LeadName, ["Source"] = x.Source ?? "-", ["Status"] = x.Status, ["Priority"] = x.Priority, ["Owner"] = Nm(x.AssignedTo), ["Converted"] = x.Status == "Converted" ? "Yes" : "No" });
                break;
            case "followups":
                foreach (var x in await db.FollowUps.ForUser(db, User, from, to).OrderBy(f => f.FollowUpDate).ToListAsync())
                    rows.Add(new() { ["Date"] = D(x.FollowUpDate), ["Subject"] = x.Subject, ["Type"] = x.FollowUpType, ["Status"] = x.Status, ["Owner"] = Nm(x.AssignedTo), ["Overdue"] = x.Status == "Planned" && x.FollowUpDate < DateTime.Today ? "Yes" : "No" });
                break;
            case "opportunities":
                foreach (var x in await db.Opportunities.ForUser(db, User, from, to).ToListAsync())
                    rows.Add(new() { ["Name"] = x.OpportunityName, ["Stage"] = x.Stage, ["Status"] = x.Status, ["Amount"] = x.Amount, ["Probability"] = x.Probability, ["ExpectedClose"] = D(x.ExpectedCloseDate), ["Owner"] = Nm(x.AssignedTo) });
                break;
            case "pipeline": {
                var os = await db.Opportunities.ForUser(db, User, from, to).ToListAsync();
                void G(string kind, Func<Opportunity, string> k) {
                    foreach (var g in os.GroupBy(k))
                        rows.Add(new() { ["Group"] = kind, ["Name"] = g.Key, ["Count"] = g.Count(), ["Amount"] = g.Sum(x => x.Amount), ["Weighted"] = g.Sum(x => x.Amount * x.Probability / 100) });
                }
                G("Stage", x => x.Stage);
                G("Owner", x => Nm(x.AssignedTo));
                break;
            }
            case "conversion": {
                var ls = db.Leads.ForUser(db, User, from, to);
                var os = db.Opportunities.ForUser(db, User, from, to);
                int t = await ls.CountAsync(), cv = await ls.CountAsync(x => x.Status == "Converted");
                rows.Add(new() { ["Metric"] = "Total leads", ["Value"] = t });
                rows.Add(new() { ["Metric"] = "Converted", ["Value"] = cv });
                rows.Add(new() { ["Metric"] = "Not converted", ["Value"] = t - cv });
                rows.Add(new() { ["Metric"] = "Conversion %", ["Value"] = t == 0 ? 0 : cv * 100 / t });
                rows.Add(new() { ["Metric"] = "Won", ["Value"] = await os.CountAsync(x => x.Stage == "Won") });
                rows.Add(new() { ["Metric"] = "Lost", ["Value"] = await os.CountAsync(x => x.Stage == "Lost") });
                break;
            }
            case "useractivity":
                if (!mgr) return Forbid();
                var aq = db.AuditLogs.AsQueryable();
                if (from.HasValue) aq = aq.Where(a => a.CreatedDate >= from.Value.Date);
                if (to.HasValue) aq = aq.Where(a => a.CreatedDate < to.Value.Date.AddDays(1));
                foreach (var g in await aq.GroupBy(a => new { a.UserId, a.Action }).Select(g => new { g.Key.UserId, g.Key.Action, C = g.Count() }).ToListAsync())
                    rows.Add(new() { ["User"] = Nm(g.UserId), ["Action"] = g.Action, ["Count"] = g.C });
                break;
            case "audit":
                if (!User.IsInRole(Roles.Admin)) return Forbid();
                var aud = db.AuditLogs.AsQueryable();
                if (from.HasValue) aud = aud.Where(a => a.CreatedDate >= from.Value.Date);
                if (to.HasValue) aud = aud.Where(a => a.CreatedDate < to.Value.Date.AddDays(1));
                foreach (var a in await aud.OrderByDescending(a => a.Id).Take(500).ToListAsync())
                    rows.Add(new() { ["Time"] = a.CreatedDate.ToString("u"), ["User"] = Nm(a.UserId), ["Action"] = a.Action, ["Entity"] = a.EntityName, ["Record"] = a.RecordId, ["IP"] = a.IpAddress });
                break;
            default:
                return NotFound();
        }

        // Dynamic Sorting
        sortDir = sortDir?.ToLower() == "desc" ? "desc" : "asc";
        if (!string.IsNullOrWhiteSpace(sortBy) && rows.Count > 0 && rows[0].ContainsKey(sortBy)) {
            rows = sortDir == "desc"
                ? rows.OrderByDescending(r => r[sortBy] is IComparable c ? c : r[sortBy]?.ToString()).ToList()
                : rows.OrderBy(r => r[sortBy] is IComparable c ? c : r[sortBy]?.ToString()).ToList();
        }

        if (csv) {
            if (rows.Count == 0) return File(Array.Empty<byte>(), "text/csv", name + ".csv");
            var k = rows[0].Keys.ToList();
            string E(object v) => "\"" + (v?.ToString() ?? "").Replace("\"", "\"\"") + "\"";
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", k));
            foreach (var r in rows) sb.AppendLine(string.Join(",", k.Select(x => E(r[x]))));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", name + ".csv");
        }

        // Pagination
        const int pageSize = 20;
        int totalRows = rows.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var pagedRows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return View(new ReportVm {
            Name = name,
            Rows = pagedRows,
            From = from,
            To = to,
            SortBy = sortBy,
            SortDir = sortDir,
            Page = page,
            Pages = totalPages,
            TotalRows = totalRows
        });
    }
}
