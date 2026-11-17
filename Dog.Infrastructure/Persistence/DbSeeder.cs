using Dog.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dog.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        private const string OwnerEmail = "systemowner@dogsection.com";
        private const string OwnerPassword = "!Physmach89";
        private const string OwnerRoleName = "SystemOwner";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = sp.GetRequiredService<RoleManager<Role>>();
            var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                //await db.Database.MigrateAsync();

                // Reference data
                var country = await SeedCountryAsync(db);
                var province = await SeedProvincesAsync(db, country.Id);
                var city = await SeedCitiesAsync(db, province);
                var currency = await SeedCurrenciesAsync(db);

                // Company (head office)
                var harareProv = await db.ka_provinces.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Name == "Harare" && p.CountryId == country.Id)
                    ?? province;

                var harareCity = await db.ka_cities.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Name == "Harare CBD" && c.ProvinceId == harareProv.Id)
                    ?? city;

                var usd = await db.ka_currencies.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Code == "USD") ?? currency;

                var company = await SeedCompanyAsync(db, country.Id, harareProv.Id, harareCity.Id, usd.Id);

                // Identity
                await SeedRolesAsync(roles);
                var owner = await SeedOwnerUserAsync(users, company.Id);
                if (!await users.IsInRoleAsync(owner, OwnerRoleName))
                    await users.AddToRoleAsync(owner, OwnerRoleName);

                // Menu & permission structure
                var moduleMap = await SeedModulesAsync(db);
                var permissionMap = await SeedPermissionsAsync(db);

                await SeedBillingMenusAsync(db, roles);
                permissionMap = await SeedPermissionsAsync(db);

                var ownerRole = await roles.FindByNameAsync(OwnerRoleName);
                if (ownerRole != null)
                {
                    await AssignMenusToRoleAsync(db, ownerRole.Id, moduleMap);
                    await AssignPermissionsToRoleAsync(db, ownerRole.Id, permissionMap);
                }

                logger.LogInformation("✅ Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Database seeding failed: {Message}", ex.Message);
                throw;
            }
        }

        // ── COUNTRY ─────────────────────────────────────────────────────────
        private static async Task<Country> SeedCountryAsync(ApplicationDbContext db)
        {
            var existing = await db.ka_countries.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Code == "ZW");
            if (existing != null) return existing;

            var country = new Country { Code = "ZW", Name = "Zimbabwe" };
            db.ka_countries.Add(country);
            await db.SaveChangesAsync();
            return country;
        }

        // ── PROVINCES ───────────────────────────────────────────────────────
        // Returns the Harare province (used as default for company seeding)
        private static async Task<Province> SeedProvincesAsync(ApplicationDbContext db, int countryId)
        {
            // All 10 Zimbabwe provinces
            var provinceDefs = new[]
            {
                "Harare", "Bulawayo", "Manicaland", "Mashonaland Central",
                "Mashonaland East", "Mashonaland West", "Masvingo",
                "Matabeleland North", "Matabeleland South", "Midlands"
            };

            Province? harare = null;

            foreach (var name in provinceDefs)
            {
                var existing = await db.ka_provinces.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Name == name && p.CountryId == countryId);

                if (existing == null)
                {
                    var prov = new Province { Name = name, CountryId = countryId };
                    db.ka_provinces.Add(prov);
                    await db.SaveChangesAsync();
                    if (name == "Harare") harare = prov;
                }
                else
                {
                    if (name == "Harare") harare = existing;
                }
            }

            // Fallback: fetch Harare if loop didn't capture it
            harare ??= await db.ka_provinces.IgnoreQueryFilters()
                .FirstAsync(p => p.Name == "Harare" && p.CountryId == countryId);

            return harare;
        }

        // ── CITIES ──────────────────────────────────────────────────────────
        // Seeds major cities per province. Returns Harare CBD as default.
        private static async Task<City> SeedCitiesAsync(ApplicationDbContext db, Province harareProv)
        {
            // Map: province name → cities
            var cityDefs = new Dictionary<string, string[]>
            {
                ["Harare"] = new[] { "Harare CBD", "Chitungwiza", "Epworth", "Norton", "Ruwa" },
                ["Bulawayo"] = new[] { "Bulawayo CBD", "Luveve", "Nkulumane", "Pumula", "Tshabalala" },
                ["Manicaland"] = new[] { "Mutare", "Chipinge", "Chimanimani", "Rusape", "Nyanga" },
                ["Mashonaland Central"] = new[] { "Bindura", "Shamva", "Guruve", "Mount Darwin", "Mvurwi" },
                ["Mashonaland East"] = new[] { "Marondera", "Mutoko", "Murewa", "Wedza", "Macheke" },
                ["Mashonaland West"] = new[] { "Chinhoyi", "Karoi", "Kadoma", "Chegutu", "Banket" },
                ["Masvingo"] = new[] { "Masvingo", "Chiredzi", "Gutu", "Bikita", "Zaka" },
                ["Matabeleland North"] = new[] { "Victoria Falls", "Hwange", "Lupane", "Binga", "Tsholotsho" },
                ["Matabeleland South"] = new[] { "Gwanda", "Beitbridge", "Plumtree", "Filabusi", "Esigodini" },
                ["Midlands"] = new[] { "Gweru", "Kwekwe", "Zvishavane", "Shurugwi", "Gokwe" },
            };

            City? harareCbd = null;

            // Load all provinces once
            var provinces = await db.ka_provinces.IgnoreQueryFilters()
                .Where(p => p.CountryId == harareProv.CountryId)
                .ToListAsync();

            foreach (var (provinceName, cities) in cityDefs)
            {
                var prov = provinces.FirstOrDefault(p => p.Name == provinceName);
                if (prov == null) continue;

                foreach (var cityName in cities)
                {
                    var existing = await db.ka_cities.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(c => c.Name == cityName && c.ProvinceId == prov.Id);

                    if (existing == null)
                    {
                        var city = new City { Name = cityName, ProvinceId = prov.Id };
                        db.ka_cities.Add(city);
                        await db.SaveChangesAsync();

                        if (provinceName == "Harare" && cityName == "Harare CBD")
                            harareCbd = city;
                    }
                    else
                    {
                        if (provinceName == "Harare" && cityName == "Harare CBD")
                            harareCbd = existing;
                    }
                }
            }

            harareCbd ??= await db.ka_cities.IgnoreQueryFilters()
                .FirstAsync(c => c.Name == "Harare CBD" && c.ProvinceId == harareProv.Id);

            return harareCbd;
        }

        // ── CURRENCIES ──────────────────────────────────────────────────────
        // Returns USD as the default system currency.
        // CompanyId = 0 is used as a system-level sentinel because Currency
        // extends AuditableEntity which requires CompanyId.
        private static async Task<Currency> SeedCurrenciesAsync(ApplicationDbContext db)
        {
            var currencyDefs = new[]
            {
                ("USD", "US Dollar",          "$"),
                ("ZWG", "Zimbabwe Gold",       "ZWG"),
                ("ZAR", "South African Rand",  "R"),
                ("GBP", "British Pound",       "£"),
                ("EUR", "Euro",                "€"),
            };

            Currency? usd = null;

            foreach (var (code, name, symbol) in currencyDefs)
            {
                var existing = await db.ka_currencies.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (existing == null)
                {
                    var currency = new Currency
                    {
                        Code = code,
                        Name = name,
                        Symbol = symbol,
                        IsActive = true,
                    };
                    db.ka_currencies.Add(currency);
                    await db.SaveChangesAsync();
                    if (code == "USD") usd = currency;
                }
                else
                {
                    if (code == "USD") usd = existing;
                }
            }

            usd ??= await db.ka_currencies.IgnoreQueryFilters()
                .FirstAsync(c => c.Code == "USD");

            return usd;
        }

        // ── COMPANY ──────────────────────────────────────────────────────────
        private static async Task<Company> SeedCompanyAsync(
            ApplicationDbContext db,
            int countryId,
            int provinceId,
            int cityId,
            int currencyId)
        {
            var existing = await db.ka_companies.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.CompanyCode == "TLTW");
            if (existing != null) return existing;

            var company = new Company
            {
                CompanyName = "Dog Section",
                CompanyCode = "TLTW",
                EmailAddress = OwnerEmail,
                PhoneNumber = "263774914150",
                Website = "www.dogsection.coM",
                Address = "Harare CBD, Zimbabwe",
                CountryId = countryId,
                ProvinceId = provinceId,
                CityId = cityId,
                DefaultCurrencyId = currencyId,
                IsActive = true,
                ParentCompanyId = null,
            };

            db.ka_companies.Add(company);
            await db.SaveChangesAsync();
            return company;
        }


        // ── ROLES ────────────────────────────────────────────────────────────
        private static async Task SeedRolesAsync(RoleManager<Role> roles)
        {
            var defs = new[]
            {
                (OwnerRoleName, "System",  "Owner"),
                ("Admin",       "System",  "Admin"),
                ("Dog Handler", "Company", "Handler"),
                ("Manager",     "Company", "Manager"),
                ("Staff",       "Company", "Staff"),
                ("Customer",    "Company", "Customer"),
            };

            foreach (var (name, type, level) in defs)
            {
                if (!await roles.RoleExistsAsync(name))
                    await roles.CreateAsync(new Role { Name = name, Type = type, Level = level });
            }
        }


        // ── SYSTEM OWNER USER ────────────────────────────────────────────────
        private static async Task<ApplicationUser> SeedOwnerUserAsync(
            UserManager<ApplicationUser> users, int companyId)
        {
            var existing = await users.FindByEmailAsync(OwnerEmail);
            if (existing != null) return existing;

            var user = new ApplicationUser
            {
                UserName = OwnerEmail,
                Email = OwnerEmail,
                Name = "Innocent",
                Surname = "Tauzeni",
                PhoneNumber = "263774914150",
                CompanyId = companyId,
                Status = "ACTIVE",
                EmailConfirmed = true,
                RoleName = OwnerRoleName,
                AdminRole = OwnerRoleName,
            };

            var result = await users.CreateAsync(user, OwnerPassword);
            if (!result.Succeeded)
                throw new Exception(
                    $"Failed to create system owner: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            return user;
        }


        // ── MODULES & SUBMODULES ─────────────────────────────────────────────
        private static async Task<Dictionary<string, int>> SeedModulesAsync(ApplicationDbContext db)
        {
            var map = new Dictionary<string, int>();

            var moduleDefs = new[]
            {
                new ModuleDef(
                    Name:        "Configuration",
                    Icon:        "bi-gear-fill",
                    Description: "System configuration and setup",
                    AccessName:  "configuration",
                    SortOrder:   1,
                    Submodules: new[]
                    {
                        new SubDef("System Modules", "bi-grid-3x3-gap",   "SystemModules/Index",   "systemmodules.view", 1),
                        new SubDef("Roles",          "bi-person-badge",   "Roles/Index",           "roles.view",         2),
                        new SubDef("Users",          "bi-people-fill",    "Users/Index",           "users.view",         3),
                        new SubDef("Companies",      "bi-building-fill",  "Companies/Index",       "companies.view",     4),
                        new SubDef("Countries",      "bi-globe2",         "Country/Index",      "country.view",     5),
                        new SubDef("Billing & Email", "bi-envelope-paper", "BillingSettings/Index", "billingsettings.view", 6),
                    }
                ),
               
              
                new ModuleDef(
                    Name:        "Webhooks",
                    Icon:        "bi-broadcast",
                    Description: "Webhook integrations and event subscriptions",
                    AccessName:  "webhooks",
                    SortOrder:   2,
                    Submodules: new[]
                    {
                        new SubDef("Webhooks",     "bi-broadcast",    "Webhooks/Index", "webhooks.view", 1),
                        new SubDef("Webhook Logs", "bi-journal-text", "Webhooks/Logs",  "webhooks.view", 2),
                    }
                ),

                new ModuleDef(
                    Name:        "Dog Training",
                    Icon:        "bi-mortarboard-fill",
                    Description: "Dog training: health assessment, enrolment, handlers, daily logbook, assessments and certificates",
                    AccessName:  "dogtraining",
                    SortOrder:   15,
                    Submodules: new[]
                    {
                        new SubDef("Training Dashboard",     "bi-speedometer2",        "Training/Dashboard",         "training.dashboard.view",   1),
                        new SubDef("Training Customers",     "bi-people-fill",         "Training/Customers",         "training.customer.view",    2),
                        new SubDef("Training Dogs",          "bi-piggy-bank-fill",     "Training/Dogs",              "training.dog.view",         3),
                        new SubDef("Health Assessments",     "bi-heart-pulse-fill",    "Training/HealthAssessments", "training.health.view",      4),
                        new SubDef("Training Enrolments",    "bi-clipboard2-check",    "Training/Enrolments",        "training.enrolment.view",   5),
                        new SubDef("Invoices & Payments",    "bi-receipt",             "Training/Invoices",          "training.invoice.view",     6),
                        new SubDef("Handler Allocation",     "bi-person-check-fill",   "Training/HandlerAllocation", "training.handler.view",     7),
                        new SubDef("Training Programmes",    "bi-journal-richtext",    "Training/Programmes",        "training.programme.view",   8),
                        new SubDef("Daily Training Logbook", "bi-journal-text",        "Training/Logbook",           "training.log.view",         9),
                        new SubDef("Progress Reports",       "bi-graph-up-arrow",      "Training/ProgressReports",   "training.progress.view",    10),
                        new SubDef("Final Assessments",      "bi-award",               "Training/FinalAssessments",  "training.assessment.view",  11),
                        new SubDef("Certificates",           "bi-patch-check-fill",    "Training/Certificates",      "training.certificate.view", 12),
                        new SubDef("Training Reports",       "bi-bar-chart-line-fill", "Training/Reports",           "training.reports.view",     13),
                    }
                ),
            };

            foreach (var def in moduleDefs)
            {
                var module = await db.ka_systemmodules.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m => m.AccessName == def.AccessName);

                if (module == null)
                {
                    module = new SystemModule
                    {
                        Name = def.Name,
                        Icon = def.Icon,
                        Description = def.Description,
                        AccessName = def.AccessName,
                        SortOrder = def.SortOrder,
                        IsActive = true,
                    };
                    db.ka_systemmodules.Add(module);
                    await db.SaveChangesAsync();
                }

                map[def.AccessName] = module.Id;

                foreach (var sub in def.Submodules)
                {
                    var exists = await db.ka_submodules.IgnoreQueryFilters()
                        .AnyAsync(s => s.AccessName == sub.AccessName && s.ModuleId == module.Id);

                    if (!exists)
                    {
                        db.ka_submodules.Add(new Submodule
                        {
                            Name = sub.Name,
                            Icon = sub.Icon,
                            Route = sub.Route,
                            AccessName = sub.AccessName,
                            Order = sub.Order,
                            ModuleId = module.Id,
                        });
                    }
                }

                await db.SaveChangesAsync();
            }

            return map;
        }

        // ── PERMISSIONS ──────────────────────────────────────────────────────
        private static async Task<Dictionary<string, int>> SeedPermissionsAsync(ApplicationDbContext db)
        {
            var map = new Dictionary<string, int>();

            var defs = new (string Name, string DisplayName, string Module)[]
            {
                // System Modules
                ("systemmodules.view",   "View System Modules",  "Configuration"),
                ("systemmodules.create", "Create System Module", "Configuration"),
                ("systemmodules.edit",   "Edit System Module",   "Configuration"),
                ("systemmodules.delete", "Delete System Module", "Configuration"),
                // Roles
                ("roles.view",   "View Roles",  "Configuration"),
                ("roles.create", "Create Role", "Configuration"),
                ("roles.edit",   "Edit Role",   "Configuration"),
                ("roles.delete", "Delete Role", "Configuration"),
                // Users
                ("users.view",   "View Users",  "Configuration"),
                ("users.create", "Create User", "Configuration"),
                ("users.edit",   "Edit User",   "Configuration"),
                ("users.delete", "Delete User", "Configuration"),
                // Companies
                ("companies.view",   "View Companies", "Configuration"),
                ("companies.create", "Create Company", "Configuration"),
                ("companies.edit",   "Edit Company",   "Configuration"),
                ("companies.delete", "Delete Company", "Configuration"),
                // Permissions
                ("permissions.view",   "View Permissions",  "Configuration"),
                ("permissions.create", "Create Permission", "Configuration"),
                ("permissions.edit",   "Edit Permission",   "Configuration"),
                ("permissions.delete", "Delete Permission", "Configuration"),
                // Role Access
                ("roleaccess.view",             "View Role Access",          "Configuration"),
                ("roleaccess.assignmenu",       "Assign Menu to Role",       "Configuration"),
                ("roleaccess.assignpermission", "Assign Permission to Role", "Configuration"),
                // System Actions
                ("systemactions.view",      "View System Actions",       "System Worker"),
                ("systemactions.create",    "Create System Action",      "System Worker"),
                ("systemactions.edit",      "Edit System Action",        "System Worker"),
                ("systemactions.delete",    "Delete System Action",      "System Worker"),
                ("systemactions.assignrole","Assign Role to Action",     "System Worker"),
               
                // country
                ("country.view",   "View Country",   "Country"),
                ("country.create", "Create Country", "Country"),
                ("country.edit",   "Edit Country",   "Country"),
                ("country.delete", "Delete Country", "Country"),


                // Province
                ("province.view",   "View Provinces",   "Province"),
                ("province.create", "Create Province", "Province"),
                ("province.edit",   "Edit Province",   "Province"),
                ("province.delete", "Delete Province", "Province"),

                // City
                ("city.view",   "View Provinces",   "City"),
                ("city.create", "Create Province", "City"),
                ("city.edit",   "Edit Province",   "City"),
                ("city.delete", "Delete Province", "City"),
                
                // Webhooks
                ("webhooks.view",   "View Webhooks",   "Webhooks"),
                ("webhooks.create", "Create Webhook",  "Webhooks"),
                ("webhooks.edit",   "Edit Webhook",    "Webhooks"),
                ("webhooks.delete", "Delete Webhook",  "Webhooks"),
                ("webhooks.manage", "Manage Webhooks", "Webhooks"),

                // Currency
                ("currency.view",   "View Currencies",   "Currency"),
                ("currency.create", "Create Currency",   "Currency"),
                ("currency.edit",   "Edit Currency",     "Currency"),
                ("currency.delete", "Delete Currency",   "Currency"),

                // Exchange Rates
                ("exchangerates.view",   "View Exchange Rates",   "Currency"),
                ("exchangerates.create", "Create Exchange Rate",  "Currency"),
                ("exchangerates.edit",   "Edit Exchange Rate",    "Currency"),
                ("exchangerates.delete", "Delete Exchange Rate",  "Currency"),

                // Payment Methods
                ("paymentmethods.view",   "View Payment Methods",   "Payments"),
                ("paymentmethods.create", "Create Payment Method",  "Payments"),
                ("paymentmethods.edit",   "Edit Payment Method",    "Payments"),
                ("paymentmethods.delete", "Delete Payment Method",  "Payments"),

                // Submodules (used by SystemModules screen)
                ("submodules.view",   "View Submodules",   "Configuration"),
                ("submodules.create", "Create Submodule",  "Configuration"),
                ("submodules.edit",   "Edit Submodule",    "Configuration"),
                ("submodules.delete", "Delete Submodule",  "Configuration"),

                // System Logs (error logs + audit trail) — used by SystemLogsController but
                // never had matching seed entries, so no role (not even SystemOwner) could
                // ever hold these claims until now.
                ("systemworker.view",   "View System Logs",        "System Worker"),
                ("systemworker.manage", "Resolve/Delete Log Entries", "System Worker"),
                ("systemworker.audit",  "View Audit Trail",        "System Worker"),
                // Dog Training
                ("training.dashboard.view", "View Training Dashboard", "Dog Training"),
                ("training.health.view", "View Health Assessments", "Dog Training"),
                ("training.health.create", "Create Health Assessments", "Dog Training"),
                ("training.health.approve", "Approve Health Assessments", "Dog Training"),
                ("training.enrolment.view", "View Training Enrolments", "Dog Training"),
                ("training.enrolment.create", "Create Training Enrolments", "Dog Training"),
                ("training.customer.view", "View Training Customers", "Dog Training"),
                ("training.customer.manage", "Manage Training Customers", "Dog Training"),
                ("training.dog.view", "View Training Dogs", "Dog Training"),
                ("training.dog.manage", "Manage Training Dogs", "Dog Training"),
                ("training.invoice.view", "View Training Invoices", "Dog Training"),
                ("training.invoice.generate", "Generate / Cancel Training Invoices", "Dog Training"),
                ("training.payment.record", "Record Training Payments", "Dog Training"),
                ("training.handler.view", "View Handler Allocation", "Dog Training"),
                ("training.handler.assign", "Assign Handlers & Activate Training", "Dog Training"),
                ("training.programme.view", "View Training Programmes", "Dog Training"),
                ("training.programme.manage", "Manage Training Programmes & Templates", "Dog Training"),
                ("training.log.view", "View Daily Training Logbook", "Dog Training"),
                ("training.log.record", "Record Daily Training", "Dog Training"),
                ("training.progress.view", "View Progress Reports", "Dog Training"),
                ("training.progress.submit", "Submit Progress Reports", "Dog Training"),
                ("training.progress.review", "Review & Approve Progress Reports", "Dog Training"),
                ("training.assessment.view", "View Final Assessments", "Dog Training"),
                ("training.assessment.conduct", "Conduct Final Assessments", "Dog Training"),
                ("training.assessment.approve", "Approve Training Completion", "Dog Training"),
                ("training.certificate.view", "View Training Certificates", "Dog Training"),
                ("training.certificate.issue", "Issue Training Certificates", "Dog Training"),
                ("training.reports.view", "View Training Reports", "Dog Training"),
                ("training.manage.all", "Manage All Dogs in Training (not only assigned)", "Dog Training"),
                ("training.payment.verify", "Confirm / Reject Training Bank & Cheque Payments", "Dog Training"),
                ("training.invoice.email", "Email Training Invoices", "Dog Training"),
                // Billing: customer companies, payment verification, email, billing settings
                ("customercompany.view", "View Customer Companies", "Managements"),
                ("customercompany.create", "Create Customer Company", "Managements"),
                ("customercompany.edit", "Edit Customer Company", "Managements"),
                ("customercompany.delete", "Delete Customer Company", "Managements"),
                ("invoice.verifypayment", "Confirm / Reject Bank & Cheque Payments", "Finance"),
                ("invoice.email", "Email Invoices", "Finance"),
                ("billingsettings.view", "View Billing & Email Settings", "Configuration"),
                ("billingsettings.edit", "Edit Billing & Email Settings", "Configuration"),
            };

            foreach (var (name, displayName, module) in defs)
            {
                var existing = await db.ka_permissions.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Name == name);

                if (existing == null)
                {
                    var perm = new Permission { Name = name, DisplayName = displayName, Module = module };
                    db.ka_permissions.Add(perm);
                    await db.SaveChangesAsync();
                    map[name] = perm.Id;
                }
                else
                {
                    map[name] = existing.Id;
                }
            }

            return map;
        }

        // ── BILLING MENUS (Customer Companies sits next to Customers) + sensible role defaults ───────
        private static async Task SeedBillingMenusAsync(ApplicationDbContext db, RoleManager<Role> roles)
        {
            // Customer Companies lives in whichever module already holds the Customers menu.
            var customersSub = await db.ka_submodules.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.AccessName == "customer.view");
            if (customersSub != null &&
                !await db.ka_submodules.IgnoreQueryFilters().AnyAsync(s => s.AccessName == "customercompany.view"))
            {
                db.ka_submodules.Add(new Submodule
                {
                    Name = "Customer Companies", Icon = "bi-buildings", Route = "CustomerCompanies",
                    AccessName = "customercompany.view", Order = customersSub.Order + 1, ModuleId = customersSub.ModuleId,
                });
                await db.SaveChangesAsync();
            }

            // Make sure the permissions exist before granting them.
            await SeedPermissionsAsync(db);
            var perms = (await db.ka_permissions.IgnoreQueryFilters().Where(p => !p.IsDeleted).Select(p => new { p.Name, p.Id }).ToListAsync())
                .GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.Min(x => x.Id));

            async Task Grant(string roleName, params string[] names)
            {
                var role = await roles.FindByNameAsync(roleName);
                if (role == null) return;
                foreach (var n in names)
                {
                    if (!perms.TryGetValue(n, out var pid)) continue;
                    if (!await db.ka_rolepermissions.IgnoreQueryFilters().AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == pid))
                        db.ka_rolepermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = pid });
                }
                await db.SaveChangesAsync();
            }

            var all = new[]
            {
                "customercompany.view", "customercompany.create", "customercompany.edit", "customercompany.delete",
                "invoice.verifypayment", "invoice.email", "billingsettings.view", "billingsettings.edit",
                "training.payment.verify", "training.invoice.email",
            };
            await Grant(OwnerRoleName, all);
            await Grant("Admin", all);

            // Anyone who could already manage customers / invoices keeps working without a manual re-tick.
            var map = new (string Has, string Gets)[]
            {
                ("customer.view", "customercompany.view"), ("customer.create", "customercompany.create"),
                ("customer.edit", "customercompany.edit"), ("customer.delete", "customercompany.delete"),
                ("invoice.edit", "invoice.email"), ("training.invoice.generate", "training.invoice.email"),
            };
            foreach (var (has, gets) in map)
            {
                if (!perms.TryGetValue(has, out var hasId) || !perms.TryGetValue(gets, out var getId)) continue;
                var roleIds = await db.ka_rolepermissions.IgnoreQueryFilters().Where(rp => rp.PermissionId == hasId).Select(rp => rp.RoleId).Distinct().ToListAsync();
                foreach (var rid in roleIds)
                    if (!await db.ka_rolepermissions.IgnoreQueryFilters().AnyAsync(rp => rp.RoleId == rid && rp.PermissionId == getId))
                        db.ka_rolepermissions.Add(new RolePermission { RoleId = rid, PermissionId = getId });
                await db.SaveChangesAsync();
            }
        }

        // ── ASSIGN MENUS TO ROLE ─────────────────────────────────────────────
        private static async Task AssignMenusToRoleAsync(
            ApplicationDbContext db, string roleId, Dictionary<string, int> moduleMap)
        {
            foreach (var moduleId in moduleMap.Values)
            {
                var exists = await db.ka_rolemenus.IgnoreQueryFilters()
                    .AnyAsync(rm => rm.RoleId == roleId && rm.SystemModuleId == moduleId);

                if (!exists)
                    db.ka_rolemenus.Add(new RoleMenu { RoleId = roleId, SystemModuleId = moduleId });
            }

            await db.SaveChangesAsync();
        }

        // ── ASSIGN PERMISSIONS TO ROLE ───────────────────────────────────────
        private static async Task AssignPermissionsToRoleAsync(
            ApplicationDbContext db, string roleId, Dictionary<string, int> permissionMap)
        {
            foreach (var permId in permissionMap.Values)
            {
                var exists = await db.ka_rolepermissions.IgnoreQueryFilters()
                    .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permId);

                if (!exists)
                    db.ka_rolepermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
            }

            await db.SaveChangesAsync();
        }

        // ── LOCAL RECORD HELPERS ─────────────────────────────────────────────
        private record ModuleDef(
            string Name, string Icon, string Description,
            string AccessName, int SortOrder, SubDef[] Submodules);

        private record SubDef(
            string Name, string Icon, string Route,
            string AccessName, int Order);









    }
}
