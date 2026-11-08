# Supplier Portal

A standalone ASP.NET Web Forms tool for **new supplier creation**. It follows the same process as the MDM Portal:

1. The requester uploads a filled-in **Vendor Form** (`Templates/Vendor_Form.xlsx`, one supplier per file) and its supporting documents.
2. The form is read cell by cell ([docs/vendor-form-cell-map.md](docs/vendor-form-cell-map.md)) into `DW_MDM_APP_SUP_NEW`, and `DW_PROC_MDM_APP_VALID_SUP_NEW` validates it.
3. Once it's valid, the requester submits it. `DW_PROC_MDM_APPROVALS` builds the approval steps and `DW_PROC_MDM_EXEC_STEPS` moves it through them.
4. At the EXECUTE step, the MDM team runs the **creation procedure chosen in the Admin tab**.

Requests use template **67** and the MDM Portal's own tables (`DW_MDM_APP_REQUESTS`, `DW_MDM_APP_REQUEST_STEPS`, `DW_MDM_APP_USERS`, `DW_MDM_APP_TEMPLATES_ROLES`), so the same users, roles and approval chains apply.

## What's different from the MDM Portal

| MDM Portal issue | Here |
|---|---|
| SQL built by string concatenation | Every query goes through `lib/Db.cs` with bind parameters. Procedure names come only from Web.config or from the Admin tab, which accepts `OWNER.NAME` only if it exists in `ALL_PROCEDURES` for an allowed owner. |
| Permissions only enforced by hiding buttons in JavaScript | Each action (create, upload, submit, approve, reject, execute, download, admin) checks the user's role or current step on the server (`lib/Requests.cs`). |
| `?i=` lets some users act as someone else | Identity comes from Windows authentication only. |
| Uploads saved under the web root with the client's file name, served to anyone | Files go under `App_Data/Uploads/{request}/` (never served by IIS) with server-chosen names, a type allow-list, a size limit and a check of the file's first bytes. They're downloaded through `Download.ashx`, which checks access. |
| Postbacks / web methods open to cross-site requests | ViewState is tied to the session (`ViewStateUserKey`) and encrypted. |
| Execute could run twice | Status moves `In Progress → Executing` in one conditional update before the procedure runs. |

## Setup

1. Run `sql/00_DW_MDM_APP_SUP_NEW.sql` (if the table isn't there yet) and `sql/01_settings.sql` on DWH.
2. Check template 67's rows in `DW_MDM_APP_TEMPLATES` and `DW_MDM_APP_TEMPLATES_ROLES` and its approval chain (`sql/02_template_67_setup.sql`).
3. Fill in the connection strings in `Web.config` on the server, and set `Admin.Roles`.
4. Deploy to IIS with **Windows authentication on and anonymous off**. The app pool identity needs write access to `App_Data/Uploads` and the Microsoft ACE OLEDB 12.0 provider (already used by the MDM Portal).
5. Open **Admin** and set the creation procedure, the connection it runs on, and the operating unit for ATS, MLS and TAR.

## Open points

- Form fields with no staging column yet: postal code, zip code, SWIFT code, payment method, tax regime code, address purpose (purchasing/payment) and "Application for" (New/Update/Add Site). They're parsed but not stored.
- Resubmitting after a rejection calls `DW_PROC_MDM_EXEC_STEPS` with `SUBMIT` again and doesn't rebuild the steps. Check that the procedure handles that.
- Email notifications aren't wired in yet.
