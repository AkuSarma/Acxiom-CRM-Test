namespace AcxiomCRM.Services;

public class AuditService(AppDb db, IHttpContextAccessor ctx) {
    static string Ser(object o) => o == null ? null : JsonSerializer.Serialize(o, new JsonSerializerOptions {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    });

    public async Task LogAsync(string action, string entity, string recordId = null, object oldV = null, object newV = null, string userId = null) {
        var http = ctx.HttpContext;
        userId ??= http?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        db.AuditLogs.Add(new AuditLog {
            UserId = userId,
            Action = action,
            EntityName = entity,
            RecordId = recordId,
            OldValue = Ser(oldV),
            NewValue = Ser(newV),
            IpAddress = http?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync();
    }
}
