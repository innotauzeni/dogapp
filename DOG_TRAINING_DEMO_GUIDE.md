# Dog Training – Setup & Demo Guide

Dog Training is a module inside the existing Dog Management portal (ASP.NET). It has **its own** customers,
dogs, invoices and payments, so outside customers' dogs and training money never mix with the company's own
dogs (`ka_dogs`), customers (`ka_customers`) or the Invoices module.

## 1. Going live on the Ubuntu server

1. **Back up the database**, then run the ONE setup file on it:

   ```bash
   mysql -u <user> -p <database> < sql/DogTraining_LiveSetup.sql
   ```

   It creates the 18 training tables **and** adds the Dog Training menu, its 13 submenus, the 29 `training.*`
   permissions and the role assignments. It is safe to run more than once and never drops or changes existing data.
   Roles set up automatically: **SystemOwner** and **Admin** (everything) and **Dog Handler** (daily logbook and
   weekly reports only). For any other role (Manager, Staff…) tick the training permissions in
   *Configuration → Roles*.
2. **Deploy the new API + Portal** and restart both. You do not add menus, submodules or permissions by hand.
3. Users **log out and in again** to see the new menu.
4. **Training Programmes → "Create default 21-day programme"**, then **Edit** it and set the **fee** (e.g. 150).
   The programme is built from your Week 1–3 plan sheets. Training cannot be invoiced while the fee is 0.
5. Create users with the **Dog Handler** role (e.g. *Hannah Handler*) and, optionally, set each handler's maximum
   dogs under *Handler Allocation → Capacity & availability*.

Suggested permissions for other roles:

| Role | Permissions to tick |
|---|---|
| **Vet / Examiner** | `training.dashboard.view`, `training.health.view`, `training.health.create`, `training.health.approve`, `training.enrolment.view`, `training.dog.view`, `training.customer.view` |
| **Cashier / Finance** | `training.dashboard.view`, `training.enrolment.view`, `training.invoice.view`, `training.invoice.generate`, `training.payment.record`, `training.customer.view`, `training.dog.view` |

A handler has none of the "manager" permissions, so they only ever see the dogs allocated to them.

> The older `sql/AddDogTraining.sql` is for a **development** database only – it drops and re-creates the training
> tables. Do **not** run it on the live database.

## 1b. WhatsApp

Dog Training is also in the WhatsApp bot: main menu option **10**, or type **training**. What each person sees
depends on their permissions:

| Option | Who | What it does |
|---|---|---|
| **My training dogs** | handlers | Their dogs in training, with week/day and whether today is logged. |
| **Log today's training** | `training.log.record` | Pick a dog → rate each scheduled activity 0-10 (or *skip*) → minutes → notes. Same record as the web and mobile logbook. |
| **Training summary** | `training.dashboard.view` | The pipeline counts: awaiting payment, in training, awaiting assessment, completed, certificates. |
| **Record a payment** | `training.payment.record` | Pick an unpaid invoice → amount (or *full*) → Cash / EcoCash / Bank / Card → receipt number. |

Photos and video are added from the mobile app or the web portal.

## 2. The process (what the boss will see)

```
Customer + Dog  →  Enrolment  →  Health assessment (PASS)  →  Invoice  →  Payment IN FULL
   →  Allocate handler  →  Activate (21 days)  →  Daily logbook + weekly reports
   →  Final assessment  →  Management approval  →  Certificate (PDF + QR verification)
```

Hard rules the system enforces (good to point out):
* A dog **cannot be invoiced** until it has **passed** the health assessment.
* A handler **cannot be allocated** and training **cannot start** until the invoice is **paid in full**
  (part payment does not unlock it).
* A handler **cannot exceed their capacity**, and cannot be allocated if marked unavailable.
* A handler only sees **their own** dogs.
* A **certificate can only be issued** for a **passed** final assessment that **management has approved**.
* Everything is recorded in the **Audit Trail** (who, when, and the previous values).

## 3. Demo script (about 15 minutes)

Log in as the **Training Manager** unless stated.

