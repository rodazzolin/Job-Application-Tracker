# CLAUDE.md

ASP.NET Core MVC job-application tracker (.NET 10, EF Core + SQL Server, Identity, Bootstrap 5). No test project.
I'm learning this stack: when a change introduces a backend concept I haven't used yet, explain it briefly.

## Commands
- `dotnet watch` for hot reload; `dotnet build` to check compilation
- After any entity/DbContext/FieldLengths change: `dotnet ef migrations add <Name>`, **review the generated migration**, then `dotnet ef database update`
- `dotnet ef migrations has-pending-model-changes` to check whether a migration is needed

## Rules
- **Per-user scoping:** `JobApplicationsController` is `[Authorize]`. Every query filters by `UserID` (NameIdentifier claim), including new ones. Single-record lookups use ID *and* UserID and return `NotFound()` when missing.
- **Forms:** never bind entities from forms; use view models. POST actions use `[ValidateAntiForgeryToken]` and redirect on success (Post/Redirect/Get). Cross-field validation goes in the view model's `IValidatableObject.Validate`.
- **Enums are stored as strings** (`JobApplicationStatus`). Never rename or reorder members (see job-form.js gotcha); appending is fine if it fits `FieldLengths.Status`.
- **Field lengths** live only in `Models/FieldLengths.cs` (used by EF config and view-model attributes).
- **Keys are `int`.** Don't change key types; it breaks migrations.
- **Times:** timestamps (`CreatedAt`, `UpdatedAt`, status change `Time`) are UTC `DateTime`; real-world dates (`PostedAt`, `AppliedAt`) are `DateOnly`.
- `JobApplication → IdentityUser` uses `DeleteBehavior.NoAction` on purpose (SQL Server multiple cascade paths). Don't change it.

## Domain rules (in JobApplicationsController)
- Status `Saved` forces `AppliedAt = null` (create and edit).
- A `JobApplicationStatusChange` row is added only when an edit actually changes the status. Creation adds none: the initial state is the job's `CreatedAt` plus the first change's `OldStatus` (or current `Status` if never changed).
- `UpdatedAt` bumps on every edit.
- All status changes go through `JobApplicationService.ChangeStatus` (shared by Edit and the board's drag-and-drop `UpdateStatus` endpoint). Leaving `Saved` with no `AppliedAt` sets it to today.
- Deleting a company's last application deletes the company. [Also on edit, when an application moves to another company: handled / TODO]
- `AppliedAt` must be on or after `PostedAt` (view-model validation; skipped for `Saved`).
- Status changes cascade-delete with their application.

## Front end
- Bootstrap 5 utility classes first; custom CSS in `wwwroot/css/site.css`.
- Page scripts go in `@section Scripts` of the page, not in partials (partials can't define sections). Shared scripts live in `wwwroot/js/`.

## Gotchas
- `wwwroot/js/job-form.js` hides Applied At when the status select is `"0"` (`Saved`), so it depends on the enum's integer values.
- Folder is `View Models/` (with space); namespace is `JobTracker.ViewModels`.
- Create/Edit share `JobApplicationFormViewModel` and `Views/Shared/_JobApplicationForm.cshtml`.