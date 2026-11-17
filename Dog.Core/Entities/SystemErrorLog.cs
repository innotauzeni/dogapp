
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations;

namespace Dog.Core.Entities
{
    // Extends AuditableBaseEntity (NOT AuditableEntity) — system-level logs
    // must work before any company exists (e.g. during onboarding).
    public class SystemErrorLog : AuditableBaseEntity
    {
        [Required]
        public string? Message { get; set; }

        public string? StackTrace { get; set; }

        public string? InnerException { get; set; }

        public string? Path { get; set; }

        public string? Method { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string? RequestBody { get; set; }

        public string? QueryString { get; set; }

        public string? Headers { get; set; }

        [Required]
        public string? Severity { get; set; } // Critical, Error, Warning, Information

        [Required]
        public string? Source { get; set; } // Controller, Service, Repository, etc.

        public string? ExceptionType { get; set; }

        public string? MachineName { get; set; }

        public string? ApplicationVersion { get; set; }

        public string? Environment { get; set; } // Development, Staging, Production

        public bool IsResolved { get; set; } = false;

        public string? Resolution { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public Guid? ResolvedBy { get; set; }

        /// Whether a WhatsApp alert was already sent for this log (Critical severity
        /// only) — prevents the alert background job from re-notifying on every poll.
        public bool IsAlerted { get; set; } = false;

        public DateTime? AlertedAt { get; set; }
    }
}
