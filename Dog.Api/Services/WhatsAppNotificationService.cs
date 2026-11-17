using Dog.Core.Entities;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace Dog.Api.Services
{
    public class WhatsAppNotificationService : IWhatsAppNotificationService
    {
        private readonly IWhatsAppBotService _whatsAppBot;
        private readonly IConfiguration _config;
        private readonly List<string> _notificationNumbers;

        public WhatsAppNotificationService(IWhatsAppBotService whatsAppBot, IConfiguration config)
        {
            _whatsAppBot = whatsAppBot;
            _config = config;
            
            // Parse notification numbers from config
            var numbersConfig = _config["NotifyWhatsapp:WhatsAppNumbers"] ?? "";
            _notificationNumbers = numbersConfig
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
        }

        public async Task NotifyDogAddedAsync(Dogtbl dog, string companyName)
        {
            if (!_notificationNumbers.Any()) return;

            var gender = dog.Gender ?? "Unknown";
            var breed = dog.Breed ?? "Mixed Breed";
            var dob = dog.DateOfBirth != default(DateTime) ? dog.DateOfBirth.ToString("dd MMM yyyy") : "Not specified";
            var microchip = !string.IsNullOrWhiteSpace(dog.MicrochipNumber) ? dog.MicrochipNumber : "Not yet assigned";

            var message = $"🐕 *NEW DOG REGISTERED*\n\n" +
                         $"📋 *Name:* {dog.Name}\n" +
                         $"🎨 *Breed:* {breed}\n" +
                         $"⚧ *Gender:* {gender}\n" +
                         $"🎂 *Date of Birth:* {dob}\n" +
                         $"💳 *Microchip:* {microchip}\n" +
                         $"🏢 *Company:* {companyName}\n\n" +
                         $"✅ The dog has been successfully added to the system.";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyDogFedAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var quantity = !string.IsNullOrWhiteSpace(careRecord.QuantityOrOutcome) 
                ? careRecord.QuantityOrOutcome 
                : "Standard portion";

            var message = $"🍖 *DOG FEEDING RECORDED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Time:* {timeDisplay}\n" +
                         $"🥘 *Quantity:* {quantity}\n" +
                         $"👤 *Fed by:* {handlerName}\n";

            if (!string.IsNullOrWhiteSpace(careRecord.Details))
            {
                message += $"📝 *Notes:* {careRecord.Details}\n";
            }

            message += $"\n✅ Feeding recorded successfully.";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyVaccinationAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var vaccineName = careRecord.VaccineName ?? "Vaccine";
            var batch = !string.IsNullOrWhiteSpace(careRecord.VaccineBatch) 
                ? careRecord.VaccineBatch 
                : "Not specified";

            var message = $"💉 *VACCINATION RECORDED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"💊 *Vaccine:* {vaccineName}\n" +
                         $"📦 *Batch:* {batch}\n" +
                         $"👤 *Administered by:* {handlerName}\n";

            if (careRecord.VaccineNextDue.HasValue)
            {
                var nextDue = careRecord.VaccineNextDue.Value.ToString("dd MMM yyyy");
                message += $"📅 *Next due:* {nextDue}\n";
            }

            if (!string.IsNullOrWhiteSpace(careRecord.VaccineNotes))
            {
                message += $"📝 *Notes:* {careRecord.VaccineNotes}\n";
            }

            message += $"\n✅ Vaccination recorded successfully.";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyMatingAsync(Dogtbl dog, DogCareRecord careRecord, string partnerName, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var partner = !string.IsNullOrWhiteSpace(partnerName) ? partnerName : "Unknown partner";
            var duration = !string.IsNullOrWhiteSpace(careRecord.MatingDuration) 
                ? careRecord.MatingDuration 
                : "Not specified";

            var message = $"💕 *MATING RECORDED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"🐾 *Partner:* {partner}\n" +
                         $"⏱ *Duration:* {duration}\n" +
                         $"👤 *Recorded by:* {handlerName}\n";

            if (!string.IsNullOrWhiteSpace(careRecord.Details))
            {
                message += $"📝 *Notes:* {careRecord.Details}\n";
            }

            message += $"\n✅ Mating event recorded successfully.";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyIllnessAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var needsAttention = careRecord.RequiresAttention ? "⚠️ *YES - REQUIRES ATTENTION*" : "Monitoring";

            var message = $"🤒 *ILLNESS REPORTED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"🚨 *Attention needed:* {needsAttention}\n" +
                         $"👤 *Reported by:* {handlerName}\n";

            if (!string.IsNullOrWhiteSpace(careRecord.Details))
            {
                message += $"📝 *Symptoms:* {careRecord.Details}\n";
            }

            if (!string.IsNullOrWhiteSpace(careRecord.QuantityOrOutcome))
            {
                message += $"💊 *Treatment/Outcome:* {careRecord.QuantityOrOutcome}\n";
            }

            message += $"\n✅ Illness recorded in the system.";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyInjuryAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var severity = !string.IsNullOrWhiteSpace(careRecord.InjurySeverity) 
                ? careRecord.InjurySeverity 
                : "Not specified";
            var treated = careRecord.InjuryTreated ? "Yes" : "No";

            var message = $"🩹 *INJURY REPORTED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"⚠️ *Severity:* {severity}\n" +
                         $"🏥 *Treated:* {treated}\n" +
                         $"👤 *Reported by:* {handlerName}\n";

            if (!string.IsNullOrWhiteSpace(careRecord.InjuryDetails))
            {
                message += $"📝 *Details:* {careRecord.InjuryDetails}\n";
            }

            if (careRecord.RequiresAttention)
            {
                message += $"\n⚠️ *REQUIRES IMMEDIATE ATTENTION*\n";
            }

            message += $"\n✅ Injury recorded in the system.";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyBiteIncidentAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var severity = !string.IsNullOrWhiteSpace(careRecord.BiteSeverity) 
                ? careRecord.BiteSeverity 
                : "Not specified";
            var reportedTo = !string.IsNullOrWhiteSpace(careRecord.BiteReportedTo) 
                ? careRecord.BiteReportedTo 
                : "Not reported";

            var message = $"🚨 *BITE INCIDENT REPORTED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"⚠️ *Severity:* {severity}\n" +
                         $"📋 *Reported to:* {reportedTo}\n" +
                         $"👤 *Recorded by:* {handlerName}\n";

            if (!string.IsNullOrWhiteSpace(careRecord.BiteDetails))
            {
                message += $"📝 *Details:* {careRecord.BiteDetails}\n";
            }

            message += $"\n⚠️ *IMPORTANT: Review and follow up on this incident.*";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyDewormingAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var product = !string.IsNullOrWhiteSpace(careRecord.DewormingProduct) 
                ? careRecord.DewormingProduct 
                : "Deworming medication";

            var message = $"💊 *DEWORMING RECORDED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"💉 *Product:* {product}\n" +
                         $"👤 *Administered by:* {handlerName}\n";

            if (careRecord.DewormingNextDue.HasValue)
            {
                var nextDue = careRecord.DewormingNextDue.Value.ToString("dd MMM yyyy");
                message += $"📅 *Next due:* {nextDue}\n";
            }

            if (!string.IsNullOrWhiteSpace(careRecord.Details))
            {
                message += $"📝 *Notes:* {careRecord.Details}\n";
            }

            message += $"\n✅ Deworming recorded successfully.";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyLitterAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var outcome = !string.IsNullOrWhiteSpace(careRecord.QuantityOrOutcome) 
                ? careRecord.QuantityOrOutcome 
                : "Puppies born";

            var message = $"🐶 *LITTER BIRTH RECORDED*\n\n" +
                         $"🐕 *Mother:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"👶 *Outcome:* {outcome}\n" +
                         $"👤 *Recorded by:* {handlerName}\n";

            if (!string.IsNullOrWhiteSpace(careRecord.Details))
            {
                message += $"📝 *Details:* {careRecord.Details}\n";
            }

            message += $"\n🎉 Congratulations on the new puppies!";

            await SendToAllNotificationNumbers(message);
        }

        public async Task NotifyGeneralCareAsync(Dogtbl dog, DogCareRecord careRecord, string eventType, string handlerName)
        {
            if (!_notificationNumbers.Any()) return;

            var timeDisplay = EventTime.When(careRecord.OccurredOn);
            var emoji = GetEmojiForEventType(eventType);

            var message = $"{emoji} *{eventType.ToUpper()} RECORDED*\n\n" +
                         $"🐕 *Dog:* {dog.Name}\n" +
                         $"🕐 *Date:* {timeDisplay}\n" +
                         $"👤 *Recorded by:* {handlerName}\n";

            if (!string.IsNullOrWhiteSpace(careRecord.Details))
            {
                message += $"📝 *Details:* {careRecord.Details}\n";
            }

            if (!string.IsNullOrWhiteSpace(careRecord.QuantityOrOutcome))
            {
                message += $"ℹ️ *Info:* {careRecord.QuantityOrOutcome}\n";
            }

            if (careRecord.RequiresAttention)
            {
                message += $"\n⚠️ *REQUIRES ATTENTION*\n";
            }

            message += $"\n✅ Activity recorded successfully.";

            await SendToAllNotificationNumbers(message);
        }

        private async Task SendToAllNotificationNumbers(string message)
        {
            foreach (var number in _notificationNumbers)
            {
                try
                {
                    await _whatsAppBot.SendMessageAsync(number, message);
                }
                catch (Exception)
                {
                    // Log error but continue sending to other numbers
                    // Error will be logged by WhatsAppBotService
                }
            }
        }

        private string GetEmojiForEventType(string eventType)
        {
            return eventType?.ToLower() switch
            {
                "feeding" => "🍖",
                "vaccinated" => "💉",
                "illness" => "🤒",
                "injured" => "🩹",
                "bite someone" => "🚨",
                "heat" => "🔥",
                "mating" => "💕",
                "birth" => "🐶",
                "litter" => "👶",
                "deworming" => "💊",
                _ => "📋"
            };
        }
    }
}
