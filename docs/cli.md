---
title: CLI usage
---

The `bicep-local-docgen` CLI provides commands for generating documentation from Bicep model source code.

## Installation

Install `bicep-local-docgen` as a global tool:

```bash
dotnet tool install -g bicep-local-docgen
```

Or as a local tool in your project:

```bash
dotnet tool install bicep-local-docgen
```

## Commands

### generate

Generate documentation from source files:

```bash
bicep-local-docgen generate [directoryOrFile...] [options]
```

#### Arguments

- `directoryOrFile` - One or more paths to directories or files to process (default: current directory)

#### Options

- `--output, -o <path>` - Output directory for generated documentation (default: docs)
- `--ignore-path <path>` - Path to ignore file (.biceplocalgenignore)
- `--log-level <level>` - Log level: Critical, Debug, Error, Information (default), None, Trace, Warning
- `--verbose, -v` - Enable verbose logging
- `--force, -f` - Overwrite existing files

#### Examples

Generate docs from current directory to docs folder:

```bash
bicep-local-docgen generate
```

Generate docs from specific directory:

```bash
bicep-local-docgen generate src/Models
```

Generate docs from multiple directories with custom output:

```bash
bicep-local-docgen generate src/Models src/Resources --output documentation
```

Generate with verbose output and force overwrite:

```bash
bicep-local-docgen generate --verbose --force
```

Process specific files:

```bash
bicep-local-docgen generate Models/Widget.cs Models/Resource.cs
```

### check

Validate that models have required documentation attributes (useful for CI/CD):

```bash
bicep-local-docgen check [directoryOrFile...] [options]
```

<!-- markdownlint-disable MD024 -->
#### Arguments

- `directoryOrFile` - One or more paths to directories or files to check (default: current directory)

#### Options

- `--ignore-path <path>` - Path to ignore file (.biceplocalgenignore)
- `--log-level <level>` - Log level: Critical, Debug, Error, Information (default), None, Trace, Warning
- `--include-extended` - Include validation for BicepFrontMatter and BicepDocCustom attributes
- `--verbose, -v` - Enable verbose logging

#### Examples

Check models in current directory:

```bash
bicep-local-docgen check
```

Check specific directory:

```bash
bicep-local-docgen check src/Models
```

Check multiple directories with verbose output:

```bash
bicep-local-docgen check src/Models src/Resources --verbose
```

Check with extended validation (includes BicepFrontMatter and BicepDocCustom):

```bash
bicep-local-docgen check --include-extended
```

Check specific files:

```bash
bicep-local-docgen check Models/Widget.cs Models/Button.cs
```

Check with custom ignore file:

```bash
bicep-local-docgen check --ignore-path .custom-ignore
```

## Exit Codes

- `0` - Success
- `1` - Error occurred
- `2` - Validation failures found (check command only)
