namespace AcxiomCRM.Models;

[AttributeUsage(AttributeTargets.Property)]
public class NotPastDateAttribute : ValidationAttribute, IClientModelValidator {
    public NotPastDateAttribute() {
        ErrorMessage = "Date cannot be earlier than today.";
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
        if (value is DateTime dt && dt.Date < DateTime.Today) {
            var members = validationContext?.MemberName != null ? new[] { validationContext.MemberName } : null;
            return new ValidationResult(ErrorMessage ?? "Date cannot be earlier than today.", members);
        }
        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context) {
        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-notpastdate"] = ErrorMessage ?? "Date cannot be earlier than today.";
        context.Attributes["min"] = DateTime.Today.ToString("yyyy-MM-dd");
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class PositiveAmountAttribute : ValidationAttribute, IClientModelValidator {
    public PositiveAmountAttribute() {
        ErrorMessage = "Opportunity Amount must be greater than 0.";
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
        if (value is double d) {
            var members = validationContext?.MemberName != null ? new[] { validationContext.MemberName } : null;
            if (validationContext?.ObjectInstance is Opportunity opp && (opp.Stage == "Won" || opp.Stage == "Lost")) {
                if (d < 0)
                    return new ValidationResult(ErrorMessage ?? "Opportunity Amount cannot be negative.", members);
                return ValidationResult.Success;
            }
            if (d <= 0) {
                return new ValidationResult(ErrorMessage ?? "Opportunity Amount must be greater than 0.", members);
            }
        }
        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context) {
        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-positiveamount"] = ErrorMessage ?? "Opportunity Amount must be greater than 0.";
    }
}
