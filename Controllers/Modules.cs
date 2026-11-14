namespace AcxiomCRM.Controllers;

public class CustomersController(AppDb db, AuditService audit, IModelMetadataProvider mp) : CrudController<Customer>(db, audit, mp) {
    protected override string Title => "Customers";
    protected override string[] Columns => new[] { "CustomerCode", "CustomerName", "Email", "Phone", "CompanyName", "Status", "AssignedTo" };

    protected override IQueryable<Customer> Search(IQueryable<Customer> q, string s) =>
        q.Where(c => c.CustomerName.Contains(s) || c.Email.Contains(s) || c.Phone.Contains(s) || c.CompanyName.Contains(s) || c.CustomerCode.Contains(s));

    protected override IQueryable<Customer> ApplyFilters(IQueryable<Customer> q) {
        var status = Request.Query["status"].ToString();
        var assignedTo = Request.Query["assignedTo"].ToString();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(c => c.Status == status);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) q = q.Where(c => c.AssignedTo == assignedTo);
        return q;
    }

    protected override Task LoadFilterLists() {
        ViewData["FilterStatuses"] = new SelectList(new[] { "Active", "Inactive" }, Request.Query["status"]);
        return Task.CompletedTask;
    }

    protected override Task Lists(Customer m) {
        ViewData["StatusList"] = new SelectList(new[] { "Active", "Inactive" }, m.Status);
        return Task.CompletedTask;
    }

    protected override void BeforeCreate(Customer m) => m.CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
    protected override void Preserve(Customer m, Customer e) => m.CustomerCode = e.CustomerCode;
    protected override void Remove(Customer e) { e.Status = "Inactive"; e.ModifiedDate = DateTime.UtcNow; }

    protected override async Task<string> Validate(Customer m, Customer old, bool isNew) {
        var em = (m.Email ?? "").Trim().ToLower();
        if (await Db.Customers.AnyAsync(c => c.Id != m.Id && c.Email.ToLower() == em))
            return "A customer with this email already exists.";
        if (await Db.Customers.AnyAsync(c => c.Id != m.Id && c.Phone == m.Phone))
            return "A customer with this phone already exists.";
        return null;
    }

    public override async Task<IActionResult> Details(int id) {
        var c = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        var userNames = await Db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        string Resolve(string uid) => uid != null && userNames.TryGetValue(uid, out var n) ? n : "Unassigned";

        var vm = new CustomerDetailsVm {
            Customer = c,
            AssignedToName = Resolve(c.AssignedTo),
            CreatedByName = Resolve(c.CreatedBy),
            Opportunities = await Db.Opportunities.Where(o => o.CustomerId == id).OrderByDescending(o => o.Id).ToListAsync(),
            FollowUps = await Db.FollowUps.Where(f => f.CustomerId == id).OrderByDescending(f => f.FollowUpDate).ToListAsync(),
            Activities = await Db.Activities.Where(a => a.CustomerId == id).OrderByDescending(a => a.ActivityDate).ToListAsync(),
            AuditLogs = await Db.AuditLogs.Where(a => a.EntityName == "Customer" && a.RecordId == id.ToString()).OrderByDescending(a => a.Id).Take(50).ToListAsync(),
            UserNames = userNames
        };
        return View("~/Views/Customers/Details.cshtml", vm);
    }
}

public class LeadsController(AppDb db, AuditService audit, IModelMetadataProvider mp) : CrudController<Lead>(db, audit, mp) {
    public static readonly Dictionary<string, string[]> Flow = new() {
        ["New"] = new[] { "Contacted", "Unqualified", "Lost" },
        ["Contacted"] = new[] { "Qualified", "Unqualified", "Lost" },
        ["Qualified"] = new[] { "Converted", "Lost" },
        ["Unqualified"] = Array.Empty<string>(),
        ["Converted"] = Array.Empty<string>(),
        ["Lost"] = Array.Empty<string>()
    };

