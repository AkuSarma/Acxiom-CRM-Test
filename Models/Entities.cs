namespace AcxiomCRM.Models;

public static class Roles {
    public const string Admin = "Admin", Manager = "Manager", Sales = "SalesExecutive";
    public static readonly string[] All = { Admin, Manager, Sales };
}

public static class Modules {
    public static readonly string[] All = {
        "Dashboard", "Customers", "Leads", "Opportunities", "FollowUps",
        "Activities", "Users", "Roles", "AuditLog", "RestApi", "Reports"
    };
}

public class AppUser : IdentityUser {
    [Required, StringLength(100)]
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string ManagerId { get; set; }
    [StringLength(80)]
    public string TeamName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class PasswordHistory {
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; }
    [Required]
    public string PasswordHash { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class RolePermission {
    public int Id { get; set; }
    [Required, StringLength(50)]
    public string RoleName { get; set; }
    [Required, StringLength(50)]
    public string Module { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanAssign { get; set; }
}

public abstract class EntityBase {
    [ScaffoldColumn(false)] public int Id { get; set; }
    [ScaffoldColumn(false)] public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [ScaffoldColumn(false)] public DateTime? ModifiedDate { get; set; }
    [ScaffoldColumn(false)] public string CreatedBy { get; set; }
    [Display(Name = "Assigned To")] public string AssignedTo { get; set; }
}

public class Customer : EntityBase {
    [ScaffoldColumn(false)] public string CustomerCode { get; set; }
    [Required(ErrorMessage = "Customer Name is required."), StringLength(100), Display(Name = "Customer Name")]
    public string CustomerName { get; set; }
    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)]
    public string Email { get; set; }
    [Required(ErrorMessage = "Phone is required."), RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; }
    [StringLength(100), Display(Name = "Company")]
    public string CompanyName { get; set; }
    [StringLength(250)]
    public string Address { get; set; }
    [StringLength(60)]
    public string City { get; set; }
    [StringLength(60)]
    public string State { get; set; }
    [Required]
    public string Status { get; set; } = "Active";
    [DataType(DataType.MultilineText), StringLength(1000)]
    public string Notes { get; set; }
}

public class Lead : EntityBase {
    [ScaffoldColumn(false)] public string LeadCode { get; set; }
    [Required(ErrorMessage = "Lead name is required."), StringLength(100), Display(Name = "Lead Name")]
    public string LeadName { get; set; }
    [EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)]
    public string Email { get; set; }
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; }
    [StringLength(100), Display(Name = "Company")]
    public string CompanyName { get; set; }
    [StringLength(60)]
    public string Source { get; set; }
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "New";
    [Required(ErrorMessage = "Priority is required.")]
    public string Priority { get; set; } = "Medium";
    [Range(0, 100000000, ErrorMessage = "Expected value is out of range."), Display(Name = "Expected Value")]
    public double ExpectedValue { get; set; }
    [DataType(DataType.MultilineText), StringLength(1000)]
    public string Notes { get; set; }
}

public class Opportunity : EntityBase, IValidatableObject {
    [Required(ErrorMessage = "Name is required."), StringLength(100), Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; }
    [Required(ErrorMessage = "Customer is required."), Display(Name = "Customer")]
    public int? CustomerId { get; set; }
    [Display(Name = "Lead")]
    public int? LeadId { get; set; }
    [PositiveAmount(ErrorMessage = "Opportunity Amount must be greater than 0."), Range(0, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public double Amount { get; set; }
    [Required]
    public string Stage { get; set; } = "Qualification";
    [Required]
    public string Status { get; set; } = "Open";
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; }
    [Required(ErrorMessage = "Expected Close Date is required."), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
    [StringLength(60)]
    public string Source { get; set; }
    [DataType(DataType.MultilineText), StringLength(1000)]
    public string Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext c) {
        if (Stage == "Won") Status = "Won";
        else if (Stage == "Lost") Status = "Lost";

        bool active = Stage != "Won" && Stage != "Lost" && Status == "Open";
        if (Amount < 0 || (active && Amount <= 0))
            yield return new("Opportunity Amount must be greater than 0.", new[] { nameof(Amount) });
        if (active && ExpectedCloseDate.Date < DateTime.Today)
            yield return new("Expected Close Date cannot be in the past.", new[] { nameof(ExpectedCloseDate) });
    }
}

public class FollowUp : EntityBase {
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }
    [Display(Name = "Lead")]
    public int? LeadId { get; set; }
    [Display(Name = "Opportunity")]
    public int? OpportunityId { get; set; }
    [Required(ErrorMessage = "Subject is required."), StringLength(150)]
    public string Subject { get; set; }
    [Required(ErrorMessage = "Follow-up date is required."), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Display(Name = "Follow-Up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;
    [Required, Display(Name = "Type")]
    public string FollowUpType { get; set; } = "Call";
    [Required]
    public string Status { get; set; } = "Planned";
    [DataType(DataType.MultilineText), StringLength(500)]
    public string Remarks { get; set; }
}

public class Activity : EntityBase {
    [Required, Display(Name = "Type")]
    public string ActivityType { get; set; } = "Call";
    [Required(ErrorMessage = "Subject is required."), StringLength(150)]
    public string Subject { get; set; }
    [DataType(DataType.MultilineText), StringLength(500)]
    public string Description { get; set; }
    [Required(ErrorMessage = "Date is required."), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.Today;
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }
    [Display(Name = "Lead")]
    public int? LeadId { get; set; }
    [Required]
    public string Status { get; set; } = "Open";
}

public class AuditLog {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public string EntityName { get; set; }
    public string RecordId { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; }
}
