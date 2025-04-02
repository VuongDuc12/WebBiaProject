using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Models;

namespace WebBiaProject.Data
{
    public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<BilliardTable> BilliardTables { get; set; }
        public virtual DbSet<Branch> Branches { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<EmployeeRole> EmployeeRoles { get; set; }
        public virtual DbSet<Invoice> Invoices { get; set; }
        public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public virtual DbSet<MembershipLevel> MembershipLevels { get; set; }
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductCategory> ProductCategories { get; set; }
        public virtual DbSet<Reservation> Reservations { get; set; }
        public virtual DbSet<ServiceProductDetail> ServiceProductDetails { get; set; }
        public virtual DbSet<TableStatus> TableStatuses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer("Name=DefaultConnection");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // QUAN TRỌNG!!!

            modelBuilder.Entity<BilliardTable>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Billiard__3214EC077260D2FA");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.Branch).WithMany(p => p.BilliardTables)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BilliardTable_Branch");

                entity.HasOne(d => d.Status).WithMany(p => p.BilliardTables)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BilliardTable_TableStatus");
            });

            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Branch__3214EC073FF21966");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC071D628847");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.MembershipLevelId).HasDefaultValue(1);

                entity.HasOne(d => d.MembershipLevel).WithMany(p => p.Customers)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Customer_MembershipLevel");

                entity.HasOne(d => d.User).WithMany(p => p.Customers)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Customer_AspNetUsers");
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Employee__3214EC07CB7FCFAC");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.Branch).WithMany(p => p.Employees).HasConstraintName("FK_Employee_Branch");

                entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Employee_EmployeeRole");

                entity.HasOne(d => d.User).WithMany(p => p.Employees)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Employee_AspNetUsers");
            });

            modelBuilder.Entity<EmployeeRole>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Employee__3214EC07F70E77B3");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Invoice__3214EC07B26A444E");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

                // Cấu hình TableId (bắt buộc)
                entity.Property(e => e.TableId).IsRequired();

                // Cấu hình CustomerName và CustomerPhone (không bắt buộc)
                entity.Property(e => e.CustomerName)
                    .HasMaxLength(100)
                    .IsUnicode(true);

                entity.Property(e => e.CustomerPhone)
                    .HasMaxLength(20)
                    .IsUnicode(true);

                // Quan hệ với Branch
                entity.HasOne(d => d.Branch).WithMany(p => p.Invoices)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Invoice_Branch");

                // Quan hệ với Customer (nullable)
                entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Invoice_Customer");

                // Quan hệ với PaymentMethod
                entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Invoices)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Invoice_PaymentMethod");

                // Quan hệ với BilliardTable
                entity.HasOne(d => d.Table).WithMany(p => p.Invoices)
                    .HasForeignKey(d => d.TableId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Invoice_BilliardTable");
            });

            modelBuilder.Entity<InvoiceDetail>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__InvoiceD__3214EC077A680323");

                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

                // Cấu hình TimeInput và TimeOutput (không bắt buộc)
                entity.Property(e => e.TimeInput)
                    .HasColumnType("datetime");

                entity.Property(e => e.TimeOutput)
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails)
                    .HasConstraintName("FK_InvoiceDetail_Invoice");
            });

            modelBuilder.Entity<MembershipLevel>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Membersh__3214EC077E9FB408");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__PaymentM__3214EC07DBD5B7BA");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.PaymentDate).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.Status).HasDefaultValue("Pending");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Product__3214EC071741E437");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.Category).WithMany(p => p.Products)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Product_ProductCategory");
            });

            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__ProductC__3214EC0716D900AF");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Reservat__3214EC07C17AA4F1");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.Status).HasDefaultValue("Pending");

                entity.HasOne(d => d.Branch).WithMany(p => p.Reservations)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Reservation_Branch");

                entity.HasOne(d => d.Customer).WithMany(p => p.Reservations)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Reservation_Customer");

                entity.HasOne(d => d.Table).WithMany(p => p.Reservations)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Reservation_BilliardTable");
            });

            modelBuilder.Entity<ServiceProductDetail>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__ServiceP__3214EC07D1DA5B90");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.Quantity).HasDefaultValue(1);

                entity.HasOne(d => d.Invoice).WithMany(p => p.ServiceProductDetails).HasConstraintName("FK_ServiceProductDetail_Invoice");

                entity.HasOne(d => d.Product).WithMany(p => p.ServiceProductDetails)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ServiceProductDetail_Product");
            });

            modelBuilder.Entity<TableStatus>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__TableSta__3214EC0721310431");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}