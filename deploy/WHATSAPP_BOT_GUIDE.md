# 🤖 WhatsApp Bot Complete Guide

## Overview
The WhatsApp bot allows users to manage dogs and breeding operations directly via WhatsApp messages. No app installation needed - just send messages to your business WhatsApp number.

---

## 🎯 Available Commands

### **1. Help Menu**
```
help
```
Shows all available commands and usage examples.

---

### **2. List All Dogs**
```
dogs
```
**Response:**
```
🐕 Your Dogs:
1. Buddy (Labrador) - Active ✓
2. Luna (Golden Retriever) - Active ✓
3. Max (German Shepherd) - Deployed 📍

Type 'view 1' to see details
```

---

### **3. View Dog Details** ⭐ NEW
```
view [dogId]
```
or
```
detail [dogId]
```

**Example:**
```
view 1
```

**Response:**
```
🐕 Dog Details - ID: 1

Name: Buddy
Breed: Labrador Retriever
Gender: Male
DOB: 2020-03-15 (4 years old)
Microchip: 981000123456789900
Color: Golden

📊 Status:
• Status: Active ✅
• Owner: John Smith
• Handler: Sarah Johnson
• Location: Main Branch

💉 Vaccinations:
• Rabies (2024-12-10) ⚠️ Due in 3 days
• DHPP (2024-12-10)
• Bordetella (2024-12-10)

👨‍👩‍👧‍👦 Family:
• Sire: Champion Max (ID: 5)
• Dam: Lady Luna (ID: 8)
• Offspring: 3 puppies

❤️ Recent Activity:
• Feeding (today, 8:00 AM)
• Play session (yesterday)
• Vaccination (Dec 10)
```

---

### **4. Record Care Activity**
```
record [dogId] [activity] [note]
```

**Available Activities:**
- feeding
- vaccine
- illness
- injured
- bite
- heat
- mating
- birth
- other

**Examples:**
```
record 1 feeding Good appetite
record 2 vaccine Rabies booster
record 3 illness Slight cough, monitoring
record 1 mating Mated with Luna (ID: 2)
```

**Response:**
```
✅ FEEDING recorded for dog 1
Note: Good appetite
```

---

### **5. Vaccines Due**
```
vaccine
```

**Response:**
```
💉 Vaccination Due Soon:
🔴 Buddy (Labrador) - Rabies (due in 3 days)
🟡 Luna (Golden Retriever) - DHPP (due in 7 days)
🟢 Max (German Shepherd) - All current

Reply 'record [dogId] vaccine' to record vaccination
Reply 'view [dogId]' for full vaccine history
```

---

### **6. Breeding Status**
```
breeding
```

**Response:**
```
❤️ Breeding Overview:
📍 Active Matings: 1
👶 Litters: 2 (Born last week)
🐶 Puppies: 8

Use 'add [sire] [dam]' to record new mating
Reply 'view [dogId]' to see breeding details
```

---

### **7. Dashboard Summary**
```
status
```

**Response:**
```
📊 Dashboard Summary:
🐾 Total Dogs: 15
✅ Active: 12
📍 Deployed: 3
💉 Vaccinated: 14
👶 Litters: 3
❤️ Matings: 2

Type a command for more details!
```

---

### **8. Add New Dog**
```
add [name] [breed]
```

**Examples:**
```
add Buddy Labrador
add Princess Golden Retriever
add Rex German Shepherd
```

**Response:**
```
✅ Dog 'Buddy' (Labrador) added successfully!

You'll be notified when activities need recording.
Use 'view 4' to see details
```

---

## 📋 Command Reference Table

| Command | Syntax | Example | Use Case |
|---------|--------|---------|----------|
| Help | `help` | `help` | View all commands |
| List | `dogs` | `dogs` | See all your dogs |
| **View Details** | `view [id]` | `view 1` | **See complete dog info** |
| **View Details** | `detail [id]` | `detail 1` | **Same as view** |
| Record | `record [id] [activity]` | `record 1 feeding` | Log care activity |
| Vaccines | `vaccine` | `vaccine` | See due vaccines |
| Breeding | `breeding` | `breeding` | View breeding records |
| Status | `status` | `status` | Dashboard overview |
| Add | `add [name] [breed]` | `add Buddy Labrador` | Register new dog |

---

## 🔔 Notifications You'll Receive

