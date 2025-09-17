# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-09-17

### Fixed

- Fixed images and link

## [1.0.0] - 2025-09-17

### Added

- Initial release of Bicep.LocalDeploy.DocGenerator CLI tool
- Initial release of Bicep.LocalDeploy library for model annotations
- Documentation generation from C# Bicep models with attributes
- Support for `.biceplocalgenignore` files to exclude files from processing
- Two main CLI commands:
  - `generate`: Generate Markdown documentation from Bicep models
  - `check`: Validate that models have required documentation attributes
- Comprehensive attribute system for customizing documentation:
  - `BicepDocCustomAttribute`: Add custom documentation content
  - `BicepDocExampleAttribute`: Provide usage examples
  - `BicepDocHeadingAttribute`: Control heading structure
  - `BicepFrontMatterAttribute`: Add front matter to generated files
- Support for multiple target frameworks (.NET 9.0 and .NET 10.0)
- Configurable output directory and file patterns
- Verbose logging support
- Force overwrite option for existing files

### Features

- **Ignore file Support**: Full support for `.biceplocalgenignore` files with glob
  patterns
  - Single filename patterns (e.g., `Widget.cs`)
  - Directory patterns (e.g., `Models/**`)
  - Wildcard patterns (e.g., `Generated/*.cs`)
  - Relative path patterns (e.g., `Models/Widget.cs`)
  - Comments support (lines starting with `#`)
- **Cross-Platform**: Works on Windows, macOS, and Linux

<!-- Link reference definitions -->
[1.0.0]: https://github.com/Gijsreyn/bicep-local-docgen/releases/tag/v1.0.0
[1.0.1]: https://github.com/Gijsreyn/bicep-local-docgen/releases/tag/v1.0.1
