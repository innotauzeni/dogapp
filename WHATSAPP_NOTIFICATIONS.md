# WhatsApp Notifications Feature

## Overview
The system now sends automatic WhatsApp notifications to configured phone numbers whenever important activities occur in the dog management system.

## Configuration

Add or update the following section in your `appsettings.json`:

```json
"NotifyWhatsapp": {
    "WhatsAppNumbers": "263773512690,263774914150,263772539121,263785764731"
}
```

### Configuration Details:
- **WhatsAppNumbers**: Comma-separated list of phone numbers (with country code) that will receive notifications
- Numbers should include country code (e.g., 263 for Zimbabwe)
- Spaces around commas are automatically trimmed
- Empty or whitespace-only numbers are ignored

## Supported Notifications

The system sends user-friendly WhatsApp messages for the following activities:

### 1. **Dog Added**
When a new dog is registered in the system.
- Includes: Name, Breed, Gender, Date of Birth, Microchip, Company

### 2. **Dog Fed**
When a handler records feeding a dog.
- Includes: Dog name, Time, Quantity, Handler name, Notes

### 3. **Vaccination**
When a dog receives a vaccination.
- Includes: Dog name, Date, Vaccine name, Batch number, Next due date, Handler name

### 4. **Mating**
When a mating event is recorded.
- Includes: Dog name, Date, Partner name, Duration, Handler name

### 5. **Illness**
When a dog is reported ill.
- Includes: Dog name, Date, Symptoms, Treatment, Attention status, Handler name

### 6. **Injury**
When a dog is injured.
- Includes: Dog name, Date, Severity, Treatment status, Details, Handler name

### 7. **Bite Incident**
When a dog bites someone.
- Includes: Dog name, Date, Severity, Reported to, Details, Handler name

### 8. **Deworming**
When deworming medication is administered.
- Includes: Dog name, Date, Product, Next due date, Handler name

### 9. **Litter/Birth**
When puppies are born.
- Includes: Mother name, Date, Outcome, Details, Handler name

### 10. **Other Activities**
Any other care activities (Heat, Other, etc.)
- Includes: Dog name, Date, Details, Handler name, Attention status

## Message Format

All messages are formatted in a user-friendly, non-technical way:
- Use emojis for quick visual identification (🐕, 🍖, 💉, etc.)
- **Bold headers** for important information
- Clear labels for each field
- Dates formatted as "dd MMM yyyy" (e.g., "25 Jan 2026")
- Status indicators (✅, ⚠️, 🚨) for quick attention

## Technical Implementation

### Services
- **IWhatsAppNotificationService**: Interface defined in `Dog.Application.Contracts`
- **WhatsAppNotificationService**: Implementation in `Dog.Api.Services`

### How It Works
1. When an activity is recorded (e.g., feeding, vaccination), the handler saves the record to the database
2. After successful save, a background task sends WhatsApp notifications asynchronously
3. Notifications are sent to all configured phone numbers
4. If notification sending fails, it fails silently and doesn't block the main operation
5. Individual notification failures are logged but don't affect other numbers

### Affected Handlers
- **CreateAnimalHandler**: Sends notification when a new dog is added
- **AddDogCareRecordHandler**: Sends notifications for all care activities (feeding, vaccination, illness, etc.)

## Error Handling

- Notification failures do not block the main operations
- If WhatsApp credentials are not configured, warnings are logged
- If no notification numbers are configured, notifications are skipped silently
- Individual number failures don't affect notifications to other numbers
- All errors are logged by the underlying WhatsAppBotService

## Testing

To test the notification system:

1. Ensure WhatsApp credentials are configured in `appsettings.json`:
   ```json
   "WhatsApp": {
       "AccessToken": "your_access_token",
       "PhoneNumberId": "your_phone_number_id"
   }
   ```

2. Add test numbers to the notification list:
   ```json
   "NotifyWhatsapp": {
       "WhatsAppNumbers": "your_test_number"
   }
   ```

3. Perform activities:
   - Add a new dog through the portal or API
   - Record feeding through the handler interface
   - Record vaccination, mating, or other care activities

4. Check the configured WhatsApp numbers for notifications

## Disabling Notifications

To temporarily disable notifications without removing the code:
- Remove all numbers from the `WhatsAppNumbers` configuration
- Or set it to an empty string: `"WhatsAppNumbers": ""`

## Future Enhancements

Potential improvements for future versions:
- User preferences for which notifications to receive
- Different notification groups for different activities
- Notification scheduling (e.g., only during business hours)
- SMS fallback when WhatsApp is unavailable
- Rich media notifications (images of dogs)
- Interactive notifications with action buttons
