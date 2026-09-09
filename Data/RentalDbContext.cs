using Microsoft.EntityFrameworkCore;
using RentalApp.Domain.Entities;

namespace RentalApp.Data;

public class RentalDbContext(DbContextOptions<RentalDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<UtilityType> UtilityTypes => Set<UtilityType>();
    public DbSet<UtilityCustomer> UtilityCustomers => Set<UtilityCustomer>();
    public DbSet<UtilityBill> UtilityBills => Set<UtilityBill>();
    public DbSet<UtilityBillPayment> UtilityBillPayments => Set<UtilityBillPayment>();
    public DbSet<UtilityCustomerCredit> UtilityCustomerCredits => Set<UtilityCustomerCredit>();
    public DbSet<UtilityAuditLog> UtilityAuditLogs => Set<UtilityAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Username).IsUnique();

            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("Properties");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name);

            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("Units");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PropertyId, x.UnitNumber }).IsUnique();
            entity.HasIndex(x => x.Status);

            entity.Property(x => x.UnitNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.MonthlyRent).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Property)
                .WithMany(x => x.Units)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Rooms");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UnitId, x.RoomNumber }).IsUnique();

            entity.Property(x => x.RoomNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.MaxCapacity).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Unit)
                .WithMany(x => x.Rooms)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.FirstName, x.LastName });

            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ContactNumber).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.UnitId);
            entity.Property(x => x.UnitNumber).HasMaxLength(50);
            entity.Property(x => x.RoomNumber).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.ToTable("ExpenseCategories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();

            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.IsActive).IsRequired();
        });

        modelBuilder.Entity<Lease>(entity =>
        {
            entity.ToTable("Leases");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UnitId, x.Status });
            entity.HasIndex(x => new { x.RoomId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.Status });

            entity.Property(x => x.StartDate).IsRequired();
            entity.Property(x => x.MonthlyRent).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.SecurityDeposit).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.DueDayOfMonth).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Unit)
                .WithMany(x => x.Leases)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Room)
                .WithMany(x => x.Leases)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Leases)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.LeaseId);
            entity.HasIndex(x => x.PaymentDate);

            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.PaymentDate).IsRequired();
            entity.Property(x => x.PaymentType).IsRequired();
            entity.Property(x => x.PaymentMethod).IsRequired();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasOne(x => x.Lease)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.LeaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Unit)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expenses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExpenseDate);
            entity.HasIndex(x => x.PropertyId);
            entity.HasIndex(x => x.CategoryId);

            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.ExpenseDate).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Property)
                .WithMany(x => x.Expenses)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Unit)
                .WithMany(x => x.Expenses)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Expenses)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
            entity.HasIndex(x => x.InvoiceDate);
            entity.HasIndex(x => x.DueDate);
            entity.HasIndex(x => x.Status);

            entity.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.InvoiceDate).IsRequired();
            entity.Property(x => x.DueDate).IsRequired();
            entity.Property(x => x.Subtotal).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.Total).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Invoices)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Lease)
                .WithMany(x => x.Invoices)
                .HasForeignKey(x => x.LeaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("InvoiceItems");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InvoiceId);

            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();

            entity.HasOne(x => x.Invoice)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UtilityType>(entity =>
        {
            entity.ToTable("UtilityTypes");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();

            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            entity.Property(x => x.DefaultRate).HasColumnType("decimal(18,4)");
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.ToTable(t => t.HasCheckConstraint("CK_UtilityTypes_DefaultRate_NonNegative", "DefaultRate IS NULL OR DefaultRate >= 0"));
        });

        modelBuilder.Entity<UtilityCustomer>(entity =>
        {
            entity.ToTable("UtilityCustomers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.CustomerType);
            entity.HasIndex(x => x.UtilityCategoryId);

            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CustomerType).IsRequired();
            entity.Property(x => x.DueDateRuleType).IsRequired();
            entity.Property(x => x.UtilityStartDate);
            entity.Property(x => x.DueDayOfMonth);
            entity.Property(x => x.DueInDays);
            entity.Property(x => x.DefaultRate).HasColumnType("decimal(18,4)");
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.Room)
                .WithMany()
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasOne(x => x.UtilityCategory)
                .WithMany()
                .HasForeignKey(x => x.UtilityCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_UtilityCustomers_DefaultRate_NonNegative", "DefaultRate IS NULL OR DefaultRate >= 0");
                t.HasCheckConstraint("CK_UtilityCustomers_DueDay_Range", "DueDayOfMonth IS NULL OR (DueDayOfMonth >= 1 AND DueDayOfMonth <= 28)");
                t.HasCheckConstraint("CK_UtilityCustomers_DueInDays_NonNegative", "DueInDays IS NULL OR DueInDays >= 0");
            });
        });


        modelBuilder.Entity<UtilityBill>(entity =>
        {
            entity.ToTable("UtilityBills");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UtilityCustomerId, x.UtilityTypeId, x.BillingPeriod }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.DueDate);

            entity.Property(x => x.BillingPeriod).HasMaxLength(7).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.DueDate);
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.Version).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.UtilityCustomer)
                .WithMany(x => x.Bills)
                .HasForeignKey(x => x.UtilityCustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UtilityType)
                .WithMany(x => x.Bills)
                .HasForeignKey(x => x.UtilityTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_UtilityBills_Amount_NonNegative", "Amount >= 0");
                t.HasCheckConstraint("CK_UtilityBills_BillingPeriod_Length", "LEN(BillingPeriod) = 7");
            });
        });

        modelBuilder.Entity<UtilityBillPayment>(entity =>
        {
            entity.ToTable("UtilityBillPayments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UtilityBillId);
            entity.HasIndex(x => x.PaymentDate);

            entity.Property(x => x.PaymentDate).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.IsVoided).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasOne(x => x.UtilityBill)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.UtilityBillId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_UtilityBillPayments_Amount_Positive", "Amount > 0"));
        });

        modelBuilder.Entity<UtilityCustomerCredit>(entity =>
        {
            entity.ToTable("UtilityCustomerCredits");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UtilityCustomerId, x.UtilityTypeId, x.OccurredAt });

            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(x => x.TransactionType).IsRequired();
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);

            entity.HasOne(x => x.UtilityCustomer)
                .WithMany(x => x.Credits)
                .HasForeignKey(x => x.UtilityCustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UtilityType)
                .WithMany(x => x.CustomerCredits)
                .HasForeignKey(x => x.UtilityTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SourcePayment)
                .WithMany(x => x.Credits)
                .HasForeignKey(x => x.SourcePaymentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });


        modelBuilder.Entity<UtilityAuditLog>(entity =>
        {
            entity.ToTable("UtilityAuditLogs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EntityName, x.EntityId });
            entity.HasIndex(x => x.CorrelationId);

            entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).IsRequired();
            entity.Property(x => x.PerformedBy).HasMaxLength(100);
            entity.Property(x => x.PerformedAt).IsRequired();
        });


    }
}