| # | Screen | What to do | What to say / point out |
|---|---|---|---|
| 1 | **Training Customers** | *Add customer* – "Cathy Customer", phone, address. | Customers are separate from the company's own customers. |
| 2 | **Training Dogs** | *Add dog* – "Rex", Ridgeback, owner Cathy, add a photo. | Dogs belong to outside customers, not the company's dogs. |
| 3 | **Training Enrolments** | *New enrolment* → pick Cathy, Rex, the 21-day programme (+ optional additional charge). | The customer/dog can also be added right here with "+ New". Status: *Awaiting health assessment*. |
| 4 | **Health Assessments** | Click **Assess** on Rex. Fill weight, tick some Normal/Abnormal, treatments, certification boxes, sign. **Save**, then **Passed**. Click **Print form (PDF)**. | Same layout as your paper Health Check Form. Show that a failed dog can be re-assessed. |
| 5 | **Enrolment page** (open Rex) | Note the progress tracker. Click **Generate invoice**. | Only possible after the health pass. Fee + extras are on the invoice. |
| 6 | **Invoices & Payments** | Open the invoice → **Record payment** for **part** of the amount. Then go back to the enrolment. | Status stays *Awaiting payment* – training is still blocked. Show **PDF**. |
| 7 | **Invoices & Payments** | Record the **balance**. | Invoice = *Paid*. Enrolment moves to *Awaiting handler*. Each payment gets a receipt number. |
| 8 | **Handler Allocation** | Click **Allocate handler** for Rex → pick Hannah (see the workload bar). | Capacity and availability are shown; a full handler can't be chosen. Status: *Ready*. |
| 9 | **Handler Allocation** | **Start** → pick the start date. | 21 days, ends day 21. Each dog gets **its own copy** of the weekly plan (Week 1 Foundation, Week 2, Week 3 Real-World Proofing). |
| 10 | **Enrolment → Weekly plan** | Show the 3 weeks, targets and the 5-slot daily schedule; click **Edit** on a week. | Management can tailor a single dog's plan; templates live in *Training Programmes*. |
| 11 | **Daily Training Logbook** (log in as **Hannah Handler**, on a **phone** if possible) | Open **My dogs today** → *Log today's training*. Mark activities done, rate each out of 10, add notes, take a **photo/video**, **Save**. | Phone-friendly. Average rating suggests the day's label (Excellent … Poor). She sees only her own dogs. |
| 12 | **Progress Reports** (as Hannah, then Manager) | *Week report* → pre-filled from the logbook → **Submit**. Manager opens it → **Approve** (or **Return** with a comment). **Download PDF**. | Reports are staff-only for now; the approved PDF can be sent to the customer. |
| 13 | **Final Assessments** | *(see the demo shortcut below)* **Assess** → score the 10 areas → result **Passed**. | A failed dog can be **extended** with extra days and activities, then re-assessed. |
| 14 | **Final Assessments** (Manager) | **Approve completion.** | Management must approve before a certificate exists. |
| 15 | **Certificates** | **Issue certificate** → **PDF**. Scan the **QR code** with a phone (or open `/Verify`). | Unique certificate number, logo, signature line. The public page shows ✔ valid – and ✖ after **Revoke**. |
| 16 | **Training Dashboard** & **Training Reports** | Show the counts (awaiting payment, in Week 1/2/3, completed, certificates), filters, revenue by programme/handler/month, **Export CSV**. | One screen for the whole pipeline. |
| 17 | **Audit Trail** | Open the latest entries for the training tables. | Who did what, with the previous values. |

### Demo shortcut – do **not** wait 21 days
At step 9 set the **Start date to ~25 days ago** (the field accepts past dates). The training period then counts as
finished, so the dog appears under **Final Assessments → Due** immediately, and the handler can back-fill a few
daily logs (the log date can be any day from the start date up to today) before you assess. Use this for the demo only.

## 4. If something looks missing
* **No "Dog Training" menu** – restart the API, then tick the permissions for the role in *Roles*.
* **"No active programmes"** when enrolling – create/activate one in *Training Programmes*.
* **Can't invoice** – the programme fee is 0, or the health assessment is not *Passed* yet.
* **A button is not showing** – the role lacks that permission (buttons the role cannot use are hidden).
* **Photos/videos don't upload** – file storage (R2) or the `wwwroot/uploads` folder must be writable on the server.