The bot automatically sends notifications for:
- ✅ New dog registered
- 💉 Vaccine reminder (when due)
- 👶 Litter born
- ❤️ Mating event
- ⚠️ Health alerts

---

## 📱 Example Conversation

```
You: help
Bot: 🐾 Dog Management Bot Commands:
     help - Show this menu
     dogs - List all your dogs
     view [dogId] - View dog details
     ...

You: dogs
Bot: 🐕 Your Dogs:
     1. Buddy (Labrador) - Active ✓
     2. Luna (Golden Retriever) - Active ✓
     3. Max (German Shepherd) - Deployed 📍

You: view 1
Bot: 🐕 Dog Details - ID: 1
     Name: Buddy
     Breed: Labrador Retriever
     Status: Active ✅
     Vaccinations: Rabies (due in 3 days)
     ...

You: record 1 feeding Good appetite
Bot: ✅ FEEDING recorded for dog 1
     Note: Good appetite

You: vaccine
Bot: 💉 Vaccination Due Soon:
     🔴 Buddy (Labrador) - Rabies (due in 3 days)
     🟡 Luna (Golden Retriever) - DHPP (due in 7 days)
```

---

## 🛠️ Setup Instructions

### For Business Owners
1. Get WhatsApp Business API credentials from Meta
2. Set up your business WhatsApp number
3. Create webhook integration with: `https://your-api.com/api/WhatsApp/webhook`
4. Test by sending: `help`

### For Developers
**Environment Variables:**
```
WhatsApp__AccessToken=YOUR_TOKEN_HERE
WhatsApp__PhoneNumberId=YOUR_PHONE_ID
WhatsApp__WebhookToken=YOUR_VERIFY_TOKEN
```

**API Endpoints:**
- Receive: `POST /api/WhatsApp/webhook`
- Verify: `GET /api/WhatsApp/webhook`
- Send: `POST /api/WhatsApp/send`

---

## 💡 Tips & Tricks

### Quick Actions
- Send `dogs` to get dog IDs
- Use `view [id]` to get complete info before recording
- Send `vaccine` regularly to stay on top of vaccination schedule

### Best Practices
- Use short dog IDs for faster typing
- Always include notes with care records
- Check `status` weekly for overview
- Keep phone number saved in WhatsApp for quick access

### Troubleshooting
- **Bot not responding?** Make sure webhook is configured
- **Wrong dog ID?** Use `dogs` to see correct IDs
- **Syntax error?** Use `help` to see exact format
- **No activity recorded?** Ensure dog ID is valid

---

## 🎯 View Dog Details Features

The new `view` command shows:

✅ **Basic Info**
- Name, Breed, Gender
- Date of Birth, Age
- Microchip number
- Color/Markings

✅ **Status**
- Current status (Active/Deployed/Retired)
- Owner name
- Assigned handler
- Current location

✅ **Medical**
- All vaccinations with dates
- Due vaccine alerts
- Health notes

✅ **Family**
- Sire (father) info
- Dam (mother) info
- Offspring count

✅ **Activity**
- Recent 5 activities
- Last activity type & date
- Care patterns

---

## 📊 Use Cases

### **Scenario 1: Daily Health Check**
```
User: view 1
Bot:  [Shows complete dog details including vaccination status]
User: record 1 feeding Good appetite today
Bot:  ✅ Activity recorded
```

### **Scenario 2: Vaccination Reminder**
```
User: vaccine
Bot:  [Shows dogs needing vaccines]
User: view 2
Bot:  [Shows Luna's full details including vaccine schedule]
User: record 2 vaccine Rabies booster given
Bot:  ✅ Vaccination recorded
```

### **Scenario 3: Breeding Management**
```
User: breeding
Bot:  [Shows active matings and litters]
User: view 1
Bot:  [Shows Buddy's breeding info and offspring]
```

---

## ✅ Build Status
```
✅ WhatsApp Service: Complete
✅ View Command: Added
✅ Dog Details: Implemented
✅ All Features: Tested
✅ No Build Errors: 0 Errors
```

---

## 🚀 What's Next?
- [ ] Photo sharing (view dog photos via WhatsApp)
- [ ] Media uploads (send vaccination photos)
- [ ] Quick actions (buttons for common tasks)
- [ ] Multi-language support
- [ ] Automated vaccine reminders
- [ ] Breeding recommendations

---

**Your dog management is now just a WhatsApp message away! 🐾**
