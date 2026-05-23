Push-Location "$PSScriptRoot"

# Version we want to download for doxygen
$RequestedVersion = "1.12.0"

# Downloads the requested doxygen version
function Get-Doxgen {
    param (
        $Version
    )

    # Convert it to how Doxygen does tags
    $FriendlyVersion = $Version.Replace(".", "_")

    $ApiUrl = "https://api.github.com/repos/doxygen/doxygen/releases/tags/Release_$FriendlyVersion"
    $FileName = "doxygen-$Version.windows.x64.bin.zip"

    $Response = Invoke-RestMethod -Uri $ApiUrl
    if (!$Response)
    {
        Write-Error "Failed to get response from $ApiUrl"
        exit
    }

    $FoundAssetUrl = ""
    foreach($Asset in $Response.assets) {
        if ($Asset.name -eq $FileName)
        {
            $FoundAssetUrl = $Asset.browser_download_url
            break;
        }

    }

    if ([string]::IsNullOrEmpty($FoundAssetUrl))
    {
        Write-Error "Failed to get asset url for doxygen!"
        exit
    }

    Write-Host "Downloading $FileName"
    Invoke-WebRequest -Uri $FoundAssetUrl -OutFile "doxygen/$FileName"

    Write-Host "Extracting $FileName to doxygen/bin"
    Expand-Archive -Path "doxygen/$FileName" -DestinationPath "doxygen/bin"

    # Remove old archive
    Remove-Item -Path "doxygen/$FileName"
}


Write-Host "Generating documentation..."

If(!(Test-Path -Path ".\doxygen\bin\doxygen.exe" -PathType Leaf))
{
    # Check if folder exiss, if not create it
    If(!(Test-Path -PathType Container "doxygen/bin"))
    {
        # Create it silently
        New-Item -Path 'doxygen/bin' -ItemType Directory | Out-Null
    }

    Write-Host "Doxygen was not found, pulling $RequestedVersion from GitHub"

    # Get doxygen
    Get-Doxgen -Version $RequestedVersion
}

# Check if the generated folder exists, if not create it
If(!(Test-Path -PathType Container "Generated"))
{
    # Create it silently
    New-Item -Path 'Generated' -ItemType Directory | Out-Null
}

# Go to project root
Push-Location "$PSScriptRoot/../"

# Make docs
$env:PROJECT_NUMBER = 'LOCAL'; &".\Documentation\doxygen\bin\doxygen.exe" "Documentation\doxygen\doxyfile"

# Wait for user responds
Write-Host -NoNewLine 'Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');

# Reset location from both stacks
Pop-Location
Pop-Location
