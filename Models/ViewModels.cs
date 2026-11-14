namespace AcxiomCRM.Models;

public class LoginVm {
    [Required, EmailAddress] public string Email { get; set; }
    [Required, DataType(DataType.Password)] public string Password { get; set; }
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
}

public class RegisterVm {
    [Required, StringLength(100), Display(Name = "Full name")] public string FullName { get; set; }
    [Required, EmailAddress] public string Email { get; set; }
    [Required, DataType(DataType.Password)] public string Password { get; set; }
    [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match."), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; }
}

public class ChangePasswordVm {
    [Required, DataType(DataType.Password), Display(Name = "Current password")] public string Current { get; set; }
    [Required, DataType(DataType.Password), Display(Name = "New password")] public string New { get; set; }
    [Required, DataType(DataType.Password), Compare("New", ErrorMessage = "Passwords do not match."), Display(Name = "Confirm new password")] public string Confirm { get; set; }
}

public class DashVm {
    public Dictionary<string, string> Cards { get; set; } = new();
    public Dictionary<string, int> LeadStatus { get; set; } = new();
    public Dictionary<string, int> Stages { get; set; } = new();
    public Dictionary<string, double> Monthly { get; set; } = new();
    public string ScopeLabel { get; set; } = "";
}

public class ReportVm {
    public string Name { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string SortBy { get; set; }
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int Pages { get; set; } = 1;
    public int TotalRows { get; set; }
}

public class UserRow {
    public AppUser User { get; set; }
    public string Role { get; set; }
    public bool Locked { get; set; }
    public string ManagerName { get; set; }
}

public class RoleSummaryVm {
    public string RoleName { get; set; }
    public int UserCount { get; set; }
    public List<string> Users { get; set; } = new();
}

public class RolesPermissionsVm {
    public List<RoleSummaryVm> Roles { get; set; } = new();
    public List<RolePermission> Permissions { get; set; } = new();
}

public class CustomerDetailsVm {
    public Customer Customer { get; set; }
    public string AssignedToName { get; set; }
    public string CreatedByName { get; set; }
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
    public Dictionary<string, string> UserNames { get; set; } = new();
}
