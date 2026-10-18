# S1 toolchain provenance and validation limits

Recorded 2026-10-18. Version **requests** are pinned; successful installation/resolution/import is **not verified**. No Unity reference-source implementation has been copied into this project.

| Request | Evidence read over verified HTTPS | Scope of evidence |
| --- | --- | --- |
| Unity 6000.3.26f1 | [Official version-tag README](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3.26f1/README.md); `git ls-remote` tag SHA `c57f9f00d22b3794130a9bfa76dd7c8b32e4f2f1` | Version identity only, not an installed Editor or verified Editor revision/hash. |
| URP 17.3.0 | [Official package metadata](https://github.com/Unity-Technologies/Graphics/blob/2d2e78cc9d6254bc6e7c9c5552cea053508e86cb/Packages/com.unity.render-pipelines.universal/package.json) | Declares Unity 6000.3 and core/shadergraph 17.3.0; registry resolution unrun. |
| Input System 1.17.0 | [Official package metadata](https://github.com/Unity-Technologies/InputSystem/blob/672e42ce4f76a7086fb09e24d5236e5f71336bd6/Packages/com.unity.inputsystem/package.json) | Declares minimum Unity 2022.3; not tested here. |
| uGUI 2.0.0 (includes TMP source) | [Official Unity 6000.3 branch metadata](https://github.com/Unity-Technologies/uGUI/blob/48376fb25008c8310adfb0e912b8e9654c37feaf/com.unity.ugui/package.json) | Package source/version identity; no separate TextMeshPro package requested. |
| Test Framework 1.4.5 | [Official Input System project manifest](https://github.com/Unity-Technologies/InputSystem/blob/672e42ce4f76a7086fb09e24d5236e5f71336bd6/Packages/manifest.json) | An official project requests this version; compatibility with this foundation remains untested. |
| Built-in imgui/jsonserialize/ui/uielements modules 1.0.0 | Same official project manifest and package module dependencies | Module requests, not independent runtime installs. |

The release API, Unity documentation and UPM registry returned proxy 403 during this task. Official raw GitHub metadata and Git read operations were accessible. No TLS/signature verification was disabled. No Unity Editor, Hub or usable activation was found. Do not confuse a source-tag SHA with an installer checksum or `m_EditorVersionWithRevision`; the actual Editor must generate/verify its revision.

URP/Mono/.NET Standard settings are configured by `FoundationProjectSetup.Configure` only after a licensed Editor resolves packages and compiles scripts. Until then, URP assets and full generated project settings are missing. No package lockfile is hand-authored. A different required package/editor version must be documented and validated rather than silently substituted.

Newtonsoft JSON and OpenCvSharp are future migration dependencies, **not imported by S1**. Input activation, TMP font assets, the maximized Windows adapter and application graphics are later work. The temporary diagnostic uses OnGUI solely to make the foundation state unmistakable; it is not the final UI system.

S2a adds a no-engine-reference `CrowdCheckpoint.Simulation` source assembly with System.Numerics vectors. Its exact helper sources compile in a separate SDK project against .NET Standard 2.1 with C# 9, explicit imports and warnings as errors. The reference-comparison host targets .NET 8 and reuses the original xUnit/Test SDK versions; no Unity package requests changed. This is an API/language portability check, not compilation or execution by Unity's toolchain. Cloud runner setup is deferred; source stages can proceed while actual S1/S2 Unity gates remain incomplete.

S1c adds Python 3.9+ standard-library orchestration without a runner; no package/version/backend requests changed. Local synthetic process-boundary tests are separate from actual Editor validation. The foundation setup/receipt/settings tests remain uncompiled and unrun in Unity. Source inspection reconfirmed the public URP asset Create API in the same pinned Graphics source; this does not establish installed package compatibility.
