# Haley.Helpers
Some of the common C# Helpers

## QR codes

`QrCodeBuilder` creates cross-platform QR codes from any text or URL. SVG is suitable
for web pages and printing; PNG bytes are suitable for files and HTTP responses.

```csharp
using Haley.Utils;

var svg = QrCodeBuilder.CreateSvg("https://example.com/view/signed-token");
var png = QrCodeBuilder.CreatePng("https://example.com/view/signed-token");
```
