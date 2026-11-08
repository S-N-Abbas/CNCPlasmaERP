# Manual ERP Test Plan

## Safe test setup

The normal profile uses the `CNCPlasmaERP` LocalDB database. Do not clear it for testing. The test profile uses a separate database named `CNCPlasmaERP_Test`.

1. Close any running ERP window.
2. From the repository root, run `.\run-test.cmd` in PowerShell or Command Prompt. The `.cmd` launcher works when PowerShell script execution is restricted.
3. On first launch, the app applies migrations to `CNCPlasmaERP_Test` and opens the first-admin setup. Create a test admin; the existing database's users are not copied.
4. Close and relaunch with the same script, then sign in to the test profile.
5. Use names prefixed with `TEST` so test records are easy to identify.

The launcher sets `CNCPLASMAERP_PROFILE=Test` only for the test process and restores the prior environment value when the app closes. Do not launch the regular app shortcut when you intend to test. If the profile or test connection cannot be confirmed, stop before entering data.

The test database is retained between launches. It is safe to remove **only** `CNCPlasmaERP_Test` through SQL Server Object Explorer / SSMS when you need a fresh run; verify the database name before dropping it. Never drop `CNCPlasmaERP`.

## Test data for the main workflow

Create these records in the test profile:

- Material: `TEST MS Sheet 16G`, type MS, default rate 200/kg, reorder level 20 kg.
- Unused material: `TEST Empty Sheet 14G` (for zero-stock deactivation checks).
- Supplier: `TEST Steel Supplier`.
- Customer: `TEST Main Customer`, opening balance 0, credit limit 5,000.
- Keep tax disabled for the first invoice calculation below. Later repeat the invoice calculation with tax enabled at 10%.

Run cases in order. Record **Pass**, **Fail**, or **Blocked**, plus what you observed. If a case fails, stop that workflow and note the record numbers; don’t “correct” balances manually while diagnosing it.

## Cases

### A. Startup, login, and access

| ID | Action | Expected result |
|---|---|---|
| A1 | Start the test profile on a fresh test database. | Migrations finish; first-admin setup appears. The normal `CNCPlasmaERP` data remains untouched. |
| A2 | Create the test admin, close, and restart using `run-test.ps1`. | Login appears; valid test credentials work. |
| A3 | Try a wrong password, then the correct password. | Wrong password is rejected; valid login succeeds without crashing. |
| A4 | Create an Operator, Manager, and Cashier; sign in as each. | Operator gets Jobs/Quotations but no stock, customers, suppliers, invoices, expenses, reports, users, or settings. Manager gets operational/finance modules but no Users or Settings. Cashier gets Customers, Invoicing, and Reports, but no Jobs or Stock. Admin can access all modules. |

### B. Master records and safe editing

| ID | Action | Expected result |
|---|---|---|
| B1 | Add the material, supplier, and customer listed above. | Each appears once in its list. Required-field and invalid-number messages are clear; invalid records are not saved. |
| B2 | Edit each record, change a contact/name or reorder/default rate, then Cancel. | Form returns to add mode; no change is saved. |
| B3 | Edit each again and Save. | New descriptive values appear after refresh/reopen. Customer balance, opening balance, stock kg, and sheet count do not change as a side effect. |
| B4 | Create two more customers with opening balances +500 and −250. | Ledger records show debit/credit with the documented sign convention; each customer’s current balance equals its ledger running balance. |

### C. Stock purchase and material usage

| ID | Action | Expected result |
|---|---|---|
| C1 | Purchase 100 kg at 200/kg, 4 sheets, and 500 carriage from the test supplier. | Material stock becomes 100 kg and 4 sheets. Purchase material cost is 20,000; carriage is recorded separately. |
| C2 | Attempt a purchase with zero/negative weight or rate, negative carriage, or negative sheet count. | Validation rejects it; no purchase row, stock change, or expense is recorded. |
| C3 | Create a job for the test customer: service 500, carriage 200. Add usage of 10 kg and 2 sheets at selling rate 300/kg, with 1.5 kg scrap. | Job moves Pending → In Progress. Stock becomes 90 kg / 2 sheets; scrap balance becomes 1.5 kg. Usage snapshots purchase cost rate 200/kg and selling rate 300/kg. |
| C4 | Attempt usage above available kg/sheets, negative sheet count, or scrap greater than usage weight. | Attempt is rejected and stock/scrap balances remain unchanged. |
| C5 | Complete the main job and try adding more usage. After E1 invoices it, try removing usage. | Adding usage to a completed job is rejected. After invoicing, removal is also rejected and stock/scrap stay unchanged. Completed jobs expose no Cancel action. |
| C6 | Create a second temporary job, record 1 kg / 1 sheet usage and 0.5 kg scrap, then try to cancel it. Reverse that usage and cancel the temporary job before selling any of its scrap. | Cancellation is first rejected while usage exists. Reversal restores the 90 kg / 2-sheet stock and 1.5 kg main-job scrap balances; cancellation then succeeds. |

### D. Scrap sale

| ID | Action | Expected result |
|---|---|---|
| D1 | Sell 1 kg scrap at 100/kg. | Sale total is 100 and scrap balance becomes 0.5 kg. |
| D2 | Try to sell more than the remaining 0.5 kg or use zero/negative amount/rate. | Rejected; scrap balance and sale history do not change. |