    protected override string Title => "Leads";
    protected override string[] Columns => new[] { "LeadCode", "LeadName", "CompanyName", "Status", "Priority", "ExpectedValue", "AssignedTo" };
    protected override (string, string)[] RowActions => new[] { ("Convert", "Convert") };

    protected override IQueryable<Lead> Search(IQueryable<Lead> q, string s) =>
        q.Where(l => l.LeadName.Contains(s) || l.CompanyName.Contains(s) || l.Email.Contains(s) || l.Phone.Contains(s) || l.LeadCode.Contains(s));

    protected override IQueryable<Lead> ApplyFilters(IQueryable<Lead> q) {
        var status = Request.Query["status"].ToString();
        var priority = Request.Query["priority"].ToString();
        var assignedTo = Request.Query["assignedTo"].ToString();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(l => l.Status == status);
        if (!string.IsNullOrWhiteSpace(priority)) q = q.Where(l => l.Priority == priority);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) q = q.Where(l => l.AssignedTo == assignedTo);
        return q;
    }

    protected override Task LoadFilterLists() {
        ViewData["FilterStatuses"] = new SelectList(new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" }, Request.Query["status"]);
        ViewData["FilterPriorities"] = new SelectList(new[] { "High", "Medium", "Low" }, Request.Query["priority"]);
        return Task.CompletedTask;
    }

    protected override Task Lists(Lead m) {
        ViewData["StatusList"] = new SelectList(new[] { "New", "Contacted", "Qualified", "Unqualified", "Lost" }, m.Status);
        ViewData["PriorityList"] = new SelectList(new[] { "High", "Medium", "Low" }, m.Priority);
        return Task.CompletedTask;
    }

    protected override void BeforeCreate(Lead m) => m.LeadCode = "LD-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
    protected override void Preserve(Lead m, Lead e) => m.LeadCode = e.LeadCode;

    protected override Task<string> Validate(Lead m, Lead old, bool isNew) {
        if (!Flow.ContainsKey(m.Status ?? "")) return Task.FromResult("Invalid lead status.");
        if (isNew && m.Status == "Converted") return Task.FromResult("Use the Convert action to convert a lead.");
        if (!isNew && m.Status != old.Status && !(Flow[old.Status]?.Contains(m.Status) ?? false))
            return Task.FromResult($"Invalid status transition: {old.Status} -> {m.Status}.");
        return Task.FromResult<string>(null);
    }

    [HttpPost]
    public async Task<IActionResult> Convert(int id) {
        var l = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound();

        string Fail(string msg) { TempData["err"] = msg; return null; }
        if (l.Status != "Qualified") { Fail("Only Qualified leads can be converted."); return RedirectToAction(nameof(Index)); }
        if (string.IsNullOrEmpty(l.Email) || string.IsNullOrEmpty(l.Phone)) { Fail("Lead needs email and phone to convert."); return RedirectToAction(nameof(Index)); }
        if (await Db.Customers.AnyAsync(c => c.Email == l.Email || c.Phone == l.Phone)) { Fail("A customer with this email/phone already exists."); return RedirectToAction(nameof(Index)); }

        await using var tx = await Db.Database.BeginTransactionAsync();
        var c = new Customer {
            CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            CustomerName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Notes = l.Notes,
            AssignedTo = l.AssignedTo,
            CreatedBy = UserId
        };
        Db.Customers.Add(c);
        await Db.SaveChangesAsync();

        var o = new Opportunity {
            OpportunityName = l.LeadName + " - Opportunity",
            CustomerId = c.Id,
            LeadId = l.Id,
            Amount = Math.Max(l.ExpectedValue, 1),
            Stage = "Qualification",
            Status = "Open",
            Probability = 25,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            Source = l.Source,
            Notes = l.Notes,
            AssignedTo = l.AssignedTo,
            CreatedBy = UserId
        };
        Db.Opportunities.Add(o);
        l.Status = "Converted";
        l.ModifiedDate = DateTime.UtcNow;
        await Db.SaveChangesAsync();
        await tx.CommitAsync();

        await Audit.LogAsync("LeadConversion", "Lead", id.ToString(), null, new { CustomerId = c.Id, OpportunityId = o.Id });
        TempData["ok"] = "Lead converted to customer and opportunity.";
        return RedirectToAction(nameof(Index));
    }
}

