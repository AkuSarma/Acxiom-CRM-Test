namespace AcxiomCRM.Controllers.Api;

public class CustomerInput {
    [Required, StringLength(100)] public string CustomerName { get; set; }
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; }
    [Required, RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; }
    [StringLength(100)] public string CompanyName { get; set; }
    [StringLength(250)] public string Address { get; set; }
    [StringLength(60)] public string City { get; set; }
    [StringLength(60)] public string State { get; set; }
    [RegularExpression("^(Active|Inactive)$")] public string Status { get; set; } = "Active";
    [StringLength(1000)] public string Notes { get; set; }
    public string AssignedTo { get; set; }
}
public record CustomerDto(int Id, string CustomerCode, string CustomerName, string Email, string Phone, string CompanyName, string Address, string City, string State, string Status, string Notes, string AssignedTo, DateTime CreatedDate);

public class LeadInput {
    [Required, StringLength(100)] public string LeadName { get; set; }
    [EmailAddress] public string Email { get; set; }
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; }
    [StringLength(100)] public string CompanyName { get; set; }
    [StringLength(60)] public string Source { get; set; }
    [Required, RegularExpression("^(New|Contacted|Qualified|Unqualified|Lost)$", ErrorMessage = "Invalid status.")] public string Status { get; set; } = "New";
    [RegularExpression("^(High|Medium|Low)$", ErrorMessage = "Invalid priority.")] public string Priority { get; set; } = "Medium";
    [Range(0, 100000000)] public double ExpectedValue { get; set; }
    [StringLength(1000)] public string Notes { get; set; }
    public string AssignedTo { get; set; }
}
public record LeadDto(int Id, string LeadCode, string LeadName, string Email, string Phone, string CompanyName, string Source, string Status, string Priority, double ExpectedValue, string Notes, string AssignedTo, DateTime CreatedDate);

public class OpportunityInput : IValidatableObject {
    [Required, StringLength(100)] public string OpportunityName { get; set; }
    [Required] public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public double Amount { get; set; }
    [Required, RegularExpression("^(Qualification|Proposal|Negotiation|Won|Lost)$")] public string Stage { get; set; } = "Qualification";
    [RegularExpression("^(Open|Won|Lost|On Hold)$")] public string Status { get; set; } = "Open";
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")] public int Probability { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    [StringLength(60)] public string Source { get; set; }
    [StringLength(1000)] public string Notes { get; set; }
    public string AssignedTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext c) {
        if (Stage == "Won") Status = "Won";
        else if (Stage == "Lost") Status = "Lost";
        bool a = Stage != "Won" && Stage != "Lost" && Status == "Open";
        if (Amount < 0 || (a && Amount <= 0)) yield return new("Opportunity Amount must be greater than 0.", new[] { nameof(Amount) });
        if (a && ExpectedCloseDate.Date < DateTime.Today) yield return new("Expected Close Date cannot be in the past.", new[] { nameof(ExpectedCloseDate) });
    }
}
public record OpportunityDto(int Id, string OpportunityName, int? CustomerId, int? LeadId, double Amount, string Stage, string Status, int Probability, DateTime ExpectedCloseDate, string Source, string Notes, string AssignedTo, DateTime CreatedDate);

public class FollowUpInput : IValidatableObject {
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
    [Required, StringLength(150)] public string Subject { get; set; }
    [Required] public DateTime FollowUpDate { get; set; }
    [Required, RegularExpression("^(Call|Meeting|Email|Task)$")] public string FollowUpType { get; set; } = "Call";
    [Required, RegularExpression("^(Planned|Completed|Missed|Cancelled)$")] public string Status { get; set; } = "Planned";
    [StringLength(500)] public string Remarks { get; set; }
    public string AssignedTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext c) {
        if (Status == "Planned" && FollowUpDate.Date < DateTime.Today)
            yield return new("Follow-up date cannot be earlier than today.", new[] { nameof(FollowUpDate) });
    }
}
public record FollowUpDto(int Id, int? CustomerId, int? LeadId, int? OpportunityId, string Subject, DateTime FollowUpDate, string FollowUpType, string Status, string Remarks, string AssignedTo, DateTime CreatedDate);

public class LoginInput {
    [Required] public string Email { get; set; }
    [Required] public string Password { get; set; }
    public bool SetCookie { get; set; } = true;
}

