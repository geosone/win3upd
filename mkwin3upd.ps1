<#
.SYNOPSIS
    Builds a German update CD for Windows 3.1 and Windows for Workgroups
    3.11 with the GeosOne Dual Installer.

.DESCRIPTION
    The script downloads the updates listed in downloads.txt (Microsoft
    downloads from the Internet Archive, German files from www.gaby.de),
    checks their SHA-256, takes the files named in pkgs\<package>\FILES.TXT
    out of them and writes one directory per package with INSTALL.EXE and
    INSTALL.INI.  The result is the folder -Out\CD and an ISO 9660 image.

    The script contains no Microsoft files; everything comes from the
    downloads.

.PARAMETER Cache
    Folder for the downloads and their unpacked files (default:
    win3upd-cache in the temp folder).  It is kept, the next run uses it
    again.

.PARAMETER Out
    Output folder (default: .\out): CD\ and WIN3UPD.ISO.

.PARAMETER Package
    Build only these packages (names of folders in pkgs\).

.PARAMETER Installer
    INSTALL.EXE of the GeosOne Dual Installer (default: .\INSTALL.EXE, then
    ..\gdi\build\INSTALL.EXE).

.PARAMETER NoIso
    Only write the folder CD\.

.EXAMPLE
    .\mkwin3upd.ps1
    .\mkwin3upd.ps1 -Package CTL3D,WINFILE -NoIso

    Copyright (C) 2026 GeosOne.  GNU General Public License version 3.
