using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TourManagementWeb.Models.Entities;

public partial class TourDbContext : DbContext
{
    public TourDbContext()
    {
    }

    public TourDbContext(DbContextOptions<TourDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingPassenger> BookingPassengers { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Destination> Destinations { get; set; }

    public virtual DbSet<DestinationImage> DestinationImages { get; set; }

    public virtual DbSet<FavoriteTour> FavoriteTours { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Province> Provinces { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tour> Tours { get; set; }

    public virtual DbSet<TourCategory> TourCategories { get; set; }

    public virtual DbSet<TourDeparture> TourDepartures { get; set; }

    public virtual DbSet<TourDestination> TourDestinations { get; set; }

    public virtual DbSet<TourImage> TourImages { get; set; }

    public virtual DbSet<TourItinerary> TourItineraries { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VDeparturePassenger> VDeparturePassengers { get; set; }

    public virtual DbSet<VTourList> VTourLists { get; set; }

    public virtual DbSet<VTourStat> VTourStats { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseOracle("User Id=TOURAPP;Password=TourApp123;Data Source=localhost:1521/FREEPDB1;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("TOURAPP")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("SYS_C009012");

            entity.ToTable("AUDIT_LOGS");

            entity.Property(e => e.LogId)
                .HasPrecision(19)
                .HasColumnName("LOG_ID");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ACTION");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.Detail)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("DETAIL");
            entity.Property(e => e.ObjectId)
                .HasPrecision(19)
                .HasColumnName("OBJECT_ID");
            entity.Property(e => e.ObjectType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("OBJECT_TYPE");
            entity.Property(e => e.UserId)
                .HasPrecision(10)
                .HasColumnName("USER_ID");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AUDIT_USER");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("SYS_C008970");

            entity.ToTable("BOOKINGS");

            entity.HasIndex(e => e.CustomerId, "IDX_BOOKING_CUSTOMER");

            entity.HasIndex(e => e.PaymentDeadline, "IDX_BOOKING_DEADLINE");

            entity.HasIndex(e => e.DepartureId, "IDX_BOOKING_DEPARTURE");

            entity.HasIndex(e => new { e.Status, e.PaymentStatus }, "IDX_BOOKING_STATUS");

            entity.HasIndex(e => e.BookingCode, "UQ_BOOKINGS_CODE").IsUnique();

            entity.Property(e => e.BookingId)
                .HasPrecision(10)
                .HasColumnName("BOOKING_ID");
            entity.Property(e => e.BookingCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("BOOKING_CODE");
            entity.Property(e => e.ConfirmedAt)
                .HasPrecision(6)
                .HasColumnName("CONFIRMED_AT");
            entity.Property(e => e.ConfirmedBy)
                .HasPrecision(10)
                .ValueGeneratedOnAdd()
                .HasColumnName("CONFIRMED_BY");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.CustomerId)
                .HasPrecision(10)
                .HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.DepartureId)
                .HasPrecision(10)
                .HasColumnName("DEPARTURE_ID");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("DISCOUNT_AMOUNT");
            entity.Property(e => e.FinalAmount)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("FINAL_AMOUNT");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("NOTE");
            entity.Property(e => e.NumAdults)
                .HasPrecision(3)
                .HasColumnName("NUM_ADULTS");
            entity.Property(e => e.NumChildren)
                .HasPrecision(3)
                .HasDefaultValueSql("0 ")
                .HasColumnName("NUM_CHILDREN");
            entity.Property(e => e.PaymentDeadline)
                .HasPrecision(6)
                .HasColumnName("PAYMENT_DEADLINE");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'UNPAID' ")
                .HasColumnName("PAYMENT_STATUS");
            entity.Property(e => e.PromotionId)
                .HasPrecision(10)
                .HasColumnName("PROMOTION_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("'PENDING' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("TOTAL_AMOUNT");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(6)
                .HasColumnName("UPDATED_AT");

            entity.HasOne(d => d.ConfirmedByNavigation).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ConfirmedBy)
                .HasConstraintName("FK_BOOKING_CONFIRM");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_CUST");

            entity.HasOne(d => d.Departure).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.DepartureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_DEP");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_BOOKING_PROMO");
        });

        modelBuilder.Entity<BookingPassenger>(entity =>
        {
            entity.HasKey(e => e.PassengerId).HasName("SYS_C008982");

            entity.ToTable("BOOKING_PASSENGERS");

            entity.Property(e => e.PassengerId)
                .HasPrecision(10)
                .HasColumnName("PASSENGER_ID");
            entity.Property(e => e.BookingId)
                .HasPrecision(10)
                .HasColumnName("BOOKING_ID");
            entity.Property(e => e.DateOfBirth)
                .HasColumnType("DATE")
                .HasColumnName("DATE_OF_BIRTH");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("FULL_NAME");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("GENDER");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ID_NUMBER");
            entity.Property(e => e.PassengerType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PASSENGER_TYPE");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingPassengers)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_PAX_BOOKING");
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("SYS_C009006");

            entity.ToTable("CONTACTS");

            entity.HasIndex(e => e.Status, "IDX_CONTACT_STATUS");

            entity.Property(e => e.ContactId)
                .HasPrecision(10)
                .HasColumnName("CONTACT_ID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("FULL_NAME");
            entity.Property(e => e.HandledBy)
                .HasPrecision(10)
                .HasColumnName("HANDLED_BY");
            entity.Property(e => e.Message)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("MESSAGE");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PHONE");
            entity.Property(e => e.RepliedAt)
                .HasPrecision(6)
                .HasColumnName("REPLIED_AT");
            entity.Property(e => e.Reply)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("REPLY");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'NEW' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("SUBJECT");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");

            entity.HasOne(d => d.HandledByNavigation).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.HandledBy)
                .HasConstraintName("FK_CONTACT_HANDLER");

            entity.HasOne(d => d.Tour).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK_CONTACT_TOUR");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("SYS_C008857");

            entity.ToTable("CUSTOMERS");

            entity.HasIndex(e => e.UserId, "UQ_CUSTOMERS_USER").IsUnique();

            entity.Property(e => e.CustomerId)
                .HasPrecision(10)
                .HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("ADDRESS");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.DateOfBirth)
                .HasColumnType("DATE")
                .HasColumnName("DATE_OF_BIRTH");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("FULL_NAME");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("GENDER");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ID_NUMBER");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PHONE");
            entity.Property(e => e.UserId)
                .HasPrecision(10)
                .HasColumnName("USER_ID");

            entity.HasOne(d => d.User).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId)
                .HasConstraintName("FK_CUSTOMERS_USER");
        });

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.HasKey(e => e.DestinationId).HasName("SYS_C008872");

            entity.ToTable("DESTINATIONS");

            entity.HasIndex(e => e.ProvinceId, "IDX_DEST_PROVINCE");

            entity.Property(e => e.DestinationId)
                .HasPrecision(10)
                .HasColumnName("DESTINATION_ID");
            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("ADDRESS");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.DestinationName)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("DESTINATION_NAME");
            entity.Property(e => e.ProvinceId)
                .HasPrecision(10)
                .HasColumnName("PROVINCE_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'ACTIVE' ")
                .HasColumnName("STATUS");

