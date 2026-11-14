namespace AcxiomCRM.Data;

public class AppDb(DbContextOptions<AppDb> o) : IdentityDbContext<AppUser>(o) {
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder b) {
        base.OnModelCreating(b);
        b.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
        b.Entity<Customer>().HasIndex(c => c.Phone).IsUnique();
        b.Entity<RolePermission>().HasIndex(rp => new { rp.RoleName, rp.Module }).IsUnique();
        b.Entity<PasswordHistory>().HasIndex(ph => ph.UserId);
    }
}

public static class ScopeExt {
    public static IQueryable<T> ForUser<T>(this IQueryable<T> q, AppDb db, ClaimsPrincipal u, DateTime? from = null, DateTime? to = null) where T : EntityBase {
        var id = u.FindFirstValue(ClaimTypes.NameIdentifier);
        if (u.IsInRole(Roles.Sales)) {
            q = q.Where(e => e.AssignedTo == id);
        } else if (u.IsInRole(Roles.Manager)) {
            var teamIds = db.Users.Where(x => x.ManagerId == id).Select(x => x.Id);
            q = q.Where(e => e.AssignedTo == id || teamIds.Contains(e.AssignedTo));
        }
        if (from.HasValue) {
            var f = from.Value.Date;
            q = q.Where(e => e.CreatedDate >= f);
        }
        if (to.HasValue) {
            var t = to.Value.Date.AddDays(1);
            q = q.Where(e => e.CreatedDate < t);
        }
        return q;
    }

    public static IQueryable<T> ForUser<T>(this IQueryable<T> q, ClaimsPrincipal u, DateTime? from = null, DateTime? to = null) where T : EntityBase {
        if (u.IsInRole(Roles.Sales)) {
            var id = u.FindFirstValue(ClaimTypes.NameIdentifier);
            q = q.Where(e => e.AssignedTo == id);
        }
        if (from.HasValue) {
            var f = from.Value.Date;
            q = q.Where(e => e.CreatedDate >= f);
        }
        if (to.HasValue) {
            var t = to.Value.Date.AddDays(1);
            q = q.Where(e => e.CreatedDate < t);
        }
        return q;
    }
}

