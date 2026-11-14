using System;
using AcxiomCRM.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcxiomCRM.Migrations {
    [DbContext(typeof(AppDb))]
    [Migration("20261114000000_InitialCreate")]
    public partial class InitialCreate : Migration {
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_AspNetRoles", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManagerId = table.Column<string>(type: "TEXT", nullable: true),
                    TeamName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_AspNetUsers", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ActivityType = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ActivityDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Activities", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: true),
                    EntityName = table.Column<string>(type: "TEXT", nullable: true),
                    RecordId = table.Column<string>(type: "TEXT", nullable: true),
                    OldValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_AuditLogs", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    CustomerCode = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Customers", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "FollowUps",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpportunityId = table.Column<int>(type: "INTEGER", nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    FollowUpDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FollowUpType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_FollowUps", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    LeadCode = table.Column<string>(type: "TEXT", nullable: true),
                    LeadName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedValue = table.Column<double>(type: "REAL", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Leads", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    OpportunityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    Stage = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Probability = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedCloseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Opportunities", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_PasswordHistories", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Module = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CanView = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanCreate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEdit = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanAssign = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_RolePermissions", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_AspNetRoleClaims_RoleId", "AspNetRoleClaims", "RoleId");
            migrationBuilder.CreateIndex("RoleNameIndex", "AspNetRoles", "NormalizedName", unique: true);
            migrationBuilder.CreateIndex("IX_AspNetUserClaims_UserId", "AspNetUserClaims", "UserId");
            migrationBuilder.CreateIndex("IX_AspNetUserLogins_UserId", "AspNetUserLogins", "UserId");
            migrationBuilder.CreateIndex("IX_AspNetUserRoles_RoleId", "AspNetUserRoles", "RoleId");
            migrationBuilder.CreateIndex("EmailIndex", "AspNetUsers", "NormalizedEmail");
            migrationBuilder.CreateIndex("UserNameIndex", "AspNetUsers", "NormalizedUserName", unique: true);
            migrationBuilder.CreateIndex("IX_Customers_Email", "Customers", "Email", unique: true);
            migrationBuilder.CreateIndex("IX_Customers_Phone", "Customers", "Phone", unique: true);
            migrationBuilder.CreateIndex("IX_PasswordHistories_UserId", "PasswordHistories", "UserId");
            migrationBuilder.CreateIndex("IX_RolePermissions_RoleName_Module", "RolePermissions", new[] { "RoleName", "Module" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable("Activities");
            migrationBuilder.DropTable("AspNetRoleClaims");
            migrationBuilder.DropTable("AspNetUserClaims");
            migrationBuilder.DropTable("AspNetUserLogins");
            migrationBuilder.DropTable("AspNetUserRoles");
            migrationBuilder.DropTable("AspNetUserTokens");
            migrationBuilder.DropTable("AuditLogs");
            migrationBuilder.DropTable("Customers");
            migrationBuilder.DropTable("FollowUps");
            migrationBuilder.DropTable("Leads");
            migrationBuilder.DropTable("Opportunities");
            migrationBuilder.DropTable("PasswordHistories");
            migrationBuilder.DropTable("RolePermissions");
            migrationBuilder.DropTable("AspNetRoles");
            migrationBuilder.DropTable("AspNetUsers");
        }
    }
}
