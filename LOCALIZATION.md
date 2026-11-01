# Interface languages

GRF Editor supports English and Korean. Select **Tools > Language > English / 한국어**
(**도구 > 언어** in Korean), then restart the editor. The choice is saved in the
existing configuration file. The default is English. Selecting a language does
not close the current archive or restart the application automatically.

Menus, context menus, window titles, settings, tooltips, progress labels, file
dialog descriptions, command descriptions, and application messages use the
shared resources in `Utilities/Localization/UiStrings.resx` (English) and
`Utilities/Localization/UiStrings.ko.resx` (Korean).

Archive paths, file contents, input defaults, encoding choices, numeric parsing,
and configuration identifiers retain their original values. Exception objects,
stack traces, and copied diagnostic reports retain the original diagnostic text.
Messages supplied by external components fall back to their original text if no
translation is available. Native Windows dialog controls follow Windows settings.

## Adding or updating text

- Use matching resource names in both `.resx` files. Keep composite format fields
  such as `{0}` and `{1:X8}` identical in both languages.
- For XAML, use `{loc:Localize ResourceName}` with the namespace
  `clr-namespace:TokeiLibrary.Localization;assembly=TokeiLibrary`.
- For C# display text, use `Utilities.Localization.UiText.Get("English text")`.
  The English resource value must match the lookup string exactly.
- Format translated templates before inserting filenames or user text.
  `UiText.Message` is also used at display boundaries for existing exception
  messages; do not use it to transform archive data or exception objects.
- New resource names can be descriptive identifiers. The existing `T...` names
  are stable identifiers derived from the English text.

## Build and distribute

Build `GRFEditor/GRFEditor.csproj` with Visual Studio 2022 / MSBuild and the .NET
Framework 4.8 targeting pack. The existing `AnyCPU` configuration is supported.
The build copies the Korean satellite assembly to
`GRFEditor/bin/<Configuration>/ko/Utilities.resources.dll`.
**Keep the `ko` folder beside the executable when distributing the build.**
Without it, .NET falls back to English.

## Verification

After building the editor in Debug, restore and build
`Tests/LocalizationVerification/LocalizationVerification.csproj`, then run its
executable with the repository root as its first argument. It verifies both
resource catalogs, format fields, formatted messages, UI/data culture isolation,
file dialog patterns, persistent language settings, and WPF menu/dialog/settings
rendering in both languages. Screenshots are written under the test executable's
`results` directory. The verification uses its own configuration file.