#>
[CmdletBinding()]
param(
    [string]$Cache = (Join-Path ([IO.Path]::GetTempPath()) 'win3upd-cache'),
    [string]$Out = (Join-Path (Get-Location) 'out'),
    [string[]]$Package = @(),
    [string]$Installer,
    [switch]$NoIso
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
. (Join-Path $PSScriptRoot 'tools/GdiDisk.ps1')
if (-not ('GeosOne.Win3Upd.Iso9660' -as [type])) {
    Add-Type -TypeDefinition (Get-Content -Raw -Path (Join-Path $PSScriptRoot 'tools/W3Build.cs')) -Language CSharp
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# --- text files in code page 850 with CR LF ----------------------------------

if ('System.Text.CodePagesEncodingProvider' -as [type]) {
    [Text.Encoding]::RegisterProvider([Text.CodePagesEncodingProvider]::Instance)
}
$cp850 = [Text.Encoding]::GetEncoding(850)

function ConvertTo-DosText([string]$text) {
    $t = ($text -replace "`r`n", "`n" -replace "`n", "`r`n")
    if (-not $t.EndsWith("`r`n")) { $t += "`r`n" }
    return $cp850.GetBytes($t)
}

function Read-Utf8([string]$path) {
    return [IO.File]::ReadAllText($path, (New-Object Text.UTF8Encoding($false, $true))).TrimStart([char]0xFEFF)
}

# --- downloads -----------------------------------------------------------------

# downloads.txt: id  sha256  url
$downloads = @{}
foreach ($l in (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'downloads.txt'))) {
    if ($l -match '^\s*(#|$)') { continue }
    $f = $l.Trim() -split '\s+'
    if ($f.Count -ne 3) { throw "downloads.txt: invalid line: $l" }
    $downloads[$f[0].ToLowerInvariant()] = @{ Sha = $f[1].ToLowerInvariant(); Url = $f[2]; File = ($f[2] -split '/')[-1] }
}

function Get-Download([string]$id) {
    $d = $downloads[$id.ToLowerInvariant()]
    if (-not $d) { throw "Unknown download: $id (downloads.txt)" }
    $dir = Join-Path $Cache 'download'
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $path = Join-Path $dir $d.File
    if ((Test-Path -LiteralPath $path) -and [GeosOne.Win3Upd.Hash]::Sha256($path) -eq $d.Sha) { return $path }
    Write-Host "Downloading $($d.File) ..."
    $tmp = "$path.part"
    for ($try = 1; ; $try++) {
        try {
            Invoke-WebRequest -Uri $d.Url -OutFile $tmp -UseBasicParsing
            break
        } catch {
            if ($try -ge 3) { throw "Download failed: $($d.Url)`n$_" }
            Start-Sleep -Seconds (5 * $try)
        }
    }
    $sha = [GeosOne.Win3Upd.Hash]::Sha256($tmp)
    if ($sha -ne $d.Sha) {
        Remove-Item -LiteralPath $tmp
        throw "$($d.File): wrong SHA-256 $sha (expected $($d.Sha))"
    }
    Move-Item -LiteralPath $tmp -Destination $path -Force
    return $path
}

function Get-SevenZip {
    foreach ($c in '7z', '7zz', '7za') {
        $cmd = Get-Command $c -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($cmd) { return $cmd.Source }
    }
    foreach ($p in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if ($p -and (Test-Path -LiteralPath (Join-Path $p '7-Zip\7z.exe'))) { return (Join-Path $p '7-Zip\7z.exe') }
    }
    foreach ($c in '7z.exe', '7zz') {
        $p = Join-Path $PSScriptRoot "tools/7zip/$c"
        if ((Test-Path -LiteralPath $p) -and (($c -eq '7z.exe') -eq ([IO.Path]::DirectorySeparatorChar -eq '\'))) { return $p }
    }
    throw "7-Zip is needed (7z on the PATH, Program Files\7-Zip\7z.exe or tools\7zip)"
}

# unpack an archive (ZIP, CAB, self-extracting EXE, LZH, ...) into $into
function Expand-Archive2([string]$file, [string]$into) {
    $done = Join-Path $into '.done'
    if (Test-Path -LiteralPath $done) { return }
    if (Test-Path -LiteralPath $into) { Remove-Item -LiteralPath $into -Recurse -Force }
    New-Item -ItemType Directory -Path $into | Out-Null
    $7z = Get-SevenZip
    $log = & $7z x -y "-o$into" $file 2>&1
    if ($LASTEXITCODE) {
        # some self-extracting ZIPs (PKSFX) only open as ZIP
        Remove-Item -LiteralPath $into -Recurse -Force
        New-Item -ItemType Directory -Path $into | Out-Null
        try {
            [IO.Compression.ZipFile]::ExtractToDirectory($file, $into)
        } catch {
            throw "Cannot unpack $($file):`n$($log -join "`n")"
        }
        # DOS ZIPs carry no Unix modes, but .NET takes them from the attributes
        if ([IO.Path]::DirectorySeparatorChar -eq '/') {
            Get-ChildItem -LiteralPath $into -Recurse -File | ForEach-Object {
                [IO.File]::SetUnixFileMode($_.FullName, [IO.UnixFileMode]'UserRead, UserWrite, GroupRead, OtherRead')
            }
        }
    }
    [IO.File]::WriteAllText($done, '')
}

# a file below $dir, by a path with / or \ (case does not matter)
function Find-Below([string]$dir, [string]$path) {
    $cur = $dir
    foreach ($part in ($path -split '[\\/]' | Where-Object { $_ })) {
        $hit = Get-ChildItem -LiteralPath $cur -Force | Where-Object { $_.Name -ieq $part } | Select-Object -First 1
        if (-not $hit) { throw "Not found: $path (in $dir)" }
        $cur = $hit.FullName
    }
    return $cur
}

# a source: id:path[!path in that archive ...][#resource type]
#   ctl3d:OBJ16/DLL/CTL3D.DLL     a file of the download ctl3d
#   ww1140:PENTFIX.EXE#VXDRESOURCE  a resource of a Windows program
# Files compressed with COMPRESS.EXE (SZDD, KWAJ) are expanded.
function Read-Source([string]$spec) {
    if ($spec -notmatch '^([\w-]+):([^#]+)(?:#(.+))?$') { throw "Invalid source: $spec" }
    $id, $path, $res = $Matches[1], $Matches[2], $Matches[3]
    $dl = Get-Download $id
    $dir = Join-Path $Cache "x/$($id.ToLowerInvariant())"
    Expand-Archive2 $dl $dir
    $parts = $path -split '!'
    $file = Find-Below $dir $parts[0]
    for ($i = 1; $i -lt $parts.Count; $i++) {
        $inner = "$file.x"
        Expand-Archive2 $file $inner
        $file = Find-Below $inner $parts[$i]
    }
    $data = [IO.File]::ReadAllBytes($file)
    if ($res) { $data = [GeosOne.Win3Upd.NeResource]::Extract($data, $res) }
    if ([GeosOne.Gdi.MsExpand]::IsCompressed($data)) {
        $name = $null
        $data = [GeosOne.Gdi.MsExpand]::Expand($data, [IO.Path]::GetFileName($file), [ref]$name)
    }
    return , $data
}

# --- packages --------------------------------------------------------------------

if (-not $Installer) {
    $Installer = @((Join-Path $PSScriptRoot 'INSTALL.EXE'), (Join-Path $PSScriptRoot '../gdi/build/INSTALL.EXE')) |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $Installer) { throw "INSTALL.EXE not found (-Installer)" }
}
$pkgRoot = Join-Path $PSScriptRoot 'pkgs'
$names = @(Get-ChildItem -LiteralPath $pkgRoot -Directory | ForEach-Object { $_.Name } | Sort-Object)
if ($Package.Count) {
    foreach ($p in $Package) { if ($names -notcontains $p.ToUpperInvariant()) { throw "No package $p in pkgs\" } }
    $names = @($names | Where-Object { $Package -contains $_ })
}
$textDe = Read-Utf8 (Join-Path $pkgRoot 'TEXT_DE.TXT')
$cd = Join-Path $Out 'CD'
if (Test-Path -LiteralPath $cd) { Remove-Item -LiteralPath $cd -Recurse -Force }
New-Item -ItemType Directory -Force -Path $cd | Out-Null
$index = @()

foreach ($name in $names) {
    Write-Host "Package $name"
    $src = Join-Path $pkgRoot $name
    $dst = Join-Path $cd $name
    New-Item -ItemType Directory -Path $dst | Out-Null
    $ini = Read-Utf8 (Join-Path $src 'INSTALL.TXT')
    # FILES.TXT: [condition|]target  source
    $files = @()
    $used = @{}
    foreach ($l in ((Read-Utf8 (Join-Path $src 'FILES.TXT')) -split "`r?`n")) {
        if ($l -match '^\s*(;|$)') { continue }
        if ($l -notmatch '^\s*(?:([^|\s]*)\|)?(\S+)\s+(\S+)(?:\s+-"([^"]+)")?\s*$') { throw "$name\FILES.TXT: invalid line: $l" }
        $cond, $target, $spec, $cut = $Matches[1], $Matches[2], $Matches[3], $Matches[4]
        $base = ($target -split '\\')[-1].ToUpperInvariant()
        $key = "$spec -$cut"
        if ($used.ContainsKey($key)) {
            # the same file for another target: once on the CD
            $cdName = $used[$key]
        } else {
            # the same name twice (alternatives): in A\, B\, ...
            $cdName = $base
            for ($n = 0; $used.ContainsValue($cdName); $n++) { $cdName = "$([char](65 + $n))\$base" }
            $used[$key] = $cdName
            if ($spec -match '^local:(.+)$') {
                # a text of this package (UTF-8): code page 850, CR LF
                $data = ConvertTo-DosText (Read-Utf8 (Join-Path $src $Matches[1]))
            } else {
                $data = Read-Source $spec
            }
            if ($cut) {
                # -"text": a text file without this text (OEMSETUP.INF)
                $t = [Text.Encoding]::GetEncoding(28591).GetString($data)
                if (-not $t.Contains($cut)) { throw "$name\FILES.TXT: not in $($spec): $cut" }
                $data = [Text.Encoding]::GetEncoding(28591).GetBytes($t.Replace($cut, ''))
            }
            $target2 = Join-Path $dst $cdName
            New-Item -ItemType Directory -Force -Path (Split-Path $target2) | Out-Null
            [IO.File]::WriteAllBytes($target2, $data)
        }
        $files += $(if ($null -ne $cond) { "$cond|" } else { '' }) + "$cdName=$target"
    }
    $text = $ini.TrimEnd() + "`n`n[Files]`n" + ($files -join "`n") + "`n`n" + $textDe
    # the engine reads lines of up to 511 characters
    $long = @($text -split "`n" | Where-Object { $_.Length -gt 500 })
    if ($long.Count) { throw "$name\INSTALL.TXT: line longer than 500 characters: $($long[0].Substring(0, 40))..." }
    [IO.File]::WriteAllBytes((Join-Path $dst 'INSTALL.INI'), (ConvertTo-DosText $text))
    Copy-Item -LiteralPath $Installer -Destination (Join-Path $dst 'INSTALL.EXE')
    $product = if ($ini -match '(?m)^Product=(.*)$') { $Matches[1].Trim() } else { $name }
    $index += '{0,-9} {1}' -f $name, $product
}

# the combined setup: INSTALL.EXE in the root, [Installer] lists the
# packages in the order of pkgs\ORDER.TXT
# ("off|NAME": not selected at first)
$order = @((Read-Utf8 (Join-Path $pkgRoot 'ORDER.TXT')) -split "`r?`n" |
    Where-Object { $_ -notmatch '^\s*(;|$)' } | ForEach-Object { $_.Trim().ToUpperInvariant() } |
    Where-Object { $names -contains ($_ -replace '^OFF\|', '') } | ForEach-Object { $_ -replace '^OFF\|', 'off|' })
$listed = @($order | ForEach-Object { $_ -replace '^off\|', '' })
$order += @($names | Where-Object { $listed -notcontains $_ })
$inst = @()
for ($i = 0; $i -lt $order.Count; $i++) { $inst += "Inst$($i + 1)=$($order[$i])" }
$main = (Read-Utf8 (Join-Path $PSScriptRoot 'INSTALL.TXT')).TrimEnd() + "`n`n[Installer]`n" + ($inst -join "`n") + "`n`n" + $textDe
[IO.File]::WriteAllBytes((Join-Path $cd 'INSTALL.INI'), (ConvertTo-DosText $main))
Copy-Item -LiteralPath $Installer -Destination (Join-Path $cd 'INSTALL.EXE')

# LIESMICH.TXT: the packages of the CD
$readme = Read-Utf8 (Join-Path $PSScriptRoot 'LIESMICH.TXT')
$readme = $readme.Replace('@PAKETE@', ($index -join "`n"))
[IO.File]::WriteAllBytes((Join-Path $cd 'LIESMICH.TXT'), (ConvertTo-DosText $readme))

if (-not $NoIso) {
    $iso = Join-Path $Out 'WIN3UPD.ISO'
    [GeosOne.Win3Upd.Iso9660]::Write($cd, $iso, 'WIN3UPD_DE')
    Write-Host "Written: $iso"
} else {
    Write-Host "Written: $cd"
}
