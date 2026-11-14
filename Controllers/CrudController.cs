namespace AcxiomCRM.Controllers;

public abstract class CrudController<T>(AppDb db, AuditService audit, IModelMetadataProvider mp) : Controller where T : EntityBase, new() {
    const string EditView = "~/Views/Crud/Edit.cshtml";
    protected AppDb Db => db;
    protected AuditService Audit => audit;
    protected abstract string Title { get; }
    protected abstract string[] Columns { get; }
    protected abstract IQueryable<T> Search(IQueryable<T> q, string s);
    protected virtual IQueryable<T> Filter(IQueryable<T> q, string view) => q;
    protected virtual IQueryable<T> ApplyFilters(IQueryable<T> q) => q;
    protected virtual Task LoadFilterLists() => Task.CompletedTask;
    protected virtual string[] Views => null;
    protected virtual (string Action, string Label)[] RowActions => Array.Empty<(string, string)>();
    protected virtual Task Lists(T m) => Task.CompletedTask;
    protected virtual Task<string> Validate(T m, T old, bool isNew) => Task.FromResult<string>(null);
    protected virtual void BeforeCreate(T m) { }
    protected virtual void Preserve(T m, T e) { }
    protected virtual void Remove(T e) => Db.Remove(e);

    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    protected bool CanAssign => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
    protected IQueryable<T> Scoped() => Db.Set<T>().ForUser(Db, User);

    protected async Task<SelectList> CustomerList(int? selected = null) =>
        new(await Db.Customers.ForUser(Db, User).OrderBy(c => c.CustomerName).ToListAsync(), "Id", "CustomerName", selected);

    protected async Task<SelectList> LeadList(int? selected = null) =>
        new(await Db.Leads.ForUser(Db, User).OrderBy(c => c.LeadName).ToListAsync(), "Id", "LeadName", selected);

    protected async Task<SelectList> OpportunityList(int? selected = null) =>
        new(await Db.Opportunities.ForUser(Db, User).OrderBy(o => o.OpportunityName).ToListAsync(), "Id", "OpportunityName", selected);

    protected async Task<SelectList> AssignableUserList(string selected = null) {
        var q = Db.Users.Where(u => u.IsActive);
        if (User.IsInRole(Roles.Manager)) {
            var myId = UserId;
            q = q.Where(u => u.Id == myId || u.ManagerId == myId);
        }
        var users = await q.OrderBy(u => u.FullName).Select(u => new { u.Id, Display = u.FullName + " (" + u.Email + ")" }).ToListAsync();
        return new SelectList(users, "Id", "Display", selected);
    }

    async Task Prep(T m, bool edit) {
        ViewBag.Title = Title;
        ViewBag.IsEdit = edit;
        var props = mp.GetMetadataForType(typeof(T)).Properties.Where(p => p.ShowForEdit).Select(p => p.Name).ToList();
        if (CanAssign) {
            ViewData["AssignedToList"] = await AssignableUserList(m.AssignedTo ?? UserId);
        } else {
            props.Remove(nameof(EntityBase.AssignedTo));
        }
        ViewBag.Props = props.ToArray();
    }

    static T Clone(T e) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(e));
    async Task<IActionResult> Form(T m, bool edit) {
        if (!edit && string.IsNullOrEmpty(m.AssignedTo)) m.AssignedTo = UserId;
        await Lists(m);
        await Prep(m, edit);
        return View(EditView, m);
    }

    public async Task<IActionResult> Index(string q, string view, int page = 1) {
        var qry = Scoped();
        if (!string.IsNullOrWhiteSpace(q)) qry = Search(qry, q.Trim());
        qry = Filter(qry, view);
        qry = ApplyFilters(qry);

        var total = await qry.CountAsync();
        page = Math.Max(1, page);
        var items = (await qry.OrderByDescending(e => e.Id).Skip((page - 1) * 20).Take(20).ToListAsync()).Cast<EntityBase>().ToList();

        if (CanAssign) {
            ViewData["FilterUsers"] = await AssignableUserList(Request.Query["assignedTo"]);
        }
        await LoadFilterLists();

        ViewBag.UserNames = await Db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.Title = Title;
        ViewBag.Columns = Columns;
        ViewBag.Q = q;
        ViewBag.View = view;
        ViewBag.Page = page;
        ViewBag.Pages = (int)Math.Ceiling(total / 20.0);
        ViewBag.Total = total;
        ViewBag.Views = Views;
        ViewBag.RowActions = RowActions;
        ViewBag.CanAssign = CanAssign;
        return View("~/Views/Crud/Index.cshtml", items);
    }

    public virtual async Task<IActionResult> Details(int id) {
        var e = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();
        ViewBag.Title = Title;
        ViewBag.UserNames = await Db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        return View("~/Views/Crud/Details.cshtml", e);
    }

    public Task<IActionResult> Create() => Form(new T(), false);

    [HttpPost]
    public async Task<IActionResult> Create(T m) {
        m.Id = 0;
        m.CreatedBy = UserId;
        m.CreatedDate = DateTime.UtcNow;
        if (!CanAssign || string.IsNullOrWhiteSpace(m.AssignedTo) || !await Db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive)) {
            m.AssignedTo = UserId;
        }
        BeforeCreate(m);
        var err = await Validate(m, null, true);
        if (err != null) ModelState.AddModelError("", err);
        if (!ModelState.IsValid) return await Form(m, false);

        try {
            Db.Add(m);
            await Db.SaveChangesAsync();
        } catch (DbUpdateException) {
            ModelState.AddModelError("", "Duplicate record: email or phone already exists.");
            Db.Entry(m).State = EntityState.Detached;
            return await Form(m, false);
        }
        await Audit.LogAsync("Create", typeof(T).Name, m.Id.ToString(), null, m);
        TempData["ok"] = "Saved.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id) {
        var e = await Scoped().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();
        return await Form(e, true);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, T m) {
        var e = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();
        var old = Clone(e);

        m.Id = id;
        m.CreatedDate = e.CreatedDate;
        m.CreatedBy = e.CreatedBy;
        m.ModifiedDate = DateTime.UtcNow;
        if (!CanAssign || string.IsNullOrWhiteSpace(m.AssignedTo) || !await Db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive)) {
            m.AssignedTo = e.AssignedTo;
        }
        Preserve(m, e);

        var err = await Validate(m, old, false);
        if (err != null) ModelState.AddModelError("", err);
        if (!ModelState.IsValid) return await Form(m, true);

        try {
            Db.Entry(e).CurrentValues.SetValues(m);
            await Db.SaveChangesAsync();
        } catch (DbUpdateException) {
            ModelState.AddModelError("", "Duplicate record: email or phone already exists.");
            return await Form(m, true);
        }
        await Audit.LogAsync("Update", typeof(T).Name, id.ToString(), old, e);
        TempData["ok"] = "Saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id) {
        var e = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();
        var old = Clone(e);
        Remove(e);
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Delete", typeof(T).Name, id.ToString(), old);
        TempData["ok"] = "Deleted.";
        return RedirectToAction(nameof(Index));
    }
}
