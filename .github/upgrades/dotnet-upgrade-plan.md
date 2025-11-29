# .NET 10 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade WebApp.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                   | Current Version | New Version | Description                                   |
|:-----------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore |   8.0.20        | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore   |   8.0.20        | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Identity.UI                  |   8.0.20        | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore.SqlServer           |   8.0.20        | 10.0.0      | Recommended for .NET 10.0                      |
| Microsoft.EntityFrameworkCore.Tools               |   8.0.20        | 10.0.0      | Recommended for .NET 10.0                      |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### WebApp.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` should be updated from `8.0.20` to `10.0.0`
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` should be updated from `8.0.20` to `10.0.0`
  - `Microsoft.AspNetCore.Identity.UI` should be updated from `8.0.20` to `10.0.0`
  - `Microsoft.EntityFrameworkCore.SqlServer` should be updated from `8.0.20` to `10.0.0`
  - `Microsoft.EntityFrameworkCore.Tools` should be updated from `8.0.20` to `10.0.0`

Feature upgrades:
  - Review Identity and EF Core APIs for any breaking changes between .NET 8 and .NET 10 and update code as needed.

Other changes:
  - Verify that other referenced packages (e.g., `HtmlAgilityPack`, `Microsoft.VisualStudio.Web.CodeGeneration.Design`) have compatible versions for .NET 10 or update them if necessary.