[ApiController, Route("api/auth"), AllowAnonymous, IgnoreAntiforgeryToken]
public class AuthApi(UserManager<AppUser> um, SignInManager<AppUser> sm, AuditService audit, JwtTokenService jwt) : ControllerBase {
    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginInput m) {
        var u = await um.FindByEmailAsync(m.Email);
        if (u == null || !u.IsActive) {
            await audit.LogAsync("FailedLogin", "User", null, null, new { m.Email });
            return Unauthorized(new { error = "Invalid credentials." });
        }
        var r = m.SetCookie
            ? await sm.PasswordSignInAsync(u, m.Password, false, true)
            : await sm.CheckPasswordSignInAsync(u, m.Password, true);

        if (r.Succeeded) {
            var roles = await um.GetRolesAsync(u);
            var (token, expiresAt) = jwt.GenerateToken(u, roles);
            await audit.LogAsync("Login", "User", u.Id, userId: u.Id);
            return Ok(new {
                ok = true,
                token,
                expiresAt,
                user = new { u.Id, u.Email, u.FullName, roles }
            });
        }
        await audit.LogAsync(r.IsLockedOut ? "Lockout" : "FailedLogin", "User", u.Id, userId: u.Id);
        return r.IsLockedOut ? StatusCode(423, new { error = "Account locked." }) : Unauthorized(new { error = "Invalid credentials." });
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout() {
        await audit.LogAsync("Logout", "User");
        await sm.SignOutAsync();
        return Ok(new { ok = true });
    }

    [HttpGet("me"), Authorize]
    public IActionResult Me() => Ok(new {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name,
        roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
    });
}

[ApiController, Route("api/customers"), Authorize, IgnoreAntiforgeryToken]
public class CustomersApi(AppDb db, AuditService audit) : ControllerBase {
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);
    bool CanAssign => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
    static CustomerDto D(Customer c) => new(c.Id, c.CustomerCode, c.CustomerName, c.Email, c.Phone, c.CompanyName, c.Address, c.City, c.State, c.Status, c.Notes, c.AssignedTo, c.CreatedDate);

    [HttpGet]
    public async Task<IActionResult> List(string q, string status, string assignedTo, int page = 1) {
        var qry = db.Customers.ForUser(db, User);
        if (!string.IsNullOrWhiteSpace(q)) qry = qry.Where(c => c.CustomerName.Contains(q) || c.Email.Contains(q) || c.Phone.Contains(q) || c.CompanyName.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) qry = qry.Where(c => c.Status == status);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) qry = qry.Where(c => c.AssignedTo == assignedTo);
        var t = await qry.CountAsync();
        var items = (await qry.OrderByDescending(c => c.Id).Skip((Math.Max(page, 1) - 1) * 20).Take(20).ToListAsync()).Select(D);
        return Ok(new { total = t, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) {
        var c = await db.Customers.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        return c == null ? NotFound(new { error = "Not found" }) : Ok(D(c));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerInput m) {
        if (await db.Customers.AnyAsync(c => c.Email.ToLower() == m.Email.ToLower() || c.Phone == m.Phone))
            return Conflict(new { error = "Customer with this email/phone already exists." });
        var assignee = CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive) ? m.AssignedTo : Uid;
        var c = new Customer {
            CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            CustomerName = m.CustomerName, Email = m.Email, Phone = m.Phone, CompanyName = m.CompanyName,
            Address = m.Address, City = m.City, State = m.State, Status = m.Status, Notes = m.Notes,
            CreatedBy = Uid, AssignedTo = assignee
        };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        await audit.LogAsync("Create", "Customer", c.Id.ToString(), null, D(c));
        return CreatedAtAction(nameof(Get), new { id = c.Id }, D(c));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerInput m) {
        var c = await db.Customers.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound(new { error = "Not found" });
        if (await db.Customers.AnyAsync(x => x.Id != id && (x.Email.ToLower() == m.Email.ToLower() || x.Phone == m.Phone)))
            return Conflict(new { error = "Duplicate email/phone." });
        var old = D(c);
        c.CustomerName = m.CustomerName; c.Email = m.Email; c.Phone = m.Phone; c.CompanyName = m.CompanyName;
        c.Address = m.Address; c.City = m.City; c.State = m.State; c.Status = m.Status; c.Notes = m.Notes;
        c.ModifiedDate = DateTime.UtcNow;
        if (CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive))
            c.AssignedTo = m.AssignedTo;
        await db.SaveChangesAsync();
        await audit.LogAsync("Update", "Customer", id.ToString(), old, D(c));
        return Ok(D(c));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id) {
        var c = await db.Customers.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound(new { error = "Not found" });
        var old = D(c);
        c.Status = "Inactive";
        c.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await audit.LogAsync("Delete", "Customer", id.ToString(), old);
        return NoContent();
    }
}

