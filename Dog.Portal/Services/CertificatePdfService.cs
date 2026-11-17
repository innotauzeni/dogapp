using Dog.Application.Common.Pdf;
using Dog.Portal.Models;

namespace Dog.Portal.Services
{
    /// <summary>Web dog registration certificate. Renders the shared <see cref="DogCertificateLayout"/>
    /// — the same design as the mobile app's "Certificate of Registration".</summary>
    public static class CertificatePdfService
    {
        public static byte[] Build(DogProfileDto profile, CompanyDto? company, byte[]? logo, byte[]? dogPhoto)
        {
            var d = profile.Dog;
            var data = new DogCertificateData
            {
                DogId = d.Id,
                CompanyId = d.CompanyId,
                Name = d.Name,
                Breed = d.Breed,
                Gender = d.Gender,
                Color = d.Color,
                DateOfBirth = d.DateOfBirth,
                DateOfDeath = d.DateOfDeath,
                MicrochipNumber = d.MicrochipNumber,
                SireName = d.SireName,
                DamName = d.DamName,
                CompanyName = company?.CompanyName ?? d.CompanyName,
                CompanyContact = string.Join("  •  ", new[] { company?.PhoneNumber, company?.EmailAddress }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
            };
            return DogCertificateLayout.Build(data, logo, dogPhoto);
        }
    }
}