public class OpportunitiesController(AppDb db, AuditService audit, IModelMetadataProvider mp) : CrudController<Opportunity>(db, audit, mp) {
    protected override string Title => "Opportunities";
    protected override string[] Columns => new[] { "OpportunityName", "Stage", "Status", "Amount", "Probability", "ExpectedCloseDate", "AssignedTo" };

    protected override IQueryable<Opportunity> Search(IQueryable<Opportunity> q, string s) =>
        q.Where(o => o.OpportunityName.Contains(s) || o.Stage.Contains(s) || o.Status.Contains(s));

    protected override IQueryable<Opportunity> ApplyFilters(IQueryable<Opportunity> q) {
        var stage = Request.Query["stage"].ToString();
        var status = Request.Query["status"].ToString();
        var assignedTo = Request.Query["assignedTo"].ToString();
        if (int.TryParse(Request.Query["customerId"], out var cid)) q = q.Where(o => o.CustomerId == cid);
        if (!string.IsNullOrWhiteSpace(stage)) q = q.Where(o => o.Stage == stage);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(o => o.Status == status);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) q = q.Where(o => o.AssignedTo == assignedTo);
        return q;
    }

    protected override async Task LoadFilterLists() {
        ViewData["FilterCustomers"] = await CustomerList(int.TryParse(Request.Query["customerId"], out var cid) ? cid : null);
        ViewData["FilterStages"] = new SelectList(new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" }, Request.Query["stage"]);
        ViewData["FilterStatuses"] = new SelectList(new[] { "Open", "Won", "Lost", "On Hold" }, Request.Query["status"]);
    }

    protected override async Task Lists(Opportunity m) {
        ViewData["StageList"] = new SelectList(new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" }, m.Stage);
        ViewData["StatusList"] = new SelectList(new[] { "Open", "Won", "Lost", "On Hold" }, m.Status);
        ViewData["CustomerIdList"] = await CustomerList(m.CustomerId);
        ViewData["LeadIdList"] = await LeadList(m.LeadId);
    }

    protected override async Task<string> Validate(Opportunity m, Opportunity old, bool isNew) =>
        await Db.Customers.ForUser(Db, User).AnyAsync(c => c.Id == m.CustomerId) ? null : "Selected customer does not exist or is not accessible.";
}

public class FollowUpsController(AppDb db, AuditService audit, IModelMetadataProvider mp) : CrudController<FollowUp>(db, audit, mp) {
    protected override string Title => "Follow-Ups";
    protected override string[] Columns => new[] { "FollowUpDate", "Subject", "FollowUpType", "Status", "AssignedTo", "Remarks" };
    protected override string[] Views => new[] { "overdue", "upcoming" };
    protected override (string, string)[] RowActions => new[] { ("Complete", "Complete") };

    protected override IQueryable<FollowUp> Search(IQueryable<FollowUp> q, string s) =>
        q.Where(f => f.Subject.Contains(s) || f.Remarks.Contains(s) || f.FollowUpType.Contains(s) || f.Status.Contains(s));

    protected override IQueryable<FollowUp> Filter(IQueryable<FollowUp> q, string v) {
        var t = DateTime.Today;
        return v == "overdue" ? q.Where(f => f.Status == "Planned" && f.FollowUpDate < t)
             : v == "upcoming" ? q.Where(f => f.Status == "Planned" && f.FollowUpDate >= t)
             : q;
    }

    protected override IQueryable<FollowUp> ApplyFilters(IQueryable<FollowUp> q) {
        var status = Request.Query["status"].ToString();
        var assignedTo = Request.Query["assignedTo"].ToString();
        if (DateTime.TryParse(Request.Query["date"], out var dt)) {
            var d = dt.Date;
            q = q.Where(f => f.FollowUpDate >= d && f.FollowUpDate < d.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(f => f.Status == status);
        if (int.TryParse(Request.Query["customerId"], out var cid)) q = q.Where(f => f.CustomerId == cid);
        if (int.TryParse(Request.Query["leadId"], out var lid)) q = q.Where(f => f.LeadId == lid);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) q = q.Where(f => f.AssignedTo == assignedTo);
        return q;
    }

    protected override async Task LoadFilterLists() {
        ViewData["FilterStatuses"] = new SelectList(new[] { "Planned", "Completed", "Missed", "Cancelled" }, Request.Query["status"]);
        ViewData["FilterCustomers"] = await CustomerList(int.TryParse(Request.Query["customerId"], out var cid) ? cid : null);
        ViewData["FilterLeads"] = await LeadList(int.TryParse(Request.Query["leadId"], out var lid) ? lid : null);
    }

    protected override async Task Lists(FollowUp m) {
        ViewData["FollowUpTypeList"] = new SelectList(new[] { "Call", "Meeting", "Email", "Task" }, m.FollowUpType);
        ViewData["StatusList"] = new SelectList(new[] { "Planned", "Completed", "Missed", "Cancelled" }, m.Status);
        ViewData["CustomerIdList"] = await CustomerList(m.CustomerId);
        ViewData["LeadIdList"] = await LeadList(m.LeadId);
        ViewData["OpportunityIdList"] = await OpportunityList(m.OpportunityId);
    }

    protected override Task<string> Validate(FollowUp m, FollowUp old, bool isNew) =>
        Task.FromResult(m.Status == "Planned" && m.FollowUpDate.Date < DateTime.Today && (isNew || m.FollowUpDate.Date != old.FollowUpDate.Date)
            ? "Follow-up date cannot be earlier than today."
            : null);

    [HttpPost]
    public async Task<IActionResult> Complete(int id) {
        var f = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
        if (f == null) return NotFound();
        var old = f.Status;
        f.Status = "Completed";
        f.ModifiedDate = DateTime.UtcNow;
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Complete", "FollowUp", id.ToString(), new { Status = old }, new { f.Status });
        TempData["ok"] = "Follow-up completed.";
        return RedirectToAction(nameof(Index));
    }
}

public class ActivitiesController(AppDb db, AuditService audit, IModelMetadataProvider mp) : CrudController<Activity>(db, audit, mp) {
    protected override string Title => "Activities";
    protected override string[] Columns => new[] { "ActivityType", "Subject", "ActivityDate", "Status", "AssignedTo" };

    protected override IQueryable<Activity> Search(IQueryable<Activity> q, string s) =>
        q.Where(a => a.Subject.Contains(s) || a.Description.Contains(s) || a.ActivityType.Contains(s) || a.Status.Contains(s));

    protected override IQueryable<Activity> ApplyFilters(IQueryable<Activity> q) {
        var type = Request.Query["activityType"].ToString();
        var status = Request.Query["status"].ToString();
        var assignedTo = Request.Query["assignedTo"].ToString();
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(a => a.ActivityType == type);
        if (DateTime.TryParse(Request.Query["date"], out var dt)) {
            var d = dt.Date;
            q = q.Where(a => a.ActivityDate >= d && a.ActivityDate < d.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) q = q.Where(a => a.AssignedTo == assignedTo);
        return q;
    }

    protected override Task LoadFilterLists() {
        ViewData["FilterActivityTypes"] = new SelectList(new[] { "Call", "Meeting", "Email", "Task" }, Request.Query["activityType"]);
        ViewData["FilterStatuses"] = new SelectList(new[] { "Open", "Done", "Cancelled" }, Request.Query["status"]);
        return Task.CompletedTask;
    }

    protected override async Task Lists(Activity m) {
        ViewData["ActivityTypeList"] = new SelectList(new[] { "Call", "Meeting", "Email", "Task" }, m.ActivityType);
        ViewData["StatusList"] = new SelectList(new[] { "Open", "Done", "Cancelled" }, m.Status);
        ViewData["CustomerIdList"] = await CustomerList(m.CustomerId);
        ViewData["LeadIdList"] = await LeadList(m.LeadId);
    }
}
