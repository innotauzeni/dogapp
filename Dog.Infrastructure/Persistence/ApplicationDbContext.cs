using Dog.Core.Common;
using Dog.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Dog.Infrastructure.Persistence
{
   public class ApplicationDbContext : IdentityDbContext<ApplicationUser, Role, string,
   Microsoft.AspNetCore.Identity.IdentityUserClaim<string>,
   ApplicationRoleUser,
   Microsoft.AspNetCore.Identity.IdentityUserLogin<string>,
   Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>,
   Microsoft.AspNetCore.Identity.IdentityUserToken<string>>
    {
        private readonly string _username;
        private readonly IHttpContextAccessor _context;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _context = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }


        public DbSet<AuditEntry> ka_auditentries { get; set; }
        public DbSet<SystemModule> ka_systemmodules { get; set; }
        public DbSet<Submodule> ka_submodules { get; set; }
        public DbSet<Permission> ka_permissions { get; set; }
        public DbSet<RoleMenu> ka_rolemenus { get; set; }
        public DbSet<RolePermission> ka_rolepermissions { get; set; }
        public DbSet<RefreshToken> ka_refreshtokens { get; set; }
        public DbSet<Country> ka_countries { get; set; }
        public DbSet<Province> ka_provinces { get; set; }
        public DbSet<City> ka_cities { get; set; }
        public DbSet<Bank> ka_banks { get; set; }
        public DbSet<BankAccount> ka_bankaccounts { get; set; }
        public DbSet<Currency> ka_currencies { get; set; }
        public DbSet<Emailqueue> ka_emailqueue { get; set; }

        public DbSet<ExchangeRate> ka_systemrates { get; set; }
        public DbSet<Company> ka_companies { get; set; }
        public DbSet<PaymentMethod> ka_paymentmethods { get; set; }

        public DbSet<Passwordreset> ka_passwordresets { get; set; }
        public DbSet<GeneralSetting> ka_generalsettings { get; set; }
        public DbSet<Invoice> ka_invoices { get; set; }
        public DbSet<InvoiceItem> ka_invoiceitemss { get; set; }
        public DbSet<Receipt> ka_receipts { get; set; }
        public DbSet<Suspense> ka_suspenses { get; set; }
        public DbSet<Customer> ka_customers { get; set; }
        public DbSet<Emailqueue> ka_emailqueues { get; set; }
        public DbSet<SystemAction> ka_systemactions { get; set; }
        public DbSet<SystemRoleAction> ka_systemroleactions { get; set; }

        public DbSet<SystemLogSetting> ka_sytemlogettings { get; set; }
        public DbSet<SystemErrorLog> ka_sytemerrorlogs { get; set; }
        public DbSet<Webhook> ka_webhooks { get; set; }
        public DbSet<WebhookLog> ka_webhooklogs { get; set; }
        public DbSet<KaAppSetting> ka_appsettings { get; set; }


        public DbSet<Dogtbl> ka_dogs { get; set; }
        public DbSet<DogAssignment> ka_dogassignments { get; set; }
        public DbSet<DogCareRecord> ka_dogcarerecords { get; set; }
        public DbSet<ShowDogInfo> ka_showdoginfos { get; set; }
        public DbSet<MatingRecord> ka_matingrecords { get; set; }
        public DbSet<Litter> ka_litters { get; set; }
        public DbSet<Puppy> ka_puppies { get; set; }
        public DbSet<VaccinationRecord> ka_vaccinationrecords { get; set; }
        public DbSet<Deployment> ka_deployments { get; set; }
        public DbSet<BreedGroup> ka_breedgroups { get; set; }
        public DbSet<Breed> ka_breeds { get; set; }
        public DbSet<Accident> ka_accidents { get; set; }
        public DbSet<CustomerSite> ka_customersites { get; set; }
        public DbSet<CustomerContract> ka_customercontracts { get; set; }
        public DbSet<Expense> ka_expenses { get; set; }
        public DbSet<DogCertification> ka_dogcertifications { get; set; }
        public DbSet<DocumentAttachment> ka_documentattachments { get; set; }
        public DbSet<NotificationLog> ka_notificationlogs { get; set; }

        // ── Dog Training ──────────────────────────────────────────────────────
        public DbSet<DogTrainingProgramme> ka_dogtrainingprogrammes { get; set; }
        public DbSet<DogTrainingProgrammeWeek> ka_dogtrainingprogrammeweeks { get; set; }
        public DbSet<DogTrainingProgrammeActivity> ka_dogtrainingprogrammeactivities { get; set; }
        public DbSet<DogHealthAssessment> ka_doghealthassessments { get; set; }
        public DbSet<DogHealthAssessmentItem> ka_doghealthassessmentitems { get; set; }
        public DbSet<DogTrainingEnrollment> ka_dogtrainingenrollments { get; set; }
        public DbSet<DogTrainingHandlerProfile> ka_dogtraininghandlerprofiles { get; set; }
        public DbSet<DogHandlerAssignment> ka_doghandlerassignments { get; set; }
        public DbSet<DogTrainingWeeklyPlan> ka_dogtrainingweeklyplans { get; set; }
        public DbSet<DogTrainingDailyLog> ka_dogtrainingdailylogs { get; set; }
        public DbSet<DogTrainingProgressReport> ka_dogtrainingprogressreports { get; set; }
        public DbSet<DogTrainingFinalAssessment> ka_dogtrainingfinalassessments { get; set; }
        public DbSet<DogTrainingCertificate> ka_dogtrainingcertificates { get; set; }
        public DbSet<DogTrainingCustomer> ka_dogtrainingcustomers { get; set; }
        public DbSet<DogTrainingDog> ka_dogtrainingdogs { get; set; }
        public DbSet<DogTrainingInvoice> ka_dogtraininginvoices { get; set; }
        public DbSet<DogTrainingInvoiceItem> ka_dogtraininginvoiceitems { get; set; }
        public DbSet<DogTrainingPayment> ka_dogtrainingpayments { get; set; }

        // ── Billing upgrade: client companies, payment ledger, email, invoice catalogue ──
        public DbSet<CustomerCompany> ka_customercompanies { get; set; }
        public DbSet<InvoicePayment> ka_invoicepayments { get; set; }
        public DbSet<EmailSetting> ka_emailsettings { get; set; }
        public DbSet<EmailLog> ka_emaillogs { get; set; }
        public DbSet<InvoiceCatalogItem> ka_invoicecatalog { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This will be configured by DI, but we add this as fallback
                optionsBuilder.EnableSensitiveDataLogging(false);
                optionsBuilder.EnableDetailedErrors(false);
            }
            else
            {
                // Disable sensitive data logging in production
                optionsBuilder.EnableSensitiveDataLogging(false);
            }
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AuditEntry>().Property(a => a.Changes)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<string, object>>(v)!);

            // RoleMenu — string RoleId → AspNetRoles.Id
            builder.Entity<RoleMenu>(b =>
            {
                b.HasOne(rm => rm.Role)
                    .WithMany(r => r.RoleMenus)
                    .HasForeignKey(rm => rm.RoleId)
                    .HasPrincipalKey(r => r.Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RolePermission — string RoleId → AspNetRoles.Id
            builder.Entity<RolePermission>(b =>
            {
                b.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .HasPrincipalKey(r => r.Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure self-referencing relationships for Dogtbl
            builder.Entity<Dogtbl>(b =>
            {
                b.HasOne(d => d.Sire)
                    .WithMany(d => d.SireOffspring)
                    .HasForeignKey(d => d.SireId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(d => d.Dam)
                    .WithMany(d => d.DamOffspring)
                    .HasForeignKey(d => d.DamId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Owner (customer); detaching the customer clears the link.
                b.HasOne(d => d.Customer)
                    .WithMany()
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure MatingRecord relationships
            builder.Entity<MatingRecord>(b =>
            {
                b.HasOne(m => m.Sire)
                    .WithMany(d => d.MatingRecordsAsSire)
                    .HasForeignKey(m => m.SireId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(m => m.Dam)
                    .WithMany(d => d.MatingRecordsAsDam)
                    .HasForeignKey(m => m.DamId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DogAssignment>(b =>
            {
                b.HasOne(a => a.Dog).WithMany(d => d.Assignments).HasForeignKey(a => a.DogId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.HandlerUser).WithMany().HasForeignKey(a => a.HandlerUserId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.Branch).WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DogCareRecord>(b =>
            {
                b.HasOne(r => r.Dog).WithMany(d => d.CareRecords).HasForeignKey(r => r.DogId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(r => r.RecordedByUser).WithMany().HasForeignKey(r => r.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
            });

            // A vaccination is linked to a registered dog OR an unregistered puppy (both optional).
            builder.Entity<VaccinationRecord>(b =>
            {
                // Must pair with Dogtbl.VaccinationRecords explicitly — an anonymous
                // .WithMany() left that collection unpaired, so EF Core additionally
                // invented a second, navigation-less relationship (and a phantom
                // "DogtblId" shadow column that was never actually migrated) to
                // represent it.
                b.HasOne(v => v.Dog).WithMany(d => d.VaccinationRecords).HasForeignKey(v => v.DogId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(v => v.Puppy).WithMany().HasForeignKey(v => v.PuppyId).OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Accident relationships
            builder.Entity<Accident>(b =>
            {
                b.HasOne(a => a.Dog)
                    .WithMany()
                    .HasForeignKey(a => a.DogId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Customer sites (premises) — must pair with Customer.Sites explicitly;
            // leaving it anonymous is what caused the VaccinationRecord/Dogtbl
            // phantom-shadow-column bug elsewhere in this model.
            builder.Entity<CustomerSite>(b =>
            {
                b.HasOne(s => s.Customer)
                    .WithMany(c => c.Sites)
                    .HasForeignKey(s => s.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Customer contracts — same pairing rule as Sites above.
            builder.Entity<CustomerContract>(b =>
            {
                b.HasOne(c => c.Customer)
                    .WithMany(c => c.Contracts)
                    .HasForeignKey(c => c.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Expenses — optional dog link. Dogtbl has no inverse "Expenses" collection,
            // so anonymous .WithMany() is safe (same reasoning as Deployment->Customer below).
            builder.Entity<Expense>(b =>
            {
                b.HasOne(e => e.Dog)
                    .WithMany()
                    .HasForeignKey(e => e.DogId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Dog certifications — Dogtbl has no inverse collection here either.
            builder.Entity<DogCertification>(b =>
            {
                b.HasOne(c => c.Dog)
                    .WithMany()
                    .HasForeignKey(c => c.DogId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Document attachments and notification logs reference their owner loosely
            // (EntityType/EntityId, not a typed FK) so they need no relationship config.

            // Deployment — who/where a dog currently is. Customer/CustomerSite have
            // no inverse collection back to Deployment, so anonymous .WithMany() is
            // safe here (nothing left unpaired for EF to invent a second relationship for).
            builder.Entity<Deployment>(b =>
            {
                b.HasOne(d => d.Dog)
                    .WithMany(d => d.Deployments)
                    .HasForeignKey(d => d.DogId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(d => d.Customer)
                    .WithMany()
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(d => d.CustomerSite)
                    .WithMany()
                    .HasForeignKey(d => d.CustomerSiteId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            ConfigureDogTraining(builder);
            ConfigureBilling(builder);

            // Global soft-delete query filters
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(AuditableBaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(ApplicationDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter),
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?.MakeGenericMethod(entityType.ClrType);
                    method?.Invoke(null, new object[] { builder });
                }
            }
        }

        /// <summary>Relationships for the Dog Training module. Parent→child (programme, enrolment) cascade;
        /// everything pointing at shared records (dogs, customers, invoices, users) is Restrict/SetNull so
        /// training data can never delete or be tangled into the existing modules.</summary>
        private static void ConfigureDogTraining(ModelBuilder builder)
        {
            builder.Entity<DogTrainingProgrammeWeek>(b =>
            {
                b.HasOne(w => w.Programme).WithMany(p => p.Weeks).HasForeignKey(w => w.ProgrammeId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(w => new { w.ProgrammeId, w.WeekNumber });
            });
            builder.Entity<DogTrainingProgrammeActivity>(b =>
            {
                b.HasOne(a => a.Programme).WithMany(p => p.Activities).HasForeignKey(a => a.ProgrammeId).OnDelete(DeleteBehavior.Cascade);
                b.Property(a => a.TimeOfDay).HasMaxLength(10);
            });


            // Training customers / dogs / invoices / payments — a separate set from the company's own dogs & invoices.
            builder.Entity<DogTrainingDog>(b =>
                b.HasOne(d => d.Customer).WithMany(c => c.Dogs).HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.Restrict));
            builder.Entity<DogTrainingInvoice>(b =>
            {
                b.Property(i => i.InvoiceNumber).HasMaxLength(40);
                b.Property(i => i.Status).HasMaxLength(20);
                b.HasIndex(i => i.InvoiceNumber).IsUnique();
                b.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<DogTrainingInvoiceItem>(b =>
                b.HasOne(i => i.Invoice).WithMany(v => v.Items).HasForeignKey(i => i.InvoiceId).OnDelete(DeleteBehavior.Cascade));
            builder.Entity<DogTrainingPayment>(b =>
            {
                b.Property(p => p.PaymentNumber).HasMaxLength(40);
                b.HasIndex(p => p.PaymentNumber).IsUnique();
                b.HasOne(p => p.Invoice).WithMany(v => v.Payments).HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DogTrainingEnrollment>(b =>
            {
                b.Property(e => e.EnrollmentNumber).HasMaxLength(40);
                b.Property(e => e.Status).HasMaxLength(40);
                b.HasIndex(e => e.EnrollmentNumber).IsUnique();
                b.HasIndex(e => e.Status);
                b.HasOne(e => e.Dog).WithMany().HasForeignKey(e => e.DogId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.Programme).WithMany().HasForeignKey(e => e.ProgrammeId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.Invoice).WithMany().HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.SetNull);
                b.HasOne(e => e.Handler).WithMany().HasForeignKey(e => e.HandlerUserId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DogHealthAssessment>(b =>
            {
                b.Property(a => a.Result).HasMaxLength(40);
                b.HasOne(a => a.Enrollment).WithMany(e => e.HealthAssessments).HasForeignKey(a => a.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(a => a.Dog).WithMany().HasForeignKey(a => a.DogId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<DogHealthAssessmentItem>(b =>
                b.HasOne(i => i.Assessment).WithMany(a => a.Items).HasForeignKey(i => i.AssessmentId).OnDelete(DeleteBehavior.Cascade));

            builder.Entity<DogTrainingHandlerProfile>(b =>
            {
                b.HasIndex(h => h.UserId).IsUnique();
                b.HasOne(h => h.User).WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<DogHandlerAssignment>(b =>
            {
                b.HasOne(a => a.Enrollment).WithMany(e => e.HandlerAssignments).HasForeignKey(a => a.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(a => a.HandlerUser).WithMany().HasForeignKey(a => a.HandlerUserId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DogTrainingWeeklyPlan>(b =>
            {
                b.HasOne(p => p.Enrollment).WithMany(e => e.WeeklyPlans).HasForeignKey(p => p.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(p => new { p.EnrollmentId, p.WeekNumber });
            });
            builder.Entity<DogTrainingDailyLog>(b =>
            {
                b.HasOne(l => l.Enrollment).WithMany(e => e.DailyLogs).HasForeignKey(l => l.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(l => l.Handler).WithMany().HasForeignKey(l => l.HandlerUserId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(l => new { l.EnrollmentId, l.TrainingDate });
            });
            builder.Entity<DogTrainingProgressReport>(b =>
            {
                b.HasOne(r => r.Enrollment).WithMany(e => e.ProgressReports).HasForeignKey(r => r.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(r => r.Handler).WithMany().HasForeignKey(r => r.HandlerUserId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(r => new { r.EnrollmentId, r.WeekNumber });
            });

            builder.Entity<DogTrainingFinalAssessment>(b =>
                b.HasOne(f => f.Enrollment).WithMany(e => e.FinalAssessments).HasForeignKey(f => f.EnrollmentId).OnDelete(DeleteBehavior.Cascade));

            builder.Entity<DogTrainingCertificate>(b =>
            {
                b.Property(c => c.CertificateNumber).HasMaxLength(40);
                b.Property(c => c.VerificationToken).HasMaxLength(64);
                b.HasIndex(c => c.CertificateNumber).IsUnique();
                b.HasIndex(c => c.VerificationToken).IsUnique();
                b.HasOne(c => c.Enrollment).WithMany().HasForeignKey(c => c.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(c => c.FinalAssessment).WithOne(f => f.Certificate).HasForeignKey<DogTrainingCertificate>(c => c.FinalAssessmentId).OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBilling(ModelBuilder builder)
        {
            builder.Entity<Customer>(b =>
                b.HasOne(c => c.CustomerCompany).WithMany(cc => cc.Contacts).HasForeignKey(c => c.CustomerCompanyId).OnDelete(DeleteBehavior.SetNull));
            builder.Entity<InvoicePayment>(b =>
            {
                b.Property(p => p.PaymentNumber).HasMaxLength(40);
                b.Property(p => p.Method).HasMaxLength(20);
                b.Property(p => p.Status).HasMaxLength(20);
                b.HasIndex(p => p.PaymentNumber).IsUnique();
                b.HasOne(p => p.Invoice).WithMany(i => i.Ledger).HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : AuditableBaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var auditEntries = await BeforeSaveChangesAsync(cancellationToken);
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await OnAfterSaveChangesAsync(auditEntries);
            return result;
        }

        private async Task<List<AuditEntry>> BeforeSaveChangesAsync(CancellationToken cancellationToken)
        {
            ChangeTracker.DetectChanges();
            var entries = new List<AuditEntry>();
            var currentUserId = GetCurrentUserId();

            foreach (var entry in ChangeTracker.Entries().ToList())
            {
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                // Set audit timestamps on all AuditableBaseEntity instances
                if (entry.Entity is AuditableBaseEntity auditable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditable.CreatedAt = DateTime.UtcNow;
                            auditable.CreatedBy = currentUserId;
                            break;

                        case EntityState.Modified:
                            auditable.UpdatedAt = DateTime.UtcNow;
                            auditable.UpdatedBy = currentUserId;
                            // Detect soft delete safely
                            try
                            {
                                var isDeletedProp = entry.Property(nameof(AuditableBaseEntity.IsDeleted));
                                if (isDeletedProp.IsModified &&
                                    isDeletedProp.CurrentValue is true &&
                                    isDeletedProp.OriginalValue is false)
                                {
                                    auditable.DeletedAt = DateTime.UtcNow;
                                    auditable.DeletedBy = currentUserId;
                                }
                            }
                            catch { /* property may not exist on all entities */ }
                            break;

                        case EntityState.Deleted:
                            // Convert hard delete → soft delete
                            entry.State = EntityState.Modified;
                            auditable.IsDeleted = true;
                            auditable.DeletedAt = DateTime.UtcNow;
                            auditable.DeletedBy = currentUserId;
                            break;
                    }
                }

                // Record audit entry for every IAuditable entity
                // (AuditableBaseEntity implements IAuditable, so all tracked entities qualify)
                if (entry.Entity is IAuditable)
                {
                    // Skip AuditEntry itself to prevent recursive audit
                    if (entry.Entity is AuditEntry) continue;

                    var actionType = entry.State switch
                    {
                        EntityState.Added => "INSERT",
                        EntityState.Deleted => "DELETE",
                        _ => "UPDATE"
                    };

                    // For soft-deleted entities the state was changed to Modified above
                    // but we want to record it as DELETE
                    if (entry.Entity is AuditableBaseEntity ae && ae.IsDeleted &&
                        entry.State == EntityState.Modified)
                    {
                        try
                        {
                            var isDelProp = entry.Property(nameof(AuditableBaseEntity.IsDeleted));
                            if (isDelProp.IsModified && isDelProp.CurrentValue is true)
                                actionType = "DELETE";
                        }
                        catch { }
                    }

                    var changes = entry.Properties
                                            .Where(p => !p.IsTemporary)
                                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue as object ?? DBNull.Value);

                    // UPDATE (and soft-DELETE): keep the record as it was BEFORE this save. It is read
                    // straight from the database because entities loaded without tracking have no
                    // usable "original values". Stored under a reserved key so no schema change is needed.
                    if (entry.State == EntityState.Modified)
                    {
                        try
                        {
                            var dbValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                            if (dbValues != null)
                            {
                                var previous = new Dictionary<string, object>();
                                foreach (var p in entry.Properties.Where(p => !p.IsTemporary))
                                    previous[p.Metadata.Name] = dbValues[p.Metadata.Name] ?? DBNull.Value;
                                changes[PreviousKey] = previous;
                            }
                        }
                        catch { /* never block a save because the previous record could not be read */ }
                    }

                    // Who did it and from where (full user details are resolved from the user id when read).
                    changes[ActorKey] = BuildActorInfo(currentUserId);

                    entries.Add(new AuditEntry
                    {
                        ActionType = actionType,
                        EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString(),
                        EntityName = entry.Metadata.ClrType.Name,
                        Username = currentUserId,
                        TimeStamp = DateTime.UtcNow,
                        Changes = changes,
                        TempProperties = entry.Properties.Where(p => p.IsTemporary).ToList(),
                    });
                }
            }

            return entries;
        }

        private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var entry in auditEntries)
            {
                foreach (var prop in entry.TempProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        entry.EntityId = prop.CurrentValue?.ToString();
                        entry.Changes[prop.Metadata.Name] = prop.CurrentValue!;
                    }
                    else
                    {
                        entry.Changes[prop.Metadata.Name] = prop.CurrentValue!;
                    }
                }
            }

            ka_auditentries.AddRange(auditEntries);

            // Call base directly to avoid re-entering our override (prevents infinite recursion)
            await base.SaveChangesAsync(acceptAllChangesOnSuccess: true, CancellationToken.None);
        }

        /// <summary>Reserved keys inside <see cref="AuditEntry.Changes"/> (the column is JSON, so no migration).</summary>
        public const string PreviousKey = "__previous";
        public const string ActorKey = "__actor";

        private Dictionary<string, object> BuildActorInfo(string userId)
        {
            var info = new Dictionary<string, object> { ["userId"] = userId };
            var http = _context.HttpContext;
            if (http == null) return info;

            var user = http.User;
            void Put(string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) info[key] = value!; }

            Put("userName", user?.Identity?.Name ?? user?.FindFirstValue(ClaimTypes.Name));
            Put("email", user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue("email"));
            var roles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
            if (roles is { Count: > 0 }) info["role"] = string.Join(", ", roles);

            // Behind a proxy/Cloudflare the real client address is in a forwarded header.
            var ip = http.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                     ?? http.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                     ?? http.Connection.RemoteIpAddress?.ToString();
            Put("ip", ip);
            Put("userAgent", http.Request.Headers.UserAgent.ToString());
            Put("method", http.Request.Method);
            Put("path", http.Request.Path.Value);
            return info;
        }

        private string GetCurrentUserId()
        {
            var user = _context.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return "System";

            // JWT Bearer: NameIdentifier claim is stored as full URI by default
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? user.FindFirstValue("nameid")
                ?? user.FindFirstValue("user_id")
                ?? user.FindFirstValue("id");

            return string.IsNullOrEmpty(userIdClaim) ? "System" : userIdClaim;
        }

        /// <summary>
        /// Soft delete an entity by setting IsDeleted = true and populating DeletedBy and DeletedAt
        /// </summary>
        public void SoftDelete<TEntity>(TEntity entity) where TEntity : AuditableBaseEntity
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = GetCurrentUserId();
            Entry(entity).State = EntityState.Modified;
        }

        public void Restore<TEntity>(TEntity entity) where TEntity : AuditableBaseEntity
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedBy = null;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = GetCurrentUserId();
            Entry(entity).State = EntityState.Modified;
        }

        /// <summary>
        /// Get all entities including soft-deleted ones
        /// </summary>
        public IQueryable<TEntity> GetAllIncludingDeleted<TEntity>() where TEntity : AuditableBaseEntity
        {
            return Set<TEntity>().IgnoreQueryFilters();
        }

        /// <summary>
        /// Get only soft-deleted entities
        /// </summary>
        public IQueryable<TEntity> GetDeleted<TEntity>() where TEntity : AuditableBaseEntity
        {
            return Set<TEntity>().IgnoreQueryFilters().Where(e => e.IsDeleted);
        }








    }
}
