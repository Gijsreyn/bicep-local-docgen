#Requires -Version 7.0
[CmdletBinding()]
param (
    [ValidateSet("Release", "Debug")]
    [string]
    $Configuration = "Release",

    [switch]
    $Clean
)

function getNetPath
{
    # Read version from global.json
    $globalJsonPath = Join-Path $PSScriptRoot 'global.json'
    $dotnetVersion = $null
    
    if (Test-Path $globalJsonPath)
    {
        if (Test-Json -Path $globalJsonPath)
        {
            $jsonContent = Get-Content $globalJsonPath -Raw | ConvertFrom-Json

            $dotnetVersion = $jsonContent.sdk.version
        }
        else
        {
            throw "Invalid JSON format in global.json"
        }
    }

    # TODO: Support multiple OS platforms
    if ($IsWindows)
    {
        $dotnetPath = Join-Path $env:ProgramFiles 'dotnet' 'sdk' $dotnetVersion
        if (-not (Test-Path $dotnetPath))
        {
            throw "Required .NET SDK version $dotnetVersion not found. Please install it using 'winget install Microsoft.DotNet.SDK.9'"
        }

        $dotnetExe = Join-Path (Split-Path (Split-Path $dotnetPath -Parent) -Parent) 'dotnet.exe'
    } 
    elseif ($IsLinux)
    {
        $dotnetExe = (Get-Command dotnet).Source
    }

    return $dotnetExe
}

function getProjectPath ()
{
    $projectPath = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter "*.csproj" -File -ErrorAction Ignore | Select-Object -First 1
    if ($null -eq $projectPath)
    {
        Write-Error "Project file '*.csproj' not found in the script directory or its subdirectories."
        return
    }

    return $projectPath.FullName
}

function getExeName($projectPath)
{
    [xml]$csprojXml = Get-Content $projectPath
    $exeName = $csprojXml.Project.PropertyGroup.AssemblyName
    return $exeName
}

function testBicepExe
{
    $bicepExe = Get-Command bicep -CommandType Application -ErrorAction Ignore 
    if (-not $bicepExe)
    {
        return $false
    }

    return $bicepExe.Source
}

# Variables
$errorActionPreference = 'Stop'
$outputDirectory = Join-Path $PSScriptRoot 'output'
$dotNetExe = getNetPath
$projectPath = getProjectPath

if ($Clean.IsPresent)
{
    if (Test-Path $outputDirectory)
    {
        Write-Verbose "Removing output directory '$outputDirectory'." -Verbose
        Remove-Item -Path $outputDirectory -Recurse -Force
    }

    $cleanParams = @(
        'clean', $projectPath,
        '-c', $Configuration,
        '-nologo'
    )
    Write-Verbose "Cleaning project '$ProjectName' with" -Verbose
    Write-Verbose ($cleanParams | ConvertTo-Json | Out-String) -Verbose
    $null = & $dotNetExe @cleanParams
}

if ($Configuration -eq 'Release')
{
    # Build the solution
    try
    {
        Push-Location (Join-Path $PSScriptRoot 'src')
        $buildParams = @(
            'build',
            $projectPath,
            '-c', $Configuration
        )

        Write-Verbose "Building solution '$projectPath' with" -Verbose
        Write-Verbose ($buildParams | ConvertTo-Json | Out-String) -Verbose
        $res = & $dotNetExe @buildParams 

        if ($LASTEXITCODE -ne 0)
        {
            throw $res
        }
    }
    finally
    {
        Pop-Location
    }

    $platforms = @('win-x64', 'linux-x64', 'osx-x64')
    $extensionParams = @(
        'publish-extension'
    )

    $exeName = getExeName $projectPath
    $targetName = Join-Path $outputDirectory $exeName

    foreach ($platform in $platforms)
    {
        $out = (Join-Path $outputDirectory $platform)

        try
        {
            Push-Location (Join-Path $PSScriptRoot 'src')
            $publishParams = @(
                'publish',
                '-c', $Configuration,
                '-r', $platform,
                '-o', $out
            )
    
            Write-Verbose "Publishing project for platform '$platform' with" -Verbose
            Write-Verbose ($publishParams | ConvertTo-Json | Out-String) -Verbose
            $res = & $dotNetExe @publishParams 

            if ($LASTEXITCODE -ne 0)
            {
                throw $res
            }

            if ($platform -eq 'win-x64')
            {
                $extensionParams += @("--bin-$platform", (Join-Path $out "$exeName.exe"))
            }
            else
            {
                $extensionParams += @("--bin-$platform", (Join-Path $out $exeName))
            }
        }
        finally
        {
            Pop-Location
        }
    }

    $extensionParams += @(
        '--target', $targetName,
        '--force'
    )

     $bicepExe = testBicepExe

    if ($bicepExe)
    {
        Write-Verbose -Message "Bicep CLI found at $bicepExe" -Verbose
        Write-Verbose -Message "Compiling Bicep files for extension '$targetName'." -Verbose
        Write-Verbose -Message ($extensionParams | ConvertTo-Json | Out-String) -Verbose
        & $bicepExe @extensionParams
    }
    else
    {
        Write-Warning "Bicep CLI is not installed. Skipping Bicep compilation."
        return
    }
}