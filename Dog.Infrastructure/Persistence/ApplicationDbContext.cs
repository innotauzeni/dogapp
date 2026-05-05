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


        public DbSet<Dogtbl> ka_dogs { get; set; }
        public DbSet<ShowDogInfo> ka_showdoginfos { get; set; }
        public DbSet<MatingRecord> ka_matingrecords { get; set; }
        public DbSet<Litter> ka_litters { get; set; }
        public DbSet<Puppy> ka_puppies { get; set; }
        public DbSet<VaccinationRecord> ka_vaccinationrecords { get; set; }
        public DbSet<Deployment> ka_deployments { get; set; }
        public DbSet<BreedGroup> ka_breedgroups { get; set; }
        public DbSet<Breed> ka_breeds { get; set; }

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

        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : AuditableBaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var auditEntries = BeforeSaveChanges();
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await OnAfterSaveChangesAsync(auditEntries);
            return result;
        }

        private List<AuditEntry> BeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var entries = new List<AuditEntry>();
            var currentUserId = GetCurrentUserId();

            foreach (var entry in ChangeTracker.Entries())
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

                    entries.Add(new AuditEntry
                    {
                        ActionType = actionType,
                        EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString(),
                        EntityName = entry.Metadata.ClrType.Name,
                        Username = currentUserId,
                        TimeStamp = DateTime.UtcNow,
                        Changes = entry.Properties
                                            .Where(p => !p.IsTemporary)
                                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue as object ?? DBNull.Value),
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
