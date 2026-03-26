
# CoreBanker MudBlazor Frontend

This is a Blazor WebAssembly frontend for CoreBanker, using MudBlazor for modern Material Design UI components.

## Features
- MudBlazor-based navigation and layout
- All major BankInsight modules scaffolded as pages
- Permission-aware navigation and role-based UI
- CoreBanker branding and custom theme

## Getting Started

### Prerequisites
- .NET 8 SDK or later

### Build and Run
```sh
dotnet build "CoreBankerWeb/CoreBanker/CoreBanker.csproj"
dotnet run --project "CoreBankerWeb/CoreBanker/CoreBanker.csproj"
```

Then open the provided local URL in your browser.

## Customization
- Update navigation and pages in the `Pages/` and `Layouts/` folders.
- Modify branding in `MainLayout.razor` and `AppNavMenu.razor`.

## Project Structure

- `Pages/` — Main pages for each module (Dashboard, Clients, Accounts, Loans, etc.)
- `Layouts/` — Application layouts (MainLayout, LoginLayout, etc.)
- `Components/` — Reusable UI components
- `Components/Shared/` — Shared components (navigation, dialogs, etc.)
- `Components/Modules/` — Module-specific components
- `Services/` — API clients and business logic
- `Models/Dto/` — Data transfer objects for API integration
- `Models/ViewModels/` — View models for UI binding
- `State/` — State containers and management
- `Auth/` — Authentication and authorization logic
- `Utilities/` — Helper classes and utilities

---
Scaffolded for a full BankInsight replatforming. All modules and navigation are in place. Next: implement business logic, UI, and backend integration for each module.
