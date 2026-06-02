#!/usr/bin/env pwsh
#Requires -Version 7.2

#####################################################################
#
# Note: 
#    Be careful to synchronize changes to `Directory.Build.targets`
#    when making changes to paths.
#
#####################################################################

<#
.SYNOPSIS
  Builds XNA CnCNet Client using specified parameters.
.DESCRIPTION
  You can use this script to make publish packages for your game.
.PARAMETER IsDebug
  Build projects in debug mode.
.PARAMETER Log
  Detail log.
.PARAMETER NoClean
  Do not clean Compiled folder.
.PARAMETER NoMove
  Do not make folder structure.
.EXAMPLE
  build.ps1
  Build.
.EXAMPLE
  build.ps1 -IsDebug
  Build on debug mode.
#>
param(
  [Parameter()]
  [switch]
  $IsDebug,
  [Parameter()]
  [switch]
  $Log,
  [Parameter()]
  [switch]
  $NoClean,
  [Parameter()]
  [switch]
  $NoMove
)

$Script:Configuration = 'Release'
if ($IsDebug) {
  $Script:Configuration = 'Debug'
}

$Script:RepoRoot = Split-Path $PSScriptRoot
$Script:ProjectPath = Join-Path $RepoRoot 'AvMainClientExe' 'AvMainClientExe.csproj'
$Script:CompiledRoot = Join-Path $RepoRoot 'Compiled'
$Script:FrameworkBinariesFolderMap = @{
  'net48'          = 'Binaries'
  'net8.0'         = 'BinariesNET8'
  # 'net8.0-windows' = 'BinariesNET8'
}

if (!$NoClean -AND (Test-Path $Script:CompiledRoot)) {
  Remove-Item -Recurse -Force -LiteralPath $Script:CompiledRoot
}

function Script:Invoke-BuildProject {
  [CmdletBinding(DefaultParameterSetName = 'ByGame')]
  param (
    [Parameter(Mandatory, ParameterSetName = 'Detail')]
    [string]
    $Framework
  )
  
  process {
    if ($Framework) {
      $Output = Join-Path $CompiledRoot 'Resources' ($FrameworkBinariesFolderMap[$Framework])

      $Private:ArgumentList = [System.Collections.Generic.List[string]]::new(11)
      $Private:ArgumentList.Add('publish')
      $Private:ArgumentList.Add("$ProjectPath")
      $Private:ArgumentList.Add('--graph')
      $Private:ArgumentList.Add("--configuration:$Script:Configuration")
      $Private:ArgumentList.Add("--framework:$Framework")
      $Private:ArgumentList.Add("--output:$Output")
      $Private:ArgumentList.Add('-property:SatelliteResourceLanguages=en')
      if ($Log) {
        $Private:ArgumentList.Add('-verbosity:diagnostic')
      }
      if ($NoMove) {
        $Private:ArgumentList.Add('-property:NoMove=true')
      }
      # $Private:ArgumentList.Add("-property:AssemblyVersion=$AssemblySemVer")
      # $Private:ArgumentList.Add("-property:FileVersion=$AssemblySemFileVer")
      # $Private:ArgumentList.Add("-property:InformationalVersion=$InformationalVersion")
  
      & 'dotnet' $Private:ArgumentList
      if ($LASTEXITCODE) {
        throw "Build failed for $Script:Configuration $Framework (exit code $LASTEXITCODE)"
      }
    }
    else {
      Write-Host "Building for all frameworks..." -ForegroundColor Cyan
      foreach ($Framework in $FrameworkBinariesFolderMap.Keys) {
        Script:Invoke-BuildProject -Framework $Framework
      }
    }
  }
}

Script:Invoke-BuildProject
