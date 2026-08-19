# Diva Cartouche Assistant

Windows desktop assistant for the ARSEF document system. It creates the cartouche in the template's central header cell, keeps the title centered, generates codes such as `OUT-QUA-Codification des documents-1`, prepares the ARSEF folders on the real Windows Desktop, opens the DOCX and its exact folder, then exports the matching PDF when the user clicks `Document fini`.

The document session is saved locally so a restart can offer to resume the unfinished DOCX. At completion, Diva asks whether the document belongs in the document-management register. It reads the existing `Lieu de classement` values from the selected Excel workbook, supports several selections, validates the register columns, appends one line, and never duplicates an existing codification.

## Requirements

- Windows 10 or 11, 64-bit
- Microsoft Word desktop

The ARSEF templates are embedded in the application and are also available as external files under `%LOCALAPPDATA%\DivaCartoucheAssistant\Templates` when the app extracts them. The optional `private-schema.json` file can customize the lists and template names for another installation.

## Build

```powershell
dotnet build -c Release
dotnet run --project tests/SelfCheck/SelfCheck.csproj -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The app remembers the preparer's name after it is entered. It does not collect telemetry. The optional updater checks the public GitHub Releases endpoint, shows download progress, verifies the package by SHA-256, preserves settings and templates, restores the previous executable if the new one does not start successfully, and confirms the update after restart.

## License

MIT. See [LICENSE](LICENSE).