### E. Invoice and customer ledger

| ID | Action | Expected result |
|---|---|---|
| E1 | With tax disabled, generate an invoice for the completed job. Try removing usage and deactivating the customer before taking payment. | Lines are material 3,000 + service 500 + carriage 200; subtotal/grand total is 3,700. Job becomes Invoiced and one matching debit appears in the ledger. Usage removal and customer deactivation are rejected; no balances change. |
| E2 | Try to generate another invoice for that job. | Rejected; no second invoice or ledger debit appears. |
| E3 | Select the invoice in Payments, note the displayed Amount Due, then record a 1,000 payment. | Before payment, Amount Due shows 3,700. Invoice becomes Partially Paid; amount due becomes 2,700; customer balance falls by 1,000. |
| E4 | Attempt payment above the remaining invoice amount or attach an invoice belonging to another customer. | Rejected; invoice paid amount and customer ledgers remain unchanged. |
| E5 | Pay the remaining 2,700. | Invoice becomes Paid, amount due is zero, and the customer balance reconciles to the sum of ledger entries (zero for this main customer). |

### F. Quotations

| ID | Action | Expected result |
|---|---|---|
| F1 | Create a quote for the test customer with a 5 kg × 300/kg item, service 500, and carriage 200. | Item amount is 1,500; quote total before tax is 2,200; status is Draft. |
| F2 | Mark it Sent, then convert it to a job. | Status becomes Sent then Converted; one linked job is created. Conversion does not consume stock; usage must be recorded through the job workflow. |
| F3 | Try to convert the same quote again and try an expired quote. | Duplicate conversion and expired quote creation are rejected without duplicate jobs. |

### G. Expenses, wages, and reports

| ID | Action | Expected result |
|---|---|---|
| G1 | Record an operating expense of 300 dated today. | It appears once in Expenses; zero/negative amounts are rejected. |
| G2 | Before recording wages, run Profit & Loss for a date range including today. | Revenue is 3,800 (invoice 3,700 + scrap sale 100), material cost 2,000, gross profit 1,800, expenses 800 (300 expense + 500 purchase carriage), and net profit 1,000. Payments do not reduce recognized revenue. |
| G3 | Record a wage payment of 150 dated today, then rerun Profit & Loss. | Wages appear as an expense; expenses become 950 and net profit becomes 850. |
| G4 | Run Profit by Material for the same range. | The invoiced usage shows 10 kg, revenue 3,000, cost 2,000, profit 1,000, and approximately 33.33% margin. |
| G5 | Run reports with ranges before, during, and after the transaction dates; use Refresh and Print. | Date filters include/exclude transactions correctly; printed values match the on-screen report. |
| G6 | Open Stock Report after C3 and C6. | Test material shows 90 kg and 2 sheets, matching the Stock screen. |

### H. Final persistence and consistency

| ID | Action | Expected result |
|---|---|---|
| H1 | Create a separate `TEST Tax Customer` and a service-only job with a 3,700 charge. Enable 10% tax in Settings, complete and invoice it. | A completed job cannot be cancelled and exposes no Cancel action. Tax is 370 and grand total 4,070. Do this after G2–G6 so it doesn’t affect the main workflow report totals. |
| H2 | After E5, deactivate the settled main customer. Try deactivating the stocked material and supplier; deactivate the unused material. | Customer deactivation succeeds at zero and ledger history remains. Material deactivation fails while kg/sheets remain. The unused material can be deactivated. The inactive supplier is hidden from active choices but stays visible on its historical purchase. |
| H3 | Close and reopen the test profile; inspect jobs, invoices, payments, balances, stock, scrap, and reports. | Saved records persist and all calculated balances match the values above. |
| H4 | Review the ledger, invoice list, purchase history, and job details for the same records. | Each transaction appears once; linked customer/material/supplier and amounts agree across screens. |

### I. Tab refresh regression checks

Perform these checks without leaving the current main menu. They specifically cover the stale-tab issue.

| ID | Action | Expected result |
|---|---|---|
| I1 | In Invoicing, generate an invoice from a completed job, then select Invoices. Return to Generate Invoice. | The invoice appears immediately in the list; the invoiced job is absent from the completed-jobs list. No main-menu navigation is needed. |
| I2 | Select an unpaid invoice, switch to Payments, record a partial payment, then return to Invoices. | The same invoice remains selected and shows Partially Paid with its updated balance. |
| I3 | Pay the remaining balance, return to Invoices, then return to Payments. | Invoice shows Paid and zero balance; the paid invoice is gone from outstanding choices and the new payment appears in recent payments. |
| I4 | In Stock, record a purchase in Purchases, switch to Materials, then back. | Material kg/sheet totals and the purchase history both reflect the new transaction. |
| I5 | In Stock, sell scrap in Scrap, switch away and back to Scrap. | The displayed scrap balance and recent sale list reflect the sale. |
| I6 | In Customers, add or edit a customer, switch between Customers and Ledger. | The ledger customer choices reflect the latest active customer details; the selected customer and statement remain selected when still valid. |

## Results log

For each case, note: `ID | Pass/Fail/Blocked | observed result | record number | screenshot/log detail`. The app writes diagnostic logs under the launched profile's `Logs` folder; redact passwords and customer information before sharing logs.