public static class Seeder {
    public static async Task RunAsync(IServiceProvider sp) {
        using var s = sp.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<AppDb>();
        var cfg = s.ServiceProvider.GetRequiredService<IConfiguration>();
        await db.Database.MigrateAsync();

        var rm = s.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var um = s.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        foreach (var r in Roles.All) {
            if (!await rm.RoleExistsAsync(r))
                await rm.CreateAsync(new IdentityRole(r));
        }

        // Seed default Role Permissions matrix
        if (!await db.RolePermissions.AnyAsync()) {
            foreach (var role in Roles.All) {
                foreach (var mod in Modules.All) {
                    bool isAdmin = role == Roles.Admin;
                    bool isMgr = role == Roles.Manager;
                    bool isCrm = mod is "Dashboard" or "Customers" or "Leads" or "Opportunities" or "FollowUps" or "Activities" or "RestApi" or "Reports";
                    db.RolePermissions.Add(new RolePermission {
                        RoleName = role,
                        Module = mod,
                        CanView = isAdmin || isCrm,
                        CanCreate = isAdmin || (isCrm && mod != "Dashboard" && mod != "Reports"),
                        CanEdit = isAdmin || (isCrm && mod != "Dashboard" && mod != "Reports"),
                        CanDelete = isAdmin || (isMgr && isCrm && mod != "Dashboard" && mod != "Reports"),
                        CanAssign = isAdmin || (isMgr && isCrm && mod != "Dashboard" && mod != "Reports")
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        bool seedEnabled = !bool.TryParse(cfg["SeedUsers:Enabled"], out var en) || en;
        if (!seedEnabled) return;

        var defaultPwd = cfg["SeedUsers:DefaultPassword"] ?? "Passw0rd!";

        async Task<AppUser> EnsureUser(string name, string email, string role, string team, string managerId = null) {
            var u = await um.FindByEmailAsync(email);
            if (u == null) {
                u = new AppUser {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true,
                    TeamName = team,
                    ManagerId = managerId
                };
                await um.CreateAsync(u, defaultPwd);
                await um.AddToRoleAsync(u, role);
                db.PasswordHistories.Add(new PasswordHistory { UserId = u.Id, PasswordHash = u.PasswordHash });
                await db.SaveChangesAsync();
            }
            return u;
        }

        var admin = await EnsureUser("System Admin", "admin@acxiom.com", Roles.Admin, "Administration");
        var manager = await EnsureUser("Priya Sharma", "manager@acxiom.com", Roles.Manager, "Enterprise Sales");
        var sales = await EnsureUser("Rahul Verma", "sales@acxiom.com", Roles.Sales, "Enterprise Sales", manager.Id);
        var sales2 = await EnsureUser("Anita Desai", "sales2@acxiom.com", Roles.Sales, "SMB Sales", null);

        // Seed sample CRM records if database is empty so Dashboard, Customer History & Reports work immediately
        if (!await db.Customers.AnyAsync() && !await db.Leads.AnyAsync()) {
            var c1 = new Customer {
                CustomerCode = "CUS-1001A", CustomerName = "Aarav Mehta", Email = "aarav@technova.in", Phone = "9876543210",
                CompanyName = "TechNova Solutions", Address = "42 MG Road", City = "Bengaluru", State = "Karnataka",
                Status = "Active", Notes = "Key enterprise account interested in annual CRM license.",
                CreatedBy = manager.Id, AssignedTo = sales.Id, CreatedDate = DateTime.UtcNow.AddDays(-20)
            };
            var c2 = new Customer {
                CustomerCode = "CUS-1002B", CustomerName = "Neha Kapoor", Email = "neha@bluepeak.co.in", Phone = "9823456781",
                CompanyName = "BluePeak Logistics", Address = "18 Linking Road", City = "Mumbai", State = "Maharashtra",
                Status = "Active", Notes = "Expanding regional warehouse operations.",
                CreatedBy = sales.Id, AssignedTo = sales.Id, CreatedDate = DateTime.UtcNow.AddDays(-12)
            };
            var c3 = new Customer {
                CustomerCode = "CUS-1003C", CustomerName = "Vikram Nair", Email = "vikram@greenleaf.org", Phone = "9811223344",
                CompanyName = "GreenLeaf Agro", Address = "7 Park Street", City = "Kolkata", State = "West Bengal",
                Status = "Active", Notes = "SMB retail chain account.",
                CreatedBy = sales2.Id, AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-8)
            };
            db.Customers.AddRange(c1, c2, c3);
            await db.SaveChangesAsync();

            var l1 = new Lead {
                LeadCode = "LD-2001A", LeadName = "Siddharth Malhotra", Email = "sid@cloudpulse.io", Phone = "9845011223",
                CompanyName = "CloudPulse Systems", Source = "Website", Status = "Qualified", Priority = "High",
                ExpectedValue = 250000, Notes = "Requested enterprise demo and pricing proposal.",
                CreatedBy = manager.Id, AssignedTo = sales.Id, CreatedDate = DateTime.UtcNow.AddDays(-5)
            };
            var l2 = new Lead {
                LeadCode = "LD-2002B", LeadName = "Kavita Joshi", Email = "kavita@finbridge.in", Phone = "9890123456",
                CompanyName = "FinBridge Capital", Source = "Referral", Status = "Contacted", Priority = "Medium",
                ExpectedValue = 180000, Notes = "Initial discovery call completed.",
                CreatedBy = sales.Id, AssignedTo = sales.Id, CreatedDate = DateTime.UtcNow.AddDays(-3)
            };
            var l3 = new Lead {
                LeadCode = "LD-2003C", LeadName = "Rohan Kulkarni", Email = "rohan@urbanmart.in", Phone = "9765432109",
                CompanyName = "UrbanMart Retail", Source = "LinkedIn", Status = "New", Priority = "Low",
                ExpectedValue = 95000, Notes = "Inbound inquiry from campaign.",
                CreatedBy = sales2.Id, AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-1)
            };
            db.Leads.AddRange(l1, l2, l3);
            await db.SaveChangesAsync();

            var o1 = new Opportunity {
                OpportunityName = "TechNova Annual Enterprise Suite", CustomerId = c1.Id, Amount = 450000,
                Stage = "Negotiation", Status = "Open", Probability = 75, ExpectedCloseDate = DateTime.Today.AddDays(15),
                Source = "Direct", Notes = "Final contract review with legal.",
                CreatedBy = manager.Id, AssignedTo = sales.Id, CreatedDate = DateTime.UtcNow.AddDays(-14)
            };
            var o2 = new Opportunity {
                OpportunityName = "BluePeak Fleet CRM Rollout", CustomerId = c2.Id, Amount = 320000,
                Stage = "Won", Status = "Won", Probability = 100, ExpectedCloseDate = DateTime.Today.AddDays(-2),
                Source = "Referral", Notes = "Signed 12-month agreement.",
                CreatedBy = sales.Id, AssignedTo = sales.Id, CreatedDate = DateTime.UtcNow.AddDays(-10)
            };
            var o3 = new Opportunity {
                OpportunityName = "GreenLeaf POS Integration", CustomerId = c3.Id, Amount = 120000,
                Stage = "Proposal", Status = "Open", Probability = 50, ExpectedCloseDate = DateTime.Today.AddDays(25),
                Source = "Inbound", Notes = "Proposal sent.",
                CreatedBy = sales2.Id, AssignedTo = sales2.Id, CreatedDate = DateTime.UtcNow.AddDays(-6)
            };
            db.Opportunities.AddRange(o1, o2, o3);
            await db.SaveChangesAsync();

            db.FollowUps.AddRange(
                new FollowUp {
                    CustomerId = c1.Id, OpportunityId = o1.Id, Subject = "Contract sign-off call",
                    FollowUpDate = DateTime.Today.AddDays(2), FollowUpType = "Call", Status = "Planned",
                    Remarks = "Confirm final seat count.", CreatedBy = sales.Id, AssignedTo = sales.Id
                },
                new FollowUp {
                    LeadId = l1.Id, Subject = "Qualification check-in",
                    FollowUpDate = DateTime.Today.AddDays(-1), FollowUpType = "Meeting", Status = "Planned",
                    Remarks = "Overdue follow-up for demo feedback.", CreatedBy = manager.Id, AssignedTo = sales.Id
                },
                new FollowUp {
                    CustomerId = c3.Id, Subject = "Proposal review",
                    FollowUpDate = DateTime.Today.AddDays(4), FollowUpType = "Email", Status = "Planned",
                    Remarks = "Follow up on sent quote.", CreatedBy = sales2.Id, AssignedTo = sales2.Id
                }
            );

            db.Activities.AddRange(
                new Activity {
                    ActivityType = "Meeting", Subject = "Executive Architecture Workshop",
                    Description = "Presented CRM security and reporting capabilities to TechNova leadership.",
                    ActivityDate = DateTime.Today.AddDays(-3), CustomerId = c1.Id, Status = "Done",
                    CreatedBy = sales.Id, AssignedTo = sales.Id
                },
                new Activity {
                    ActivityType = "Call", Subject = "Discovery Call with FinBridge",
                    Description = "Discussed lead pipeline requirements.",
                    ActivityDate = DateTime.Today.AddDays(-1), LeadId = l2.Id, Status = "Done",
                    CreatedBy = sales.Id, AssignedTo = sales.Id
                }
            );

            db.AuditLogs.Add(new AuditLog {
                UserId = admin.Id, Action = "SeedInitialize", EntityName = "System",
                RecordId = "Init", NewValue = "Initial CRM baseline data seeded", IpAddress = "127.0.0.1"
            });
            await db.SaveChangesAsync();
        }
    }
}