[ApiController, Route("api/leads"), Authorize, IgnoreAntiforgeryToken]
public class LeadsApi(AppDb db, AuditService audit) : ControllerBase {
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);
    bool CanAssign => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
    static LeadDto D(Lead l) => new(l.Id, l.LeadCode, l.LeadName, l.Email, l.Phone, l.CompanyName, l.Source, l.Status, l.Priority, l.ExpectedValue, l.Notes, l.AssignedTo, l.CreatedDate);

    [HttpGet]
    public async Task<IActionResult> List(string q, string status, string priority, string assignedTo) {
        var qry = db.Leads.ForUser(db, User);
        if (!string.IsNullOrWhiteSpace(q)) qry = qry.Where(l => l.LeadName.Contains(q) || l.CompanyName.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) qry = qry.Where(l => l.Status == status);
        if (!string.IsNullOrWhiteSpace(priority)) qry = qry.Where(l => l.Priority == priority);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) qry = qry.Where(l => l.AssignedTo == assignedTo);
        return Ok((await qry.OrderByDescending(l => l.Id).Take(100).ToListAsync()).Select(D));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) {
        var l = await db.Leads.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        return l == null ? NotFound(new { error = "Not found" }) : Ok(D(l));
    }

    [HttpPost]
    public async Task<IActionResult> Create(LeadInput m) {
        var assignee = CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive) ? m.AssignedTo : Uid;
        var l = new Lead {
            LeadCode = "LD-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            LeadName = m.LeadName, Email = m.Email, Phone = m.Phone, CompanyName = m.CompanyName,
            Source = m.Source, Status = m.Status, Priority = m.Priority ?? "Medium",
            ExpectedValue = m.ExpectedValue, Notes = m.Notes, CreatedBy = Uid, AssignedTo = assignee
        };
        db.Leads.Add(l);
        await db.SaveChangesAsync();
        await audit.LogAsync("Create", "Lead", l.Id.ToString(), null, D(l));
        return CreatedAtAction(nameof(Get), new { id = l.Id }, D(l));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LeadInput m) {
        var l = await db.Leads.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound(new { error = "Not found" });
        if (m.Status != l.Status && !(LeadsController.Flow.TryGetValue(l.Status, out var allowed) && allowed.Contains(m.Status)))
            return BadRequest(new { error = $"Invalid status transition: {l.Status} -> {m.Status}." });

        var old = D(l);
        l.LeadName = m.LeadName; l.Email = m.Email; l.Phone = m.Phone; l.CompanyName = m.CompanyName;
        l.Source = m.Source; l.Status = m.Status; l.Priority = m.Priority ?? l.Priority;
        l.ExpectedValue = m.ExpectedValue; l.Notes = m.Notes; l.ModifiedDate = DateTime.UtcNow;
        if (CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive))
            l.AssignedTo = m.AssignedTo;
        await db.SaveChangesAsync();
        await audit.LogAsync("Update", "Lead", id.ToString(), old, D(l));
        return Ok(D(l));
    }

    [HttpPost("{id:int}/convert")]
    public async Task<IActionResult> Convert(int id) {
        var l = await db.Leads.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound(new { error = "Not found" });
        if (l.Status != "Qualified") return BadRequest(new { error = "Only Qualified leads can be converted." });
        if (string.IsNullOrEmpty(l.Email) || string.IsNullOrEmpty(l.Phone)) return BadRequest(new { error = "Lead needs email and phone to convert." });
        if (await db.Customers.AnyAsync(c => c.Email == l.Email || c.Phone == l.Phone)) return Conflict(new { error = "A customer with this email/phone already exists." });

        await using var tx = await db.Database.BeginTransactionAsync();
        var c = new Customer {
            CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            CustomerName = l.LeadName, Email = l.Email, Phone = l.Phone, CompanyName = l.CompanyName,
            Notes = l.Notes, AssignedTo = l.AssignedTo, CreatedBy = Uid
        };
        db.Customers.Add(c);
        await db.SaveChangesAsync();

        var o = new Opportunity {
            OpportunityName = l.LeadName + " - Opportunity", CustomerId = c.Id, LeadId = l.Id,
            Amount = Math.Max(l.ExpectedValue, 1), Stage = "Qualification", Status = "Open",
            Probability = 25, ExpectedCloseDate = DateTime.Today.AddDays(30), Source = l.Source,
            Notes = l.Notes, AssignedTo = l.AssignedTo, CreatedBy = Uid
        };
        db.Opportunities.Add(o);
        l.Status = "Converted";
        l.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await audit.LogAsync("LeadConversion", "Lead", id.ToString(), null, new { CustomerId = c.Id, OpportunityId = o.Id });
        return Ok(new { ok = true, customerId = c.Id, opportunityId = o.Id });
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id) {
        var l = await db.Leads.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound(new { error = "Not found" });
        var old = D(l);
        db.Leads.Remove(l);
        await db.SaveChangesAsync();
        await audit.LogAsync("Delete", "Lead", id.ToString(), old);
        return NoContent();
    }
}

