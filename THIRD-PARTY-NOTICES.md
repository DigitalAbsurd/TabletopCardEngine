# Third-party notices

OpenHoldemEngine is licensed under the terms in [LICENSE](LICENSE). It builds on the third-party
components listed here, and the demo site redistributes some of them. Each component keeps its own
license, and this file reproduces the notices those licenses require.

- [What ships, and under which license](#what-ships-and-under-which-license)
- [PHH dataset](#phh-dataset)
- [Package inventory](#package-inventory)
- [License texts](#license-texts)

[scripts/audit-licenses.ps1](scripts/audit-licenses.ps1) generates every row of the package
inventory from the nuspecs that NuGet restored. `ThirdPartyNoticesTests` fails the test run in three
cases: a project resolves a package that has no row here, a row's license differs from the
package's nuspec, or a license falls outside the permissive allowlist. After changing a dependency,
re-run the script and paste its tables over the ones below.

## What ships, and under which license

**The library, `OpenHoldemEngine`**, references its dependencies as NuGet packages and redistributes
none of them. Its runtime closure is FluentResults plus Microsoft's EF Core and
`Microsoft.Extensions.*` packages, all MIT.

**The demo site, `OpenHoldemEngine.Demo`**, is a Blazor WebAssembly application, so publishing it
copies its dependencies into the site. On top of the library's closure it ships:

| Component | Where it comes from | License |
| --- | --- | --- |
| Ant Design Blazor 0.17.0: assemblies, plus the JavaScript and CSS under `_content/AntDesign` | NuGet `AntDesign` | MIT |
| OneOf 2.1.155 | NuGet `OneOf`, a dependency of AntDesign | MIT |
| Tomlyn 0.19.0 | NuGet `Tomlyn` | BSD-2-Clause |
| ASP.NET Core Blazor components, `Microsoft.JSInterop`, `Microsoft.Data.Sqlite`, EF Core SQLite | NuGet, Microsoft | MIT |
| `Microsoft.AspNetCore.Components.DataAnnotations.Validation` 3.2.0-rc1 | NuGet, Microsoft, a dependency of AntDesign | Apache-2.0 |
| SQLitePCLRaw 2.1.10 | NuGet `SQLitePCLRaw.*`, SourceGear | Apache-2.0 |
| SQLite, compiled into `dotnet.native.wasm` by `WasmBuildNative` | NuGet `SQLitePCLRaw.lib.e_sqlite3` | Public domain |
| The .NET 8 WebAssembly runtime and base class library (`dotnet.*`, `System.*.wasm`) | The SDK's `Microsoft.NETCore.App.Runtime.Mono.browser-wasm` pack | MIT, plus the notices in [dotnet/runtime's THIRD-PARTY-NOTICES.TXT](https://github.com/dotnet/runtime/blob/release/8.0/THIRD-PARTY-NOTICES.TXT) |
| PrismJS core, its C# grammar and its default theme | Vendored in `wwwroot/lib/prism` | MIT |
| `favicon.png`, `icon-192.png`, and the loading indicator in `index.html` and `css/app.css` | The .NET 8 Blazor project templates; the icons are byte-identical | MIT |
| `sample/sample.phhs` | The PHH dataset | MIT, see [PHH dataset](#phh-dataset) |

The legacy `System.* 4.3.0`, `runtime.*`, `Microsoft.NETCore.*` 1.1.0 and `NETStandard.Library`
1.6.1 packages in the demo's inventory carry the old Microsoft .NET Library License rather than an
SPDX expression. They arrive transitively through AntDesign's `netstandard` dependencies, and **none
of their assemblies ships**: the SDK's conflict resolution replaces each of them with the runtime
pack's newer `8.0.0.0` assembly. A detailed build
(`dotnet build src/OpenHoldemEngine.Demo -c Release -v:d`) resolves no conflict in favour of a 4.x
package. Only three NuGet assemblies win over the runtime pack, and all are MIT 9.0.1 packages:
`System.Diagnostics.DiagnosticSource`, `System.Text.Encodings.Web` and `System.Text.Json`.

Restore also puts `Microsoft.AspNetCore.Components.WebAssembly.DevServer`,
`Microsoft.NET.ILLink.Tasks` and `Microsoft.NET.Sdk.WebAssembly.Pack` in the demo's inventory.
These are the development server and the build tooling, and nothing from them is published.

**The tests** redistribute nothing. Their packages are listed below to make the audit complete. Two
of them are pinned on purpose. FluentAssertions stays on 6.x (Apache-2.0) because 8.0 moved to a
commercial license. Moq is 4.20.70 (BSD-3-Clause), the release that removed SponsorLink. Playwright
downloads its browsers at test time, and no browser is committed or redistributed.

## PHH dataset

The following hand histories come from the [PHH dataset](https://github.com/uoftcprg/phh-dataset):

- `src/OpenHoldemEngine.Tests/Resources/phh/pluribus.phhs.gz`
- `src/OpenHoldemEngine.Tests/Resources/phh/handhq-headsup.phhs.gz`
- `src/OpenHoldemEngine.Demo/wwwroot/sample/sample.phhs`, ten Pluribus hands

The two test corpora are pinned to upstream commit `e47fbd5816372360bade4de5d712346fe1bb70f6`. The
dataset is published under the MIT license:

```
MIT License

Copyright (c) 2024-2025 Universal, Open, Free, and Transparent Computer Poker Research Group

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

The dataset in turn compiles hands from other sources:

- `pluribus`: the 10 000 hands played by the Pluribus bot, released with Brown, N. & Sandholm, T.
  (2019), *Superhuman AI for multiplayer poker*, Science 365(6456), 885–890.
- `handhq`: anonymised PokerStars $0.25/$0.50 hand logs from July 2009. The source obfuscates player
  names, and the records hold only game play.

This repository redistributes those records under the dataset's MIT grant, and every copy keeps the
dataset's copyright notice beside it (`LICENSE-phh-dataset.txt`).
[The fixture README](src/OpenHoldemEngine.Tests/Resources/phh/README.md) covers provenance and
citation.

## Package inventory

Every NuGet package that restore resolves, listed under the first project that needs it. The
library comes first, then what the demo adds, then what the test projects add. `MS-.NET-Library` is
the pre-SPDX Microsoft .NET Library License discussed above.

### OpenHoldemEngine

| Package | Version | License |
| --- | --- | --- |
| FluentResults | 3.16.0 | MIT |
| Microsoft.EntityFrameworkCore | 9.0.1 | MIT |
| Microsoft.EntityFrameworkCore.Abstractions | 9.0.1 | MIT |
| Microsoft.EntityFrameworkCore.Analyzers | 9.0.1 | MIT |
| Microsoft.Extensions.Caching.Abstractions | 9.0.1 | MIT |
| Microsoft.Extensions.Caching.Memory | 9.0.1 | MIT |
| Microsoft.Extensions.DependencyInjection | 9.0.1 | MIT |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.1 | MIT |
| Microsoft.Extensions.Logging | 9.0.1 | MIT |
| Microsoft.Extensions.Logging.Abstractions | 9.0.1 | MIT |
| Microsoft.Extensions.Options | 9.0.1 | MIT |
| Microsoft.Extensions.Primitives | 9.0.1 | MIT |
| System.Diagnostics.DiagnosticSource | 9.0.1 | MIT |
| System.Threading.Tasks.Extensions | 4.5.4 | MIT |

### OpenHoldemEngine.Demo

| Package | Version | License |
| --- | --- | --- |
| AntDesign | 0.17.0 | MIT |
| Microsoft.AspNetCore.Authorization | 8.0.13 | MIT |
| Microsoft.AspNetCore.Components | 8.0.13 | MIT |
| Microsoft.AspNetCore.Components.Analyzers | 8.0.13 | MIT |
| Microsoft.AspNetCore.Components.DataAnnotations.Validation | 3.2.0-rc1.20223.4 | Apache-2.0 |
| Microsoft.AspNetCore.Components.Forms | 8.0.13 | MIT |
| Microsoft.AspNetCore.Components.Web | 8.0.13 | MIT |
| Microsoft.AspNetCore.Components.WebAssembly | 8.0.13 | MIT |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 8.0.13 | MIT |
| Microsoft.AspNetCore.Metadata | 8.0.13 | MIT |
| Microsoft.Data.Sqlite.Core | 9.0.1 | MIT |
| Microsoft.EntityFrameworkCore.Relational | 9.0.1 | MIT |
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.1 | MIT |
| Microsoft.EntityFrameworkCore.Sqlite.Core | 9.0.1 | MIT |
| Microsoft.Extensions.Configuration | 8.0.0 | MIT |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.1 | MIT |
| Microsoft.Extensions.Configuration.Binder | 8.0.2 | MIT |
| Microsoft.Extensions.Configuration.FileExtensions | 8.0.1 | MIT |
| Microsoft.Extensions.Configuration.Json | 8.0.1 | MIT |
| Microsoft.Extensions.DependencyModel | 9.0.1 | MIT |
| Microsoft.Extensions.FileProviders.Abstractions | 8.0.0 | MIT |
| Microsoft.Extensions.FileProviders.Physical | 8.0.0 | MIT |
| Microsoft.Extensions.FileSystemGlobbing | 8.0.0 | MIT |
| Microsoft.JSInterop | 8.0.13 | MIT |
| Microsoft.JSInterop.WebAssembly | 8.0.13 | MIT |
| Microsoft.NET.ILLink.Tasks | 8.0.31 | MIT |
| Microsoft.NET.Sdk.WebAssembly.Pack | 8.0.31 | MIT |
| Microsoft.NETCore.Platforms | 1.1.0 | MS-.NET-Library |
| Microsoft.NETCore.Targets | 1.1.0 | MS-.NET-Library |
| Microsoft.Win32.Primitives | 4.3.0 | MS-.NET-Library |
| NETStandard.Library | 1.6.1 | MS-.NET-Library |
| OneOf | 2.1.155 | MIT |
| runtime.any.System.Collections | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Diagnostics.Tools | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Diagnostics.Tracing | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Globalization | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Globalization.Calendars | 4.3.0 | MS-.NET-Library |
| runtime.any.System.IO | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Reflection | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Reflection.Extensions | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Reflection.Primitives | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Resources.ResourceManager | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Runtime | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Runtime.Handles | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Runtime.InteropServices | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Text.Encoding | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Text.Encoding.Extensions | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Threading.Tasks | 4.3.0 | MS-.NET-Library |
| runtime.any.System.Threading.Timer | 4.3.0 | MS-.NET-Library |
| runtime.debian.8-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.fedora.23-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.fedora.24-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.native.System | 4.3.0 | MS-.NET-Library |
| runtime.native.System.IO.Compression | 4.3.0 | MS-.NET-Library |
| runtime.native.System.Net.Http | 4.3.0 | MS-.NET-Library |
| runtime.native.System.Security.Cryptography.Apple | 4.3.0 | MS-.NET-Library |
| runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.opensuse.13.2-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.opensuse.42.1-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.osx.10.10-x64.runtime.native.System.Security.Cryptography.Apple | 4.3.0 | MS-.NET-Library |
| runtime.osx.10.10-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.rhel.7-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.ubuntu.14.04-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.ubuntu.16.04-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| runtime.ubuntu.16.10-x64.runtime.native.System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.10 | Apache-2.0 |
| SQLitePCLRaw.core | 2.1.10 | Apache-2.0 |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.10 | Apache-2.0 |
| SQLitePCLRaw.provider.e_sqlite3 | 2.1.10 | Apache-2.0 |
| System.AppContext | 4.3.0 | MS-.NET-Library |
| System.Buffers | 4.3.0 | MS-.NET-Library |
| System.Collections | 4.3.0 | MS-.NET-Library |
| System.Collections.Concurrent | 4.3.0 | MS-.NET-Library |
| System.Console | 4.3.0 | MS-.NET-Library |
| System.Diagnostics.Debug | 4.3.0 | MS-.NET-Library |
| System.Diagnostics.Tools | 4.3.0 | MS-.NET-Library |
| System.Diagnostics.Tracing | 4.3.0 | MS-.NET-Library |
| System.Globalization | 4.3.0 | MS-.NET-Library |
| System.Globalization.Calendars | 4.3.0 | MS-.NET-Library |
| System.Globalization.Extensions | 4.3.0 | MS-.NET-Library |
| System.IO | 4.3.0 | MS-.NET-Library |
| System.IO.Compression | 4.3.0 | MS-.NET-Library |
| System.IO.Compression.ZipFile | 4.3.0 | MS-.NET-Library |
| System.IO.FileSystem | 4.3.0 | MS-.NET-Library |
| System.IO.FileSystem.Primitives | 4.3.0 | MS-.NET-Library |
| System.IO.Pipelines | 9.0.1 | MIT |
| System.Linq | 4.3.0 | MS-.NET-Library |
| System.Linq.Expressions | 4.3.0 | MS-.NET-Library |
| System.Memory | 4.5.3 | MIT |
| System.Net.Http | 4.3.0 | MS-.NET-Library |
| System.Net.Primitives | 4.3.0 | MS-.NET-Library |
| System.Net.Sockets | 4.3.0 | MS-.NET-Library |
| System.ObjectModel | 4.3.0 | MS-.NET-Library |
| System.Private.Uri | 4.3.0 | MS-.NET-Library |
| System.Reflection | 4.3.0 | MS-.NET-Library |
| System.Reflection.Emit | 4.3.0 | MS-.NET-Library |
| System.Reflection.Emit.ILGeneration | 4.3.0 | MS-.NET-Library |
| System.Reflection.Emit.Lightweight | 4.3.0 | MS-.NET-Library |
| System.Reflection.Extensions | 4.3.0 | MS-.NET-Library |
| System.Reflection.Primitives | 4.3.0 | MS-.NET-Library |
| System.Reflection.TypeExtensions | 4.3.0 | MS-.NET-Library |
| System.Resources.ResourceManager | 4.3.0 | MS-.NET-Library |
| System.Runtime | 4.3.0 | MS-.NET-Library |
| System.Runtime.Extensions | 4.3.0 | MS-.NET-Library |
| System.Runtime.Handles | 4.3.0 | MS-.NET-Library |
| System.Runtime.InteropServices | 4.3.0 | MS-.NET-Library |
| System.Runtime.InteropServices.RuntimeInformation | 4.3.0 | MS-.NET-Library |
| System.Runtime.Numerics | 4.3.0 | MS-.NET-Library |
| System.Security.Cryptography.Algorithms | 4.3.0 | MS-.NET-Library |
| System.Security.Cryptography.Cng | 4.3.0 | MS-.NET-Library |
| System.Security.Cryptography.Csp | 4.3.0 | MS-.NET-Library |
| System.Security.Cryptography.Encoding | 4.3.0 | MS-.NET-Library |
| System.Security.Cryptography.OpenSsl | 4.3.0 | MS-.NET-Library |
| System.Security.Cryptography.Primitives | 4.3.0 | MS-.NET-Library |
| System.Security.Cryptography.X509Certificates | 4.3.0 | MS-.NET-Library |
| System.Text.Encoding | 4.3.0 | MS-.NET-Library |
| System.Text.Encoding.Extensions | 4.3.0 | MS-.NET-Library |
| System.Text.Encodings.Web | 9.0.1 | MIT |
| System.Text.Json | 9.0.1 | MIT |
| System.Text.RegularExpressions | 4.3.0 | MS-.NET-Library |
| System.Threading | 4.3.0 | MS-.NET-Library |
| System.Threading.Tasks | 4.3.0 | MS-.NET-Library |
| System.Threading.Timer | 4.3.0 | MS-.NET-Library |
| System.Xml.ReaderWriter | 4.3.0 | MS-.NET-Library |
| System.Xml.XDocument | 4.3.0 | MS-.NET-Library |
| Tomlyn | 0.19.0 | BSD-2-Clause |

### Test and development only

| Package | Version | License |
| --- | --- | --- |
| AngleSharp | 1.2.0 | MIT |
| AngleSharp.Css | 1.0.0-beta.154 | MIT |
| AngleSharp.Diffing | 1.0.0 | MIT |
| bunit | 1.40.0 | MIT |
| bunit.core | 1.40.0 | MIT |
| bunit.web | 1.40.0 | MIT |
| Castle.Core | 5.1.1 | Apache-2.0 |
| FluentAssertions | 6.0.0 | Apache-2.0 |
| FluentAssertions | 6.1.0 | Apache-2.0 |
| FluentAssertions.Json | 6.1.0 | Apache-2.0 |
| Microsoft.ApplicationInsights | 2.22.0 | MIT |
| Microsoft.AspNetCore.Components.Authorization | 8.0.11 | MIT |
| Microsoft.AspNetCore.Components.WebAssembly.Authentication | 8.0.11 | MIT |
| Microsoft.Bcl.AsyncInterfaces | 6.0.0 | MIT |
| Microsoft.CodeCoverage | 17.11.1 | MIT |
| Microsoft.Extensions.Localization.Abstractions | 8.0.11 | MIT |
| Microsoft.NET.Test.Sdk | 17.11.1 | MIT |
| Microsoft.Playwright | 1.49.0 | MIT |
| Microsoft.Testing.Extensions.Telemetry | 1.4.1 | MIT |
| Microsoft.Testing.Extensions.TrxReport.Abstractions | 1.4.1 | MIT |
| Microsoft.Testing.Extensions.VSTestBridge | 1.4.1 | MIT |
| Microsoft.Testing.Platform | 1.4.1 | MIT |
| Microsoft.Testing.Platform.MSBuild | 1.4.1 | MIT |
| Microsoft.TestPlatform.ObjectModel | 17.11.1 | MIT |
| Microsoft.TestPlatform.TestHost | 17.11.1 | MIT |
| Moq | 4.20.70 | BSD-3-Clause |
| MSTest.Analyzers | 3.6.1 | MIT |
| MSTest.TestAdapter | 3.6.1 | MIT |
| MSTest.TestFramework | 3.6.1 | MIT |
| Newtonsoft.Json | 13.0.1 | MIT |
| System.ComponentModel.Annotations | 5.0.0 | MIT |
| System.Configuration.ConfigurationManager | 4.4.0 | MIT |
| System.Diagnostics.DiagnosticSource | 5.0.0 | MIT |
| System.Diagnostics.EventLog | 6.0.0 | MIT |
| System.Reflection.Metadata | 1.6.0 | MIT |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 | MIT |
| System.Security.Cryptography.ProtectedData | 4.4.0 | MIT |
| System.Text.Encodings.Web | 6.0.0 | MIT |
| System.Text.Json | 6.0.10 | MIT |

## License texts

### MIT License

Used by FluentResults, Ant Design Blazor, OneOf and PrismJS. Also used by every Microsoft package
marked MIT above: EF Core, `Microsoft.Extensions.*`, ASP.NET Core and `System.*`, and by the .NET
runtime and the project templates. Their copyright notices:

- Copyright (c) 2021 Michael Altmann (FluentResults)
- Copyright (c) .NET Foundation and Contributors (Ant Design Blazor)
- Copyright (c) 2016 Harry McIntyre (OneOf)
- Copyright (c) 2012 Lea Verou (PrismJS)
- Copyright (c) .NET Foundation and Contributors, © Microsoft Corporation (EF Core, ASP.NET Core,
  `Microsoft.Extensions.*`, `System.*`, the .NET runtime and the Blazor project templates)
- Copyright (c) 2024-2025 Universal, Open, Free, and Transparent Computer Poker Research Group (the
  PHH dataset, reproduced in full above)

The license text, identical for each of them:

```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Apache License 2.0

Used by SQLitePCLRaw (Copyright 2014-2024 SourceGear, LLC) and
`Microsoft.AspNetCore.Components.DataAnnotations.Validation` (© Microsoft Corporation).

```
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
```

### BSD 2-Clause License

Used by Tomlyn.

```
Copyright (c) 2019-2026, Alexandre Mutel
All rights reserved.

Redistribution and use in source and binary forms, with or without modification
, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

### SQLite

SQLite is in the public domain. Its authors disclaim copyright to the source code and, in place of a
legal notice, offer this blessing:

```
May you do good and not evil.
May you find forgiveness for yourself and forgive others.
May you share freely, never taking more than you give.
```

See <https://www.sqlite.org/copyright.html>.

### Microsoft .NET Library License

This license is carried by the legacy `System.* 4.3.0`, `runtime.*`, `Microsoft.NETCore.*` 1.1.0 and
`NETStandard.Library` 1.6.1 packages listed above, and is published at
<http://go.microsoft.com/fwlink/?LinkId=329770>. As
[What ships](#what-ships-and-under-which-license) explains, the published demo contains no assembly
from these packages. The .NET Foundation has since released the same code under MIT in
[dotnet/runtime](https://github.com/dotnet/runtime).
