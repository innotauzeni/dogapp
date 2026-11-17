using Dog.Core.Entities;

namespace Dog.Application.Contracts
{
    /// <summary>
    /// Service for sending WhatsApp notifications for various dog management activities.
    /// Notifications are sent to configured admin/staff numbers in user-friendly format.
    /// </summary>
    public interface IWhatsAppNotificationService
    {
        Task NotifyDogAddedAsync(Dogtbl dog, string companyName);
        Task NotifyDogFedAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName);
        Task NotifyVaccinationAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName);
        Task NotifyMatingAsync(Dogtbl dog, DogCareRecord careRecord, string partnerName, string handlerName);
        Task NotifyIllnessAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName);
        Task NotifyInjuryAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName);
        Task NotifyBiteIncidentAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName);
        Task NotifyDewormingAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName);
        Task NotifyLitterAsync(Dogtbl dog, DogCareRecord careRecord, string handlerName);
        Task NotifyGeneralCareAsync(Dogtbl dog, DogCareRecord careRecord, string eventType, string handlerName);
    }
}