[ApiController, Route("api/opportunities"), Authorize, IgnoreAntiforgeryToken]
public class OpportunitiesApi(AppDb db, AuditService audit) : ControllerBase {
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);
    bool CanAssign => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
    static OpportunityDto D(Opportunity o) => new(o.Id, o.OpportunityName, o.CustomerId, o.LeadId, o.Amount, o.Stage, o.Status, o.Probability, o.ExpectedCloseDate, o.Source, o.Notes, o.AssignedTo, o.CreatedDate);

    [HttpGet]
    public async Task<IActionResult> List(string q, int? customerId, string stage, string status, string assignedTo) {
        var qry = db.Opportunities.ForUser(db, User);
        if (!string.IsNullOrWhiteSpace(q)) qry = qry.Where(o => o.OpportunityName.Contains(q));
        if (customerId.HasValue) qry = qry.Where(o => o.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(stage)) qry = qry.Where(o => o.Stage == stage);
        if (!string.IsNullOrWhiteSpace(status)) qry = qry.Where(o => o.Status == status);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) qry = qry.Where(o => o.AssignedTo == assignedTo);
        return Ok((await qry.OrderByDescending(o => o.Id).Take(100).ToListAsync()).Select(D));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) {
        var o = await db.Opportunities.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        return o == null ? NotFound(new { error = "Not found" }) : Ok(D(o));
    }

    [HttpPost]
    public async Task<IActionResult> Create(OpportunityInput m) {
        if (!await db.Customers.ForUser(db, User).AnyAsync(c => c.Id == m.CustomerId))
            return BadRequest(new { error = "Customer does not exist or is not accessible." });
        var assignee = CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive) ? m.AssignedTo : Uid;
        var o = new Opportunity {
            OpportunityName = m.OpportunityName, CustomerId = m.CustomerId, LeadId = m.LeadId,
            Amount = m.Amount, Stage = m.Stage, Status = m.Stage is "Won" or "Lost" ? m.Stage : m.Status,
            Probability = m.Probability, ExpectedCloseDate = m.ExpectedCloseDate,
            Source = m.Source, Notes = m.Notes, CreatedBy = Uid, AssignedTo = assignee
        };
        db.Opportunities.Add(o);
        await db.SaveChangesAsync();
        await audit.LogAsync("Create", "Opportunity", o.Id.ToString(), null, D(o));
        return CreatedAtAction(nameof(Get), new { id = o.Id }, D(o));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OpportunityInput m) {
        var o = await db.Opportunities.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound(new { error = "Not found" });
        if (!await db.Customers.ForUser(db, User).AnyAsync(c => c.Id == m.CustomerId))
            return BadRequest(new { error = "Customer does not exist or is not accessible." });
        var old = D(o);
        o.OpportunityName = m.OpportunityName; o.CustomerId = m.CustomerId; o.LeadId = m.LeadId;
        o.Amount = m.Amount; o.Stage = m.Stage; o.Status = m.Stage is "Won" or "Lost" ? m.Stage : m.Status;
        o.Probability = m.Probability; o.ExpectedCloseDate = m.ExpectedCloseDate;
        o.Source = m.Source; o.Notes = m.Notes; o.ModifiedDate = DateTime.UtcNow;
        if (CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive))
            o.AssignedTo = m.AssignedTo;
        await db.SaveChangesAsync();
        await audit.LogAsync("Update", "Opportunity", id.ToString(), old, D(o));
        return Ok(D(o));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id) {
        var o = await db.Opportunities.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound(new { error = "Not found" });
        var old = D(o);
        db.Opportunities.Remove(o);
        await db.SaveChangesAsync();
        await audit.LogAsync("Delete", "Opportunity", id.ToString(), old);
        return NoContent();
    }
}

