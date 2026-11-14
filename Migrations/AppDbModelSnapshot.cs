using System;
using AcxiomCRM.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AcxiomCRM.Migrations {
    [DbContext(typeof(AppDb))]
    partial class AppDbModelSnapshot : ModelSnapshot {
        protected override void BuildModel(ModelBuilder modelBuilder) {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("AcxiomCRM.Models.Activity", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<DateTime>("ActivityDate").HasColumnType("TEXT");
                b.Property<string>("ActivityType").IsRequired().HasColumnType("TEXT");
                b.Property<string>("AssignedTo").HasColumnType("TEXT");
                b.Property<string>("CreatedBy").HasColumnType("TEXT");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<int?>("CustomerId").HasColumnType("INTEGER");
                b.Property<string>("Description").HasMaxLength(500).HasColumnType("TEXT");
                b.Property<int?>("LeadId").HasColumnType("INTEGER");
                b.Property<DateTime?>("ModifiedDate").HasColumnType("TEXT");
                b.Property<string>("Status").IsRequired().HasColumnType("TEXT");
                b.Property<string>("Subject").IsRequired().HasMaxLength(150).HasColumnType("TEXT");
                b.HasKey("Id");
                b.ToTable("Activities");
            });

            modelBuilder.Entity("AcxiomCRM.Models.AppUser", b => {
                b.Property<string>("Id").HasColumnType("TEXT");
                b.Property<int>("AccessFailedCount").HasColumnType("INTEGER");
                b.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("TEXT");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<string>("Email").HasMaxLength(256).HasColumnType("TEXT");
                b.Property<bool>("EmailConfirmed").HasColumnType("INTEGER");
                b.Property<string>("FullName").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
                b.Property<bool>("IsActive").HasColumnType("INTEGER");
                b.Property<bool>("LockoutEnabled").HasColumnType("INTEGER");
                b.Property<DateTimeOffset?>("LockoutEnd").HasColumnType("TEXT");
                b.Property<string>("ManagerId").HasColumnType("TEXT");
                b.Property<string>("NormalizedEmail").HasMaxLength(256).HasColumnType("TEXT");
                b.Property<string>("NormalizedUserName").HasMaxLength(256).HasColumnType("TEXT");
                b.Property<string>("PasswordHash").HasColumnType("TEXT");
                b.Property<string>("PhoneNumber").HasColumnType("TEXT");
                b.Property<bool>("PhoneNumberConfirmed").HasColumnType("INTEGER");
                b.Property<string>("SecurityStamp").HasColumnType("TEXT");
                b.Property<string>("TeamName").HasMaxLength(80).HasColumnType("TEXT");
                b.Property<bool>("TwoFactorEnabled").HasColumnType("INTEGER");
                b.Property<string>("UserName").HasMaxLength(256).HasColumnType("TEXT");
                b.HasKey("Id");
                b.HasIndex("NormalizedEmail").HasDatabaseName("EmailIndex");
                b.HasIndex("NormalizedUserName").IsUnique().HasDatabaseName("UserNameIndex");
                b.ToTable("AspNetUsers");
            });

            modelBuilder.Entity("AcxiomCRM.Models.AuditLog", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<string>("Action").HasColumnType("TEXT");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<string>("EntityName").HasColumnType("TEXT");
                b.Property<string>("IpAddress").HasColumnType("TEXT");
                b.Property<string>("NewValue").HasColumnType("TEXT");
                b.Property<string>("OldValue").HasColumnType("TEXT");
                b.Property<string>("RecordId").HasColumnType("TEXT");
                b.Property<string>("UserId").HasColumnType("TEXT");
                b.HasKey("Id");
                b.ToTable("AuditLogs");
            });

            modelBuilder.Entity("AcxiomCRM.Models.Customer", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<string>("Address").HasMaxLength(250).HasColumnType("TEXT");
                b.Property<string>("AssignedTo").HasColumnType("TEXT");
                b.Property<string>("City").HasMaxLength(60).HasColumnType("TEXT");
                b.Property<string>("CompanyName").HasMaxLength(100).HasColumnType("TEXT");
                b.Property<string>("CreatedBy").HasColumnType("TEXT");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<string>("CustomerCode").HasColumnType("TEXT");
                b.Property<string>("CustomerName").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
                b.Property<string>("Email").IsRequired().HasMaxLength(150).HasColumnType("TEXT");
                b.Property<DateTime?>("ModifiedDate").HasColumnType("TEXT");
                b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("TEXT");
                b.Property<string>("Phone").IsRequired().HasColumnType("TEXT");
                b.Property<string>("State").HasMaxLength(60).HasColumnType("TEXT");
                b.Property<string>("Status").IsRequired().HasColumnType("TEXT");
                b.HasKey("Id");
                b.HasIndex("Email").IsUnique();
                b.HasIndex("Phone").IsUnique();
                b.ToTable("Customers");
            });

            modelBuilder.Entity("AcxiomCRM.Models.FollowUp", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<string>("AssignedTo").HasColumnType("TEXT");
                b.Property<string>("CreatedBy").HasColumnType("TEXT");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<int?>("CustomerId").HasColumnType("INTEGER");
                b.Property<DateTime>("FollowUpDate").HasColumnType("TEXT");
                b.Property<string>("FollowUpType").IsRequired().HasColumnType("TEXT");
                b.Property<int?>("LeadId").HasColumnType("INTEGER");
                b.Property<DateTime?>("ModifiedDate").HasColumnType("TEXT");
                b.Property<int?>("OpportunityId").HasColumnType("INTEGER");
                b.Property<string>("Remarks").HasMaxLength(500).HasColumnType("TEXT");
                b.Property<string>("Status").IsRequired().HasColumnType("TEXT");
                b.Property<string>("Subject").IsRequired().HasMaxLength(150).HasColumnType("TEXT");
                b.HasKey("Id");
                b.ToTable("FollowUps");
            });

            modelBuilder.Entity("AcxiomCRM.Models.Lead", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<string>("AssignedTo").HasColumnType("TEXT");
                b.Property<string>("CompanyName").HasMaxLength(100).HasColumnType("TEXT");
                b.Property<string>("CreatedBy").HasColumnType("TEXT");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<string>("Email").HasMaxLength(150).HasColumnType("TEXT");
                b.Property<double>("ExpectedValue").HasColumnType("REAL");
                b.Property<string>("LeadCode").HasColumnType("TEXT");
                b.Property<string>("LeadName").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
                b.Property<DateTime?>("ModifiedDate").HasColumnType("TEXT");
                b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("TEXT");
                b.Property<string>("Phone").HasColumnType("TEXT");
                b.Property<string>("Priority").IsRequired().HasColumnType("TEXT");
                b.Property<string>("Source").HasMaxLength(60).HasColumnType("TEXT");
                b.Property<string>("Status").IsRequired().HasColumnType("TEXT");
                b.HasKey("Id");
                b.ToTable("Leads");
            });

            modelBuilder.Entity("AcxiomCRM.Models.Opportunity", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<double>("Amount").HasColumnType("REAL");
                b.Property<string>("AssignedTo").HasColumnType("TEXT");
                b.Property<string>("CreatedBy").HasColumnType("TEXT");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<int?>("CustomerId").IsRequired().HasColumnType("INTEGER");
                b.Property<DateTime>("ExpectedCloseDate").HasColumnType("TEXT");
                b.Property<int?>("LeadId").HasColumnType("INTEGER");
                b.Property<DateTime?>("ModifiedDate").HasColumnType("TEXT");
                b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("TEXT");
                b.Property<string>("OpportunityName").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
                b.Property<int>("Probability").HasColumnType("INTEGER");
                b.Property<string>("Source").HasMaxLength(60).HasColumnType("TEXT");
                b.Property<string>("Stage").IsRequired().HasColumnType("TEXT");
                b.Property<string>("Status").IsRequired().HasColumnType("TEXT");
                b.HasKey("Id");
                b.ToTable("Opportunities");
            });

            modelBuilder.Entity("AcxiomCRM.Models.PasswordHistory", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<DateTime>("CreatedDate").HasColumnType("TEXT");
                b.Property<string>("PasswordHash").IsRequired().HasColumnType("TEXT");
                b.Property<string>("UserId").IsRequired().HasColumnType("TEXT");
                b.HasKey("Id");
                b.HasIndex("UserId");
                b.ToTable("PasswordHistories");
            });

            modelBuilder.Entity("AcxiomCRM.Models.RolePermission", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<bool>("CanAssign").HasColumnType("INTEGER");
                b.Property<bool>("CanCreate").HasColumnType("INTEGER");
                b.Property<bool>("CanDelete").HasColumnType("INTEGER");
                b.Property<bool>("CanEdit").HasColumnType("INTEGER");
                b.Property<bool>("CanView").HasColumnType("INTEGER");
                b.Property<string>("Module").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
                b.Property<string>("RoleName").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
                b.HasKey("Id");
                b.HasIndex("RoleName", "Module").IsUnique();
                b.ToTable("RolePermissions");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b => {
                b.Property<string>("Id").HasColumnType("TEXT");
                b.Property<string>("ConcurrencyStamp").IsConcurrencyToken().HasColumnType("TEXT");
                b.Property<string>("Name").HasMaxLength(256).HasColumnType("TEXT");
                b.Property<string>("NormalizedName").HasMaxLength(256).HasColumnType("TEXT");
                b.HasKey("Id");
                b.HasIndex("NormalizedName").IsUnique().HasDatabaseName("RoleNameIndex");
                b.ToTable("AspNetRoles");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<string>("ClaimType").HasColumnType("TEXT");
                b.Property<string>("ClaimValue").HasColumnType("TEXT");
                b.Property<string>("RoleId").IsRequired().HasColumnType("TEXT");
                b.HasKey("Id");
                b.HasIndex("RoleId");
                b.ToTable("AspNetRoleClaims");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                b.Property<string>("ClaimType").HasColumnType("TEXT");
                b.Property<string>("ClaimValue").HasColumnType("TEXT");
                b.Property<string>("UserId").IsRequired().HasColumnType("TEXT");
                b.HasKey("Id");
                b.HasIndex("UserId");
                b.ToTable("AspNetUserClaims");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b => {
                b.Property<string>("LoginProvider").HasColumnType("TEXT");
                b.Property<string>("ProviderKey").HasColumnType("TEXT");
                b.Property<string>("ProviderDisplayName").HasColumnType("TEXT");
                b.Property<string>("UserId").IsRequired().HasColumnType("TEXT");
                b.HasKey("LoginProvider", "ProviderKey");
                b.HasIndex("UserId");
                b.ToTable("AspNetUserLogins");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b => {
                b.Property<string>("UserId").HasColumnType("TEXT");
                b.Property<string>("RoleId").HasColumnType("TEXT");
                b.HasKey("UserId", "RoleId");
                b.HasIndex("RoleId");
                b.ToTable("AspNetUserRoles");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b => {
                b.Property<string>("UserId").HasColumnType("TEXT");
                b.Property<string>("LoginProvider").HasColumnType("TEXT");
                b.Property<string>("Name").HasColumnType("TEXT");
                b.Property<string>("Value").HasColumnType("TEXT");
                b.HasKey("UserId", "LoginProvider", "Name");
                b.ToTable("AspNetUserTokens");
            });
        }
    }
}
