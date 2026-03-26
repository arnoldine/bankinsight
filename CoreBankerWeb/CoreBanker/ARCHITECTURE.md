
# CoreBanker MudBlazor Architecture Notes

This project replatforms the BankInsight React+Vite frontend to a Blazor WebAssembly app using MudBlazor, following a modular, scalable, and enterprise-grade architecture.

## Key Concepts
- **Blazor + MudBlazor**: Modern .NET SPA with Material Design components
- **Strongly Typed Services & DTOs**: All backend integration uses typed C# services and DTOs
- **Dependency Injection**: All services are registered and injected
- **Centralized API Client Layer**: `ApiClientBase` and per-domain services
- **Authentication & Authorization**: Custom `AuthService`, permission/role-aware UI, MFA, Clerk integration option
- **Role-Aware Navigation**: Menu and UI adapt to user permissions
- **Reusable Components**: Shared and module-specific components for maintainability
- **State Management**: Lightweight state containers for complex modules
- **Custom Theming**: Enterprise look, dense layouts, strong information hierarchy

## Mapping React to Blazor
- React pages/workbenches → Blazor `Pages/` and `Components/Modules/`
- React context/state → Blazor `State/` and cascading parameters
- React hooks → Blazor lifecycle methods and dependency injection
- React services → C# services in `Services/`
- React DTOs/Types → C# DTOs in `Models/Dto/`
- React permission wrappers → Blazor `AuthorizePermission` component
- React navigation → MudBlazor `MudNavMenu` with permission filtering

## Implementation Phases
1. Shell, theme, layout, auth, permissions (complete)
2. Navigation and dashboard (complete)
3. Core modules (clients, accounts, teller, transactions) (scaffolded)
4. Loans, approvals, accounting (scaffolded)
5. Treasury, vault, risk (scaffolded)
6. Reporting, audit, EOD, products, settings (scaffolded)
7. Security, migration, task inbox (scaffolded)
8. BankingOS, dynamic forms, extensibility (scaffolded)
9. Group lending, role workspaces (scaffolded)
10. Hardening, testing, polish (next)

---
All modules and navigation are scaffolded. Next: implement business logic, UI, and backend integration for each module. See the project prompt for full requirements and deliverables.
