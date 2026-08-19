# Diva Cartouche Assistant

A small Windows desktop app for creating Word documents from local templates, applying a predictable code, preparing the parent folders on the real Windows Desktop, and keeping a PDF beside each DOCX.

## Requirements

- Windows 10 or 11
- Microsoft Word desktop for `.dotm` and `.docx` generation
- A local template bundle supplied separately by the organization

## Private configuration

The public repository contains no organization names, logos, templates, folder taxonomy, keys, or telemetry. At runtime the app reads an optional private file from:

`%LOCALAPPDATA%\DivaCartoucheAssistant\private-schema.json`

Templates are read from:

`%LOCALAPPDATA%\DivaCartoucheAssistant\Templates`

The private schema controls the root folder name, types, domains, services, template filenames, register markers, and letter location. This keeps an organization's exact folder vocabulary out of a public repository and public releases.

Copy `schema.example.json` and adapt it for a private installation. Do not commit the adapted file or private templates.

## Building

```powershell
dotnet build -c Release
dotnet run --project tests/SelfCheck/SelfCheck.csproj -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The app does not collect telemetry. The optional updater only checks the public GitHub Releases endpoint and downloads a signed-by-hash release asset when the user accepts it. It updates the executable in place, preserves the local settings/schema/templates, and restores the previous executable if the new one does not start successfully.

## License

MIT. See [LICENSE](LICENSE).
