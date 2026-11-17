namespace Dog.Portal.Models
{
    /// <summary>
    /// Dynamic content for the public welcome / landing page. Every field has sensible defaults.
    /// The whole model can be overridden at runtime from the DB (ka_appsettings key "Landing:Json").
    /// </summary>
    public class LandingViewModel
    {
        public string BrandName { get; set; } = "Dog Section";
        public string? LogoUrl { get; set; } = "/logo.png";
        public string? FaviconUrl { get; set; }
        public string Accent { get; set; } = "#dc2626";     // red-600 for dog theme
        public string Accent2 { get; set; } = "#059669";    // emerald-600

        // Hero
        public bool HeroEnabled { get; set; } = true;
        public string HeroBadge { get; set; } = "Professional Dog Breeding Management";
        public string HeroTitle { get; set; } = "Manage Your Dog Breeding Operation Smarter";
        public string HeroSubtitle { get; set; } =
            "Dog Section is the all-in-one platform for dog breeders. Track pedigrees, manage vaccinations, monitor matings, record puppy care, and build your breeding empire — all in one intelligent system.";
        public string PrimaryCtaText { get; set; } = "Get Started";
        public string SecondaryCtaText { get; set; } = "Learn More";
        public string HeroSecondaryText { get; set; } = "See what's inside";
        public string HeroTitleColor { get; set; } = "#1e293b";
        public string HeroSubtitleColor { get; set; } = "#64748b";
        public string HeroOverlayOpacity { get; set; } = "0.4";
        public List<HeroImage> HeroImages { get; set; } = [];

        // Hero mockup
        public string MockupTitle { get; set; } = "Dog Section Dashboard";
        public string MockupStatLabel { get; set; } = "Active Dogs";
        public string MockupStatValue { get; set; } = "48";
        public List<MockupCard> MockupCards { get; set; } = [];

        // Trust strip
        public List<TrustItem> TrustStrip { get; set; } =
        [
            new("bi-shield-check-fill",  "Secure & Encrypted"),
            new("bi-cloud-check-fill",   "Cloud Hosted"),
            new("bi-people-fill",        "Multi-user & Branch"),
            new("bi-arrow-repeat",       "Automated Backups"),
            new("bi-graph-up-arrow",     "Smart Analytics"),
        ];

        public List<Stat> Stats { get; set; } =
        [
            new("1000+", "Breeders Using"),
            new("50K+", "Dogs Tracked"),
            new("99.9%", "Uptime"),
            new("24/7", "Support"),
        ];

        // Features
        public string FeaturesHeading { get; set; } = "Complete Dog Breeding Management";
        public string FeaturesSub { get; set; } =
            "Everything professional breeders need, from pedigree tracking to vaccination schedules, mating records, and puppy health monitoring.";

        public List<Feature> Features { get; set; } =
        [
            new("bi-diagram-3",       "Pedigree Tracking",     "Track bloodlines, sires, dams and family trees. Build rich genealogies and avoid genetic issues."),
            new("bi-shield-plus",     "Vaccination Records",   "Never miss a vaccination. Track schedules, set reminders and generate compliance reports."),
            new("bi-heart-pulse",     "Health Monitoring",     "Record illnesses, injuries, treatments and health alerts. Keep detailed medical histories."),
            new("bi-calendar-heart",  "Mating Management",     "Plan matings, track cycles, record mating dates and expected litter dates with ease."),
            new("bi-baby-carriage",   "Puppy Care Tracking",   "Monitor puppies from birth through registration. Track vaccines, milestones and development."),
            new("bi-people",          "User Management",       "Add handlers, veterinarians and staff. Control permissions and track who did what."),
            new("bi-building",        "Multi-Branch Support",  "Manage multiple locations, branches and kennels from one dashboard."),
            new("bi-file-earmark",    "Reports & Analytics",   "Generate pedigree certificates, vaccination reports, breeding analytics and more."),
            new("bi-whatsapp",        "WhatsApp Integration",  "Check dog details, record activities and manage operations via WhatsApp."),
            new("bi-phone-landscape", "Mobile App",            "Full iOS and Android apps for on-the-go breeding management."),
            new("bi-lock-fill",       "Data Security",         "Enterprise-grade encryption, backups and compliance built-in."),
            new("bi-lightning-charge","Real-time Sync",        "All devices sync instantly. Your data is always current."),
        ];

        // How it works
        public string StepsEyebrow { get; set; } = "Getting Started";
        public string StepsHeading { get; set; } = "Set up your breeding operation in minutes";
        public List<Step> Steps { get; set; } =
        [
            new("1", "Sign Up & Configure",     "Create your account, set up your kennel/branch profile and add your team members."),
            new("2", "Add Your Dogs",           "Register your existing dogs and import their pedigrees and vaccination history."),
            new("3", "Start Tracking",          "Begin recording matings, puppies, vaccinations and health events in real-time."),
            new("4", "Grow Your Breeding",      "Use analytics and reports to make better breeding decisions and scale your operation."),
        ];

        public string IndustriesHeading { get; set; } = "Built for All Breeding Types";
        public string IndustriesSub { get; set; } =
            "Whether you're a single-breeder enthusiast or a multi-branch commercial operation, Dog Section scales with you.";
        public List<string> Industries { get; set; } =
        [
            "Labrador Breeders", "German Shepherd Breeders", "Mixed Breed Rescues", "Show Dog Breeders",
            "Service Dog Organizations", "Boarding Facilities", "Veterinary Clinics", "Kennel Clubs",
        ];

        // Quick links
        public List<QuickLink> QuickLinks { get; set; } =
        [
            new("bi-download",                "Download Mobile App",    "/app"),
            new("bi-chat-dots-fill",          "WhatsApp Bot",           "/whatsapp"),
            new("bi-book-fill",               "Documentation",          "/docs"),
            new("bi-headset",                 "Support & Contact",      "/support"),
        ];

        // Testimonials
        public string TestimonialsHeading { get; set; } = "Trusted by Professional Breeders";
        public List<Testimonial> Testimonials { get; set; } =
        [
            new("Dog Section transformed how I manage my breeding program. Never missed a vaccination again.", "Sarah Johnson", "Premium Labrador Breeder"),
            new("The pedigree tracking is incredible. My clients love the detailed lineage reports.", "Michael Chen", "Show Dog Breeder"),
            new("Managing 3 branches was chaotic until Dog Section. Now everything syncs perfectly.", "Emma Williams", "Canine Care Center"),
        ];

        // CTA + Contact
        public string CtaHeading { get; set; } = "Ready to Transform Your Breeding Operation?";
        public string CtaSub { get; set; } = "Join professional breeders who've switched to Dog Section. Start free today — no credit card required.";
        public string Phone { get; set; } = "+1 (555) 123-4567";
        public string Email { get; set; } = "support@dogsection.com";
        public string Address { get; set; } = "Harare, Zimbabwe";

        // APK Download
        public string ApkHeading { get; set; } = "Download the Mobile App";
        public string ApkSubheading { get; set; } = "Available for iOS and Android";
        public string ApkUrl { get; set; } = "/downloads/dogsection.apk";
        public string IosUrl { get; set; } = "https://apps.apple.com/dogsection";
        public string AndroidUrl { get; set; } = "https://play.google.com/store/apps/details?id=com.dogsection";
        public List<ApkFile> ApkFiles { get; set; } = [];

        // Footer
        public string FooterLinksHeading { get; set; } = "Quick Links";
        public string FooterContactHeading { get; set; } = "Get In Touch";
        public string FooterTagline { get; set; } = "Professional Dog Breeding Management 🐾";

        public record Stat(string Value, string Label);
        public record Feature(string Icon, string Title, string Description);
        public record Step(string Number, string Title, string Description);
        public record Testimonial(string Quote, string Name, string Company);
        public record TrustItem(string Icon, string Text);
        public record MockupCard(string Icon, string Text);
        public record QuickLink(string Icon, string Label, string Url);
        public record ApkFile(string Name, string Url);
        public record HeroImage(string Url, string Title = "");
    }
}
