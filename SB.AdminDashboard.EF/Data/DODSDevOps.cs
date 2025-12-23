using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SB.AdminDashboard.EF.Models;

namespace SB.AdminDashboard.EF.Data;

public partial class DODSDevOps : DbContext
{
    public DODSDevOps()
    {
    }

    public DODSDevOps(DbContextOptions<DODSDevOps> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminDashboardAudit> AdminDashboardAudits { get; set; }

    public virtual DbSet<AdminDashboardConfigAudit> AdminDashboardConfigAudits { get; set; }

    public virtual DbSet<AdminDashboardConfigMapping> AdminDashboardConfigMappings { get; set; }

    public virtual DbSet<AdminDashboardMapping> AdminDashboardMappings { get; set; }

    public virtual DbSet<Approval> Approvals { get; set; }

    public virtual DbSet<ApprovalLevel> ApprovalLevels { get; set; }

    public virtual DbSet<ApprovalObjectConfiguration> ApprovalObjectConfigurations { get; set; }

    public virtual DbSet<ApprovalStatus> ApprovalStatuses { get; set; }

    public virtual DbSet<ApprovingUser> ApprovingUsers { get; set; }

    public virtual DbSet<AuditApproval> AuditApprovals { get; set; }

    public virtual DbSet<AuditApprovalLevel> AuditApprovalLevels { get; set; }

    public virtual DbSet<AuditApprovalObjectConfiguration> AuditApprovalObjectConfigurations { get; set; }

    public virtual DbSet<AuditApprovalStatus> AuditApprovalStatuses { get; set; }

    public virtual DbSet<AuditApprovingUser> AuditApprovingUsers { get; set; }

    public virtual DbSet<AuditPostApprovalAction> AuditPostApprovalActions { get; set; }

    public virtual DbSet<AuditRequest> AuditRequests { get; set; }

    public virtual DbSet<AuditRequestStatus> AuditRequestStatuses { get; set; }

    public virtual DbSet<ConfigurationParameter> ConfigurationParameters { get; set; }

    public virtual DbSet<PostApprovalAction> PostApprovalActions { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestStatus> RequestStatuses { get; set; }

    public virtual DbSet<RequestView> RequestViews { get; set; }

    public virtual DbSet<SecurityPolicy> SecurityPolicies { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserRoleGroupMapping> UserRoleGroupMappings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
       => optionsBuilder.UseSqlServer("Name=DodsDevOpsConnectionString", o => o.UseCompatibilityLevel(110));


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminDashboardAudit>(entity =>
        {
            entity.ToTable("AdminDashboardAudit", "dbo");

            entity.HasIndex(e => new { e.Application, e.Configuration, e.LastUpdatedTime }, "UX_AdminDashboardAudit").IsUnique();

            entity.Property(e => e.ApiEndpoint)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Application)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Configuration)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdatedTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NewValue)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.OldValue)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.User)
                .HasMaxLength(260)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AdminDashboardConfigAudit>(entity =>
        {
            entity.ToTable("AdminDashboardConfigAudit", "dbo");

            entity.HasIndex(e => new { e.Application, e.Configuration, e.LastUpdatedTime }, "UX_AdminDashboardConfigAudit").IsUnique();

            entity.Property(e => e.ApiEndpoint)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Application)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Configuration)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdatedTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NewValue)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.OldValue)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.User)
                .HasMaxLength(260)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AdminDashboardConfigMapping>(entity =>
        {
            entity.ToTable("AdminDashboardConfigMapping", "dbo");

            entity.HasIndex(e => new { e.Application, e.Configuration, e.BusinessUnit }, "UX_AdminDashboardConfigMapping").IsUnique();

            entity.Property(e => e.ApiEndpoint)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Application)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("FO");
            entity.Property(e => e.Configuration)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.GetConditions)
                .HasMaxLength(260)
                .IsUnicode(false);
            entity.Property(e => e.Map)
                .HasMaxLength(260)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AdminDashboardMapping>(entity =>
        {
            entity.ToTable("AdminDashboardMapping", "dbo");

            entity.HasIndex(e => new { e.Application, e.Configuration, e.BusinessUnit }, "UX_AdminDashboardMapping").IsUnique();

            entity.Property(e => e.ApiEndpoint)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Application)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.BusinessUnit)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValue("FO");
            entity.Property(e => e.Configuration)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.GetConditions)
                .HasMaxLength(260)
                .IsUnicode(false);
            entity.Property(e => e.Map)
                .HasMaxLength(260)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity
                .HasKey(e => new { e.RequestId, e.UserId, e.LevelId })
                .HasName("PK_Approvals");

            entity
                .ToTable("Approvals", "dbo", tb => tb.HasTrigger("tr_Approvals_Audit"));

            entity.Property(e => e.Comments)
                .HasMaxLength(350)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Level).WithMany()
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approvals__Level__32AB8735");

            entity.HasOne(d => d.Request).WithMany()
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approvals__Reque__31B762FC");
        });

        modelBuilder.Entity<ApprovalLevel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Approval__3214EC0764223616");

            entity.ToTable("ApprovalLevels", "dbo", tb => tb.HasTrigger("tr_ApprovalLevels_Audit"));

            entity.Property(e => e.LevelName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ApprovalObjectConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Approval__3214EC073160C4F0");

            entity.ToTable("ApprovalObjectConfiguration", "dbo", tb => tb.HasTrigger("tr_ApprovalObjectConfiguration_Audit"));

            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ObjectName)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.PostApprovalAction).WithMany(p => p.ApprovalObjectConfigurations)
                .HasForeignKey(d => d.PostApprovalActionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ApprovalO__PostA__123EB7A3");
        });

        modelBuilder.Entity<ApprovalStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Approval__3214EC07015B66D9");

            entity.ToTable("ApprovalStatus", "dbo", tb => tb.HasTrigger("tr_ApprovalStatus_Audit"));

            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ApprovingUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ApprovingUsers", "dbo", tb => tb.HasTrigger("tr_ApprovingUsers_Audit"));

            entity.HasIndex(e => new { e.ConfigurationId, e.UserId, e.LevelId }, "UQ__Approvin__63DB2F4208C74A13").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Configuration).WithMany()
                .HasForeignKey(d => d.ConfigurationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approving__Confi__1EA48E88");

            entity.HasOne(d => d.Level).WithMany()
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Approving__Level__1F98B2C1");
        });

        modelBuilder.Entity<AuditApproval>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditApprovals", "dbo");

            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.Comments)
                .HasMaxLength(350)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuditApprovalLevel>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditApprovalLevels", "dbo");

            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.LevelName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuditApprovalObjectConfiguration>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditApprovalObjectConfiguration", "dbo");

            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ObjectName)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuditApprovalStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditApprovalStatus", "dbo");

            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuditApprovingUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditApprovingUsers", "dbo");

            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.UserId)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuditPostApprovalAction>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditPostApprovalAction", "dbo");

            entity.Property(e => e.Action)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.ExecutionType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Item)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AuditRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditRequests", "dbo");

            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.MetaDataKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ObjectName)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Operation)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OriginalData).IsUnicode(false);
            entity.Property(e => e.RequestingComments)
                .HasMaxLength(350)
                .IsUnicode(false);
            entity.Property(e => e.RequestingUserId)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedData).IsUnicode(false);
        });

        modelBuilder.Entity<AuditRequestStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuditRequestStatus", "dbo");

            entity.Property(e => e.AuditOperation)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.AuditTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuditUser)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("(suser_sname())");
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ConfigurationParameter>(entity =>
        {
            entity.HasKey(e => new { e.Group, e.Name }).HasName("PK_ConfirgurationParameters");

            entity.ToTable("ConfigurationParameters", "dbo");

            entity.Property(e => e.Group)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Application)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Value)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PostApprovalAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PostAppr__3214EC07810E383F");

            entity.ToTable("PostApprovalAction", "dbo", tb => tb.HasTrigger("tr_PostApprovalAction_Audit"));

            entity.Property(e => e.Action)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ExecutionType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Item)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Requests__33A8517A5CA0920A");

            entity.ToTable("Requests", "dbo", tb => tb.HasTrigger("tr_Requests_Audit"));

            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.MetaDataKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ObjectName)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Operation)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OriginalData).IsUnicode(false);
            entity.Property(e => e.RequestingComments)
                .HasMaxLength(350)
                .IsUnicode(false);
            entity.Property(e => e.RequestingUserId)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedData).IsUnicode(false);

            entity.HasOne(d => d.Configuration).WithMany(p => p.Requests)
                .HasForeignKey(d => d.ConfigurationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Requests__Config__2BFE89A6");
        });

        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.StatusId })
                .HasName("PK_RequestStatus");

            entity.ToTable("RequestStatus", "dbo", tb => tb.HasTrigger("tr_RequestStatus_Audit"));

            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.Request).WithMany()
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RequestSt__Reque__3864608B");

            entity.HasOne(d => d.Status).WithMany()
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RequestSt__Statu__395884C4");
        });

        modelBuilder.Entity<RequestView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("RequestView", "dbo");

            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ObjectName)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Operation)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OriginalData).IsUnicode(false);
            entity.Property(e => e.PostApprovalAction)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.RequestingComments)
                .HasMaxLength(350)
                .IsUnicode(false);
            entity.Property(e => e.RequestingUser)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedData).IsUnicode(false);
        });

        modelBuilder.Entity<SecurityPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("PK_Table1");

            entity.ToTable("SecurityPolicies", "dbo");

            entity.HasIndex(e => new { e.Role, e.App, e.Policy }, "RolePolicyAppMap").IsUnique();

            entity.Property(e => e.App)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdatedTime).HasColumnType("datetime");
            entity.Property(e => e.Policy)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(128)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles", "dbo");

            entity.HasIndex(e => new { e.Role, e.Group }, "UK_UserRoles_Name_Group").IsUnique();

            entity.Property(e => e.Application)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Group)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UserRoleGroupMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserRole__3214EC27E6BEFD80");

            entity.ToTable("UserRoleGroupMapping", "dbo");

            entity.HasIndex(e => new { e.Role, e.Group, e.Application }, "UK_UserRoleGroupMapping").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Application)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Group)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
