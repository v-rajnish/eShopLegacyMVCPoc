# Modernization Plan: Migrate Local File Storage to Azure Blob Storage

**Project**: eShopPorted

---

## Technical Framework

- **Language**: .NET 8 (net8.0)
- **Framework**: ASP.NET Core MVC
- **Build Tool**: dotnet CLI / MSBuild
- **Database**: SQL Server (Entity Framework Core 8.0)
- **Key Dependencies**: Autofac, Azure.Identity, Entity Framework Core, log4net, Application Insights

---

## Overview

This migration moves whole-file and document storage from the local disk to Azure Blob Storage. Today the application stores and serves catalog item images and files directly from the local `wwwroot/Pics` folder — `PicController` reads image bytes with `System.IO.File.ReadAllBytes` from `WebRootPath/Pics`, and images are also served as static files (`/Pics/{id}.png`) via `app.UseStaticFiles()`. The new architecture will:

- Store and serve all files and documents from an Azure Blob Storage container instead of local disk, so the application is stateless and container/cloud ready.
- Authenticate to Azure Blob Storage with `DefaultAzureCredential` / Managed Identity, keeping secrets out of source and configuration.
- Introduce a reusable blob storage service abstraction (upload, download, list, delete) with dependency-injection registration, and migrate existing local assets into the blob container.

The migration is delivered as a single cohesive transform of the current file-access code paths, followed by a dependency CVE remediation pass, with build verification after changes.

---

## Migration Impact Summary

| Application  | Original Service            | New Azure Service    | Authentication   | Comments                                             |
|--------------|-----------------------------|----------------------|------------------|------------------------------------------------------|
| eShopPorted  | Local disk (`wwwroot/Pics`) | Azure Blob Storage   | Managed Identity | Serve/store all files & documents from a blob container |

---

## Detected File-Access Code Paths (Scope)

The transform task is scoped to these existing code paths detected in the workspace:

- `eShopPorted/Controllers/PicController.cs` — `Index(int catalogItemId)` builds `Path.Combine(env.WebRootPath, "Pics")` and reads bytes via `System.IO.File.ReadAllBytes(path)`, then returns `File(buffer, mimetype)`.
- `eShopPorted/Controllers/CatalogController.cs` — `AddUriPlaceHolder` sets `item.PictureUri = $"/Pics/{item.Id}.png"` (static-file URL).
- `eShopPorted/Program.cs` — `app.UseStaticFiles()` serves `wwwroot` (including `/Pics/*`) from local disk.
- `eShopPorted/appsettings.json` — configuration root; add a Blob Storage section (account URI / container name).
- `eShopPorted/eShopPorted.csproj` — already references `Azure.Identity`; add `Azure.Storage.Blobs`.
- `eShopPorted/Modules/ApplicationModule.cs` + `Program.cs` — DI composition (Autofac) where the blob service is registered.
- `eShopPorted/wwwroot/Pics/*.png` — existing local assets (`1.png`–`12.png`) to seed into the blob container.

---

## Tasks

The detailed, executable task breakdown is maintained in `.metadata/tasks.json`. High-level summary:

1. **Migrate local file storage to Azure Blob Storage** (`transform`) — Add the Azure Blob Storage SDK and configure `DefaultAzureCredential`/Managed Identity; introduce a blob storage service abstraction (upload, download, list, delete) with DI registration; refactor `PicController` and other local-disk file access to read/write from Blob Storage; provide a mechanism to upload existing `wwwroot/Pics` assets and documents into the container; add appsettings configuration for the storage account/container; verify the project builds.
2. **Security compliance** (`security`) — Scan project dependencies for known CVEs and remediate to a patched version, then verify the build and tests.

---

## Open Questions & Questionnaire

- [x] Q: What authentication method should be used for Azure Blob Storage? → A: Managed Identity via `DefaultAzureCredential` (no secrets in code or config).
- [x] Q: Is an assessment report available? → A: No — plan generated directly from the user-provided task specification.
- [ ] Integration testing was not requested, so no baseline/integration-test tasks are included.