[ApiController, Route("api/followups"), Authorize, IgnoreAntiforgeryToken]
public class FollowUpsApi(AppDb db, AuditService audit) : ControllerBase {
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);
    bool CanAssign => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
    static FollowUpDto D(FollowUp f) => new(f.Id, f.CustomerId, f.LeadId, f.OpportunityId, f.Subject, f.FollowUpDate, f.FollowUpType, f.Status, f.Remarks, f.AssignedTo, f.CreatedDate);

    [HttpGet]
    public async Task<IActionResult> List(string q, string status, DateTime? date, int? customerId, int? leadId, string assignedTo, string view) {
        var qry = db.FollowUps.ForUser(db, User);
        if (!string.IsNullOrWhiteSpace(q)) qry = qry.Where(f => f.Subject.Contains(q) || f.Remarks.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) qry = qry.Where(f => f.Status == status);
        if (date.HasValue) { var d = date.Value.Date; qry = qry.Where(f => f.FollowUpDate >= d && f.FollowUpDate < d.AddDays(1)); }
        if (customerId.HasValue) qry = qry.Where(f => f.CustomerId == customerId);
        if (leadId.HasValue) qry = qry.Where(f => f.LeadId == leadId);
        if (CanAssign && !string.IsNullOrWhiteSpace(assignedTo)) qry = qry.Where(f => f.AssignedTo == assignedTo);
        var today = DateTime.Today;
        if (view == "overdue") qry = qry.Where(f => f.Status == "Planned" && f.FollowUpDate < today);
        else if (view == "upcoming") qry = qry.Where(f => f.Status == "Planned" && f.FollowUpDate >= today);
        return Ok((await qry.OrderBy(f => f.FollowUpDate).Take(100).ToListAsync()).Select(D));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) {
        var f = await db.FollowUps.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        return f == null ? NotFound(new { error = "Not found" }) : Ok(D(f));
    }

    [HttpPost]
    public async Task<IActionResult> Create(FollowUpInput m) {
        var assignee = CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive) ? m.AssignedTo : Uid;
        var f = new FollowUp {
            CustomerId = m.CustomerId, LeadId = m.LeadId, OpportunityId = m.OpportunityId,
            Subject = m.Subject, FollowUpDate = m.FollowUpDate, FollowUpType = m.FollowUpType,
            Status = m.Status, Remarks = m.Remarks, CreatedBy = Uid, AssignedTo = assignee
        };
        db.FollowUps.Add(f);
        await db.SaveChangesAsync();
        await audit.LogAsync("Create", "FollowUp", f.Id.ToString(), null, D(f));
        return CreatedAtAction(nameof(Get), new { id = f.Id }, D(f));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FollowUpInput m) {
        var f = await db.FollowUps.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (f == null) return NotFound(new { error = "Not found" });
        var old = D(f);
        f.CustomerId = m.CustomerId; f.LeadId = m.LeadId; f.OpportunityId = m.OpportunityId;
        f.Subject = m.Subject; f.FollowUpDate = m.FollowUpDate; f.FollowUpType = m.FollowUpType;
        f.Status = m.Status; f.Remarks = m.Remarks; f.ModifiedDate = DateTime.UtcNow;
        if (CanAssign && !string.IsNullOrWhiteSpace(m.AssignedTo) && await db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive))
            f.AssignedTo = m.AssignedTo;
        await db.SaveChangesAsync();
        await audit.LogAsync("Update", "FollowUp", id.ToString(), old, D(f));
        return Ok(D(f));
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id) {
        var f = await db.FollowUps.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (f == null) return NotFound(new { error = "Not found" });
        var old = f.Status;
        f.Status = "Completed";
        f.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await audit.LogAsync("Complete", "FollowUp", id.ToString(), new { Status = old }, new { f.Status });
        return Ok(D(f));
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id) {
        var f = await db.FollowUps.ForUser(db, User).FirstOrDefaultAsync(x => x.Id == id);
        if (f == null) return NotFound(new { error = "Not found" });
        var old = D(f);
        db.FollowUps.Remove(f);
        await db.SaveChangesAsync();
        await audit.LogAsync("Delete", "FollowUp", id.ToString(), old);
        return NoContent();
    }
}

[ApiController, Route("api/reports"), Authorize(Roles = "Admin,Manager"), IgnoreAntiforgeryToken]
public class ReportsApi(AppDb db) : ControllerBase {
    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline() {
        var list = await db.Opportunities.ForUser(db, User).ToListAsync();
        return Ok(list.GroupBy(o => o.Stage).Select(g => new {
            stage = g.Key,
            count = g.Count(),
            amount = g.Sum(x => x.Amount),
            weighted = g.Sum(x => x.Amount * x.Probability / 100)
        }));
    }
}
