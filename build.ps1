param
(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Command
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetName = (Get-ChildItem -Path "$PSScriptRoot\src" -Recurse -Filter *.csproj | select -First 1).Basename
$distDir = "$PSScriptRoot\dist"

function removePath($path)
{
    while ($true)
    {
        if (!(Test-Path $path))
        {
            return
        }

        Write-Host "Deleting $path"
        try
        {
            Remove-Item -Recurse $path
            break
        }
        catch
        {
            Write-Host "Could not remove $path, will retry"
            Start-Sleep 3
        }
    }
}

function getInstallDir
{
    Write-Host "Looking for RimWorld installation..."
    $installSubDir = "Steam\SteamApps\common\RimWorld"
    $installDir = "$(${Env:ProgramFiles(x86)})\$($installSubDir)"
    Write-Host "Checking $installDir"
    if (Test-Path $installDir)
    {
        return $installDir
    }

    $installDir = "$($Env:ProgramFiles)\$($installSubDir)"
    Write-Host "Checking $installDir"
    if (Test-Path $installDir)
    {
        return $installDir
    }

    Write-Host "RimWorld not found"
    return $null
}

$installDir = getInstallDir

function getProjectDir
{
    return "$PSScriptRoot\src\$targetName"
}

$assemblyInfoFile = "$(getProjectDir)\properties\AssemblyInfo.cs"

function getGameVersion
{
    $gameVersionFile = "$installDir\Version.txt"
    $gameVersionWithRev = Get-Content $gameVersionFile
    $version = [version] ($gameVersionWithRev.Split(" "))[0]

    return "$($version.Major).$($version.Minor)"
}

function updateToGameVersion
{
    if (!$installDir)
    {
        Write-Host -ForegroundColor Red `
            "Rimworld installation not found; not setting game version."

        return
    }

    $gameVersion = getGameVersion

    $content = Get-Content -Raw $assemblyInfoFile
    $newContent = $content -replace '"\d+\.\d+(\.\d+\.\d+")', "`"$gameVersion`$1"

    if ($newContent -eq $content)
    {
        return
    }
    Set-Content -Encoding UTF8 -Path $assemblyInfoFile $newContent
}

function copyDependencies
{
    $thirdpartyDir = "$PSScriptRoot\ThirdParty"
    if (Test-Path "$thirdpartyDir\*.dll")
    {
        return
    }

    if (!$installDir)
    {
        Write-Host -ForegroundColor Red `
            "Rimworld installation not found; see Readme for how to set up pre-requisites manually."

        exit 1
    }

    $depsDir = "$installDir\RimWorldWin64_Data\Managed"
    Write-Host "Copying dependencies from installation directory"
    if (!(Test-Path $thirdpartyDir)) { mkdir $thirdpartyDir | Out-Null }
    Copy-Item -Force "$depsDir\Unity*.dll" "$thirdpartyDir\"
    Copy-Item -Force "$depsDir\Assembly-CSharp.dll" "$thirdpartyDir\"
}

function doPreBuild
{
    removePath $distDir
    copyDependencies
    updateToGameVersion
}

function doPostBuild
{
    $distTargetDir = "$distDir\$targetName"
    removePath $distDir

    # Create dist directory structure
    if (!(Test-Path $distTargetDir))
    {
        mkdir $distTargetDir -Force | Out-Null
    }
    
    # Copy mod structure
    Copy-Item -Recurse -Force "$PSScriptRoot\mod-structure\*" $distTargetDir
    
    # Handle 1.6 build
    $targetDir16 = "$(getProjectDir)\bin\Release"
    $targetPath16 = "$targetDir16\$targetName.dll"
    if (Test-Path $targetPath16)
    {
        $distAssemblyDir16 = "$distTargetDir\1.6\Assemblies"
        mkdir $distAssemblyDir16 -Force | Out-Null
        Copy-Item -Force $targetPath16 $distAssemblyDir16
        
        $modStructureAssemblyLocation16 = "$PSScriptRoot\mod-structure\1.6\Assemblies"
        if (!(Test-Path $modStructureAssemblyLocation16))
        {
            mkdir $modStructureAssemblyLocation16 -Force | Out-Null
        }
        Copy-Item -Force $targetPath16 $modStructureAssemblyLocation16
    }
    
    # Handle 1.5 build
    $targetDir15 = "$(getProjectDir)\bin\Release_1.5"
    $targetPath15 = "$targetDir15\$targetName.dll"
    if (Test-Path $targetPath15)
    {
        $distAssemblyDir15 = "$distTargetDir\1.5\Assemblies"
        mkdir $distAssemblyDir15 -Force | Out-Null
        Copy-Item -Force $targetPath15 $distAssemblyDir15
        
        $modStructureAssemblyLocation15 = "$PSScriptRoot\mod-structure\1.5\Assemblies"
        if (!(Test-Path $modStructureAssemblyLocation15))
        {
            mkdir $modStructureAssemblyLocation15 -Force | Out-Null
        }
        Copy-Item -Force $targetPath15 $modStructureAssemblyLocation15
    }

    Write-Host "Creating distro package"
    $content = Get-Content -Raw $assemblyInfoFile
    if (!($content -match '"(\d+\.\d+\.\d+\.\d+)"'))
    {
        throw "Version info not found in $assemblyInfoFile"
    }

    $version = $matches[1]
    $distZip = "$distDir\$targetName.$version.zip"
    removePath $distZip
    $sevenZip = "$PSScriptRoot\7z.exe"
    & $sevenZip a -mx=9 "$distZip" "$distDir\*"
    if ($LASTEXITCODE -ne 0)
    {
        throw "7zip command failed"
    }

    Write-Host "Created $distZip"


    if (!$installDir)
    {
        Write-Host -ForegroundColor Yellow `
            "No Steam installation found, build will not be published"

        return
    }

    $modsDir = "$installDir\Mods"
    $modDir = "$modsDir\$targetName"
    removePath $modDir

    Write-Host "Copying mod to $modDir"
    Copy-Item -Recurse -Force -Exclude *.zip "$distDir\*" $modsDir
}

& $Command