            entity.HasOne(d => d.Province).WithMany(p => p.Destinations)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DEST_PROVINCE");
        });

        modelBuilder.Entity<DestinationImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("SYS_C008879");

            entity.ToTable("DESTINATION_IMAGES");

            entity.Property(e => e.ImageId)
                .HasPrecision(10)
                .HasColumnName("IMAGE_ID");
            entity.Property(e => e.Caption)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CAPTION");
            entity.Property(e => e.DestinationId)
                .HasPrecision(10)
                .HasColumnName("DESTINATION_ID");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IMAGE_URL");
            entity.Property(e => e.IsPrimary)
                .IsRequired()
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(1)")
                .HasColumnName("IS_PRIMARY");

            entity.HasOne(d => d.Destination).WithMany(p => p.DestinationImages)
                .HasForeignKey(d => d.DestinationId)
                .HasConstraintName("FK_DESTIMG_DEST");
        });

        modelBuilder.Entity<FavoriteTour>(entity =>
        {
            entity.HasKey(e => new { e.CustomerId, e.TourId });

            entity.ToTable("FAVORITE_TOURS");

            entity.Property(e => e.CustomerId)
                .HasPrecision(10)
                .HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");

            entity.HasOne(d => d.Customer).WithMany(p => p.FavoriteTours)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_FAV_CUSTOMER");

            entity.HasOne(d => d.Tour).WithMany(p => p.FavoriteTours)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK_FAV_TOUR");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("SYS_C008995");

            entity.ToTable("PAYMENTS");

            entity.HasIndex(e => e.BookingId, "IDX_PAYMENT_BOOKING");

            entity.HasIndex(e => e.TransactionRef, "UQ_PAYMENT_TXN").IsUnique();

            entity.Property(e => e.PaymentId)
                .HasPrecision(10)
                .HasColumnName("PAYMENT_ID");
            entity.Property(e => e.Amount)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("AMOUNT");
            entity.Property(e => e.BookingId)
                .HasPrecision(10)
                .ValueGeneratedOnAdd()
                .HasColumnName("BOOKING_ID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("METHOD");
            entity.Property(e => e.PaidAt)
                .HasPrecision(6)
                .HasColumnName("PAID_AT");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("'PAYMENT' ")
                .HasColumnName("PAYMENT_TYPE");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("'PENDING' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.TransactionRef)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("TRANSACTION_REF");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAYMENT_BOOKING");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("SYS_C008952");

            entity.ToTable("PROMOTIONS");

            entity.HasIndex(e => e.PromoCode, "UQ_PROMO_CODE").IsUnique();

            entity.Property(e => e.PromotionId)
                .HasPrecision(10)
                .HasColumnName("PROMOTION_ID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("DISCOUNT_TYPE");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("DISCOUNT_VALUE");
            entity.Property(e => e.EndDate)
                .HasColumnType("DATE")
                .HasColumnName("END_DATE");
            entity.Property(e => e.MinOrderAmount)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("MIN_ORDER_AMOUNT");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PROMO_CODE");
            entity.Property(e => e.StartDate)
                .HasColumnType("DATE")
                .HasColumnName("START_DATE");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'ACTIVE' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.UsageLimit)
                .HasPrecision(8)
                .HasColumnName("USAGE_LIMIT");
            entity.Property(e => e.UsedCount)
                .HasPrecision(8)
                .HasDefaultValueSql("0 ")
                .HasColumnName("USED_COUNT");
        });

        modelBuilder.Entity<Province>(entity =>
        {
            entity.HasKey(e => e.ProvinceId).HasName("SYS_C008864");

            entity.ToTable("PROVINCES");

            entity.HasIndex(e => e.ProvinceName, "UQ_PROVINCES_NAME").IsUnique();

            entity.Property(e => e.ProvinceId)
                .HasPrecision(10)
                .HasColumnName("PROVINCE_ID");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IMAGE_URL");
            entity.Property(e => e.ProvinceName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("PROVINCE_NAME");
            entity.Property(e => e.Region)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("REGION");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("SYS_C009023");

            entity.ToTable("REVIEWS");

            entity.HasIndex(e => new { e.TourId, e.Status }, "IDX_REVIEW_TOUR");

            entity.HasIndex(e => new { e.CustomerId, e.TourId }, "UQ_REVIEWS_CUST_TOUR").IsUnique();

            entity.Property(e => e.ReviewId)
                .HasPrecision(10)
                .HasColumnName("REVIEW_ID");
            entity.Property(e => e.BookingId)
                .HasPrecision(10)
                .HasColumnName("BOOKING_ID");
            entity.Property(e => e.CommentText)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("COMMENT_TEXT");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.CustomerId)
                .HasPrecision(10)
                .HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.ModeratedBy)
                .HasPrecision(10)
                .HasColumnName("MODERATED_BY");
            entity.Property(e => e.Rating)
                .HasPrecision(2)
                .HasColumnName("RATING");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'PENDING' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");

            entity.HasOne(d => d.Booking).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_REVIEWS_BOOKING");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVIEWS_CUSTOMER");

            entity.HasOne(d => d.ModeratedByNavigation).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ModeratedBy)
                .HasConstraintName("FK_REVIEWS_MODERATOR");

            entity.HasOne(d => d.Tour).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVIEWS_TOUR");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("SYS_C008835");

            entity.ToTable("ROLES");

            entity.HasIndex(e => e.RoleName, "UQ_ROLES_NAME").IsUnique();

            entity.Property(e => e.RoleId)
                .HasPrecision(10)
                .HasColumnName("ROLE_ID");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.RoleName)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ROLE_NAME");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.TourId).HasName("SYS_C008899");

            entity.ToTable("TOURS");

            entity.HasIndex(e => e.CategoryId, "IDX_TOURS_CATEGORY");

            entity.HasIndex(e => e.TourCode, "UQ_TOURS_CODE").IsUnique();

            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");
            entity.Property(e => e.CategoryId)
                .HasPrecision(10)
                .HasColumnName("CATEGORY_ID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.CreatedBy)
                .HasPrecision(10)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.DefaultRating)
                .HasDefaultValueSql("5 ")
                .HasColumnType("NUMBER(2,1)")
                .HasColumnName("DEFAULT_RATING");
            entity.Property(e => e.DepartureProvinceId)
                .HasPrecision(10)
                .HasColumnName("DEPARTURE_PROVINCE_ID");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.DurationDays)
                .HasPrecision(3)
                .HasColumnName("DURATION_DAYS");
            entity.Property(e => e.DurationNights)
                .HasPrecision(3)
                .HasColumnName("DURATION_NIGHTS");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'DRAFT' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.Summary)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("SUMMARY");
            entity.Property(e => e.TourCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("TOUR_CODE");
            entity.Property(e => e.TourName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TOUR_NAME");
            entity.Property(e => e.Transport)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'BUS' ")
                .HasColumnName("TRANSPORT");

            entity.HasOne(d => d.Category).WithMany(p => p.Tours)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOURS_CATEGORY");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tours)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOURS_CREATOR");

            entity.HasOne(d => d.DepartureProvince).WithMany(p => p.Tours)
                .HasForeignKey(d => d.DepartureProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOURS_DEP_PROV");
        });

        modelBuilder.Entity<TourCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("SYS_C008883");

            entity.ToTable("TOUR_CATEGORIES");

            entity.HasIndex(e => e.CategoryName, "UQ_CATEGORIES_NAME").IsUnique();

            entity.Property(e => e.CategoryId)
                .HasPrecision(10)
                .HasColumnName("CATEGORY_ID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("CATEGORY_NAME");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
        });

        modelBuilder.Entity<TourDeparture>(entity =>
        {
            entity.HasKey(e => e.DepartureId).HasName("SYS_C008937");

            entity.ToTable("TOUR_DEPARTURES");

            entity.HasIndex(e => new { e.DepartureDate, e.Status }, "IDX_DEP_DATE_STATUS");

            entity.HasIndex(e => e.TourId, "IDX_DEP_TOUR");

            entity.Property(e => e.DepartureId)
                .HasPrecision(10)
                .HasColumnName("DEPARTURE_ID");
            entity.Property(e => e.BookedSeats)
                .HasPrecision(4)
                .HasDefaultValueSql("0 ")
                .HasColumnName("BOOKED_SEATS");
            entity.Property(e => e.DepartureDate)
                .HasColumnType("DATE")
                .HasColumnName("DEPARTURE_DATE");
            entity.Property(e => e.GuideName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("GUIDE_NAME");
            entity.Property(e => e.MaxSeats)
                .HasPrecision(4)
                .HasColumnName("MAX_SEATS");
            entity.Property(e => e.MeetingPoint)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("MEETING_POINT");
            entity.Property(e => e.PriceAdult)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("PRICE_ADULT");
            entity.Property(e => e.PriceChild)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("PRICE_CHILD");
            entity.Property(e => e.ReturnDate)
                .HasColumnType("DATE")
                .HasColumnName("RETURN_DATE");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'OPEN' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourDepartures)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DEP_TOUR");
        });

        modelBuilder.Entity<TourDestination>(entity =>
        {
            entity.HasKey(e => new { e.TourId, e.DestinationId });

            entity.ToTable("TOUR_DESTINATIONS");

            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");
            entity.Property(e => e.DestinationId)
                .HasPrecision(10)
                .HasColumnName("DESTINATION_ID");
            entity.Property(e => e.VisitOrder)
                .HasPrecision(3)
                .HasColumnName("VISIT_ORDER");

            entity.HasOne(d => d.Destination).WithMany(p => p.TourDestinations)
                .HasForeignKey(d => d.DestinationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TD_DEST");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourDestinations)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK_TD_TOUR");
        });

        modelBuilder.Entity<TourImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("SYS_C008909");

            entity.ToTable("TOUR_IMAGES");

            entity.Property(e => e.ImageId)
                .HasPrecision(10)
                .HasColumnName("IMAGE_ID");
            entity.Property(e => e.Caption)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CAPTION");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("IMAGE_URL");
            entity.Property(e => e.IsPrimary)
                .IsRequired()
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(1)")
                .HasColumnName("IS_PRIMARY");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourImages)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK_TOURIMG_TOUR");
        });

        modelBuilder.Entity<TourItinerary>(entity =>
        {
            entity.HasKey(e => e.ItineraryId).HasName("SYS_C008921");

            entity.ToTable("TOUR_ITINERARIES");

            entity.HasIndex(e => new { e.TourId, e.DayNo }, "UQ_ITINERARY_DAY").IsUnique();

            entity.Property(e => e.ItineraryId)
                .HasPrecision(10)
                .HasColumnName("ITINERARY_ID");
            entity.Property(e => e.Accommodation)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ACCOMMODATION");
            entity.Property(e => e.Content)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("CONTENT");
            entity.Property(e => e.DayNo)
                .HasPrecision(3)
                .HasColumnName("DAY_NO");
            entity.Property(e => e.Meals)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("MEALS");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TITLE");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .HasColumnName("TOUR_ID");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourItineraries)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK_ITINERARY_TOUR");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("SYS_C008847");

            entity.ToTable("USERS");

            entity.HasIndex(e => e.Email, "UQ_USERS_EMAIL").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_USERS_USERNAME").IsUnique();

            entity.Property(e => e.UserId)
                .HasPrecision(10)
                .HasColumnName("USER_ID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP ")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FailedLoginCount)
                .HasPrecision(3)
                .HasDefaultValueSql("0 ")
                .HasColumnName("FAILED_LOGIN_COUNT");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("FULL_NAME");
            entity.Property(e => e.LastLoginAt)
                .HasPrecision(6)
                .HasColumnName("LAST_LOGIN_AT");
            entity.Property(e => e.LockoutEnd)
                .HasPrecision(6)
                .HasColumnName("LOCKOUT_END");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.PasswordResetExpires)
                .HasPrecision(6)
                .HasColumnName("PASSWORD_RESET_EXPIRES");
            entity.Property(e => e.PasswordResetHash)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("PASSWORD_RESET_HASH");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PHONE");
            entity.Property(e => e.RoleId)
                .HasPrecision(10)
                .HasColumnName("ROLE_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'ACTIVE' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("USERNAME");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_USERS_ROLE");
        });

        modelBuilder.Entity<VDeparturePassenger>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_DEPARTURE_PASSENGERS");

            entity.Property(e => e.BookingCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("BOOKING_CODE");
            entity.Property(e => e.BookingId)
                .HasPrecision(10)
                .ValueGeneratedOnAdd()
                .HasColumnName("BOOKING_ID");
            entity.Property(e => e.ContactName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("CONTACT_NAME");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CONTACT_PHONE");
            entity.Property(e => e.DateOfBirth)
                .HasColumnType("DATE")
                .HasColumnName("DATE_OF_BIRTH");
            entity.Property(e => e.DepartureDate)
                .HasColumnType("DATE")
                .HasColumnName("DEPARTURE_DATE");
            entity.Property(e => e.DepartureId)
                .HasPrecision(10)
                .ValueGeneratedOnAdd()
                .HasColumnName("DEPARTURE_ID");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("GENDER");
            entity.Property(e => e.GuideName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("GUIDE_NAME");
            entity.Property(e => e.PassengerId)
                .HasPrecision(10)
                .ValueGeneratedOnAdd()
                .HasColumnName("PASSENGER_ID");
            entity.Property(e => e.PassengerName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("PASSENGER_NAME");
            entity.Property(e => e.PassengerType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PASSENGER_TYPE");
            entity.Property(e => e.ReturnDate)
                .HasColumnType("DATE")
                .HasColumnName("RETURN_DATE");
            entity.Property(e => e.TourCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("TOUR_CODE");
            entity.Property(e => e.TourName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TOUR_NAME");
        });

        modelBuilder.Entity<VTourList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_TOUR_LIST");

            entity.Property(e => e.AvgRating)
                .HasColumnType("NUMBER")
                .HasColumnName("AVG_RATING");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("CATEGORY_NAME");
            entity.Property(e => e.DepartureFrom)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DEPARTURE_FROM");
            entity.Property(e => e.DurationDays)
                .HasPrecision(3)
                .HasColumnName("DURATION_DAYS");
            entity.Property(e => e.DurationNights)
                .HasPrecision(3)
                .HasColumnName("DURATION_NIGHTS");
            entity.Property(e => e.MinPrice)
                .HasColumnType("NUMBER")
                .HasColumnName("MIN_PRICE");
            entity.Property(e => e.NextDeparture)
                .HasColumnType("DATE")
                .HasColumnName("NEXT_DEPARTURE");
            entity.Property(e => e.ReviewCount)
                .HasColumnType("NUMBER")
                .HasColumnName("REVIEW_COUNT");
            entity.Property(e => e.TourCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("TOUR_CODE");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .ValueGeneratedOnAdd()
                .HasColumnName("TOUR_ID");
            entity.Property(e => e.TourName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TOUR_NAME");
            entity.Property(e => e.Transport)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TRANSPORT");
        });

        modelBuilder.Entity<VTourStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_TOUR_STATS");

            entity.Property(e => e.CancelledBookings)
                .HasColumnType("NUMBER")
                .HasColumnName("CANCELLED_BOOKINGS");
            entity.Property(e => e.ConfirmedBookings)
                .HasColumnType("NUMBER")
                .HasColumnName("CONFIRMED_BOOKINGS");
            entity.Property(e => e.PendingBookings)
                .HasColumnType("NUMBER")
                .HasColumnName("PENDING_BOOKINGS");
            entity.Property(e => e.Revenue)
                .HasColumnType("NUMBER")
                .HasColumnName("REVENUE");
            entity.Property(e => e.TotalBookings)
                .HasColumnType("NUMBER")
                .HasColumnName("TOTAL_BOOKINGS");
            entity.Property(e => e.TotalPassengers)
                .HasColumnType("NUMBER")
                .HasColumnName("TOTAL_PASSENGERS");
            entity.Property(e => e.TourId)
                .HasPrecision(10)
                .ValueGeneratedOnAdd()
                .HasColumnName("TOUR_ID");
            entity.Property(e => e.TourName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TOUR_NAME");
        });
        modelBuilder.HasSequence("SEQ_BOOKING_CODE");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
