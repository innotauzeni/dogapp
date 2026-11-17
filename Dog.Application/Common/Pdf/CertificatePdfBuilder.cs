using Dog.Application.Features.Animals.DTOs;
using Dog.Application.Features.Companies.DTOs;

namespace Dog.Application.Common.Pdf
{
    /// <summary>Dog registration certificate used by the API (WhatsApp bot). Renders the shared
    /// <see cref="DogCertificateLayout"/> so it matches the mobile app and web portal.</summary>
    public static class CertificatePdfBuilder
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
