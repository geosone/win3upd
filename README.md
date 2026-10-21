# Windows 3.1x Update (deutsch)

A German update CD for Windows 3.1 and Windows for Workgroups 3.11, built
with the [GeosOne Dual Installer](https://github.com/geosone/gdi), after the
English *Windows 3.1x Update Script*.

`mkwin3upd.ps1` downloads the updates listed in `downloads.txt` (Microsoft
downloads from the Internet Archive, German files from
[win31.de](http://www.win31.de/software.htm)), checks their SHA-256, takes
the files out of them and writes the CD: one directory per package with
`INSTALL.EXE` and `INSTALL.INI`, and a combined `INSTALL.EXE` in the root
that installs the packages chosen one after the other. The repository
contains no Microsoft files.

    pwsh ./mkwin3upd.ps1                      # out/CD and out/WIN3UPD.ISO
    pwsh ./mkwin3upd.ps1 -Package CTL3D -NoIso

Needs PowerShell 7 (or Windows PowerShell 5.1), 7-Zip and the `INSTALL.EXE`
of the GeosOne Dual Installer (`-Installer`, default `./INSTALL.EXE` or
`../gdi/build/INSTALL.EXE`). The ISO 9660 image is written by the script
itself (`tools/W3Build.cs`).

## Packages

`pkgs/<NAME>/INSTALL.TXT` is the package's `INSTALL.INI` without `[Files]`
(UTF-8; written in code page 850 with the German texts of
`pkgs/TEXT_DE.TXT`). `pkgs/<NAME>/FILES.TXT` names the files:

    ; [condition|]target                source
    %WINDIR%\SYSTEM\CTL3D.DLL           ctl3d:OBJ16/DLL/CTL3D.DLL
    1|%WINDIR%\WINFILE.EXE              wfwy2kde:WINFILE.Y2K

A source is `download-id:path` in the unpacked download, `!path` goes into
an archive inside it, `#TYPE` takes a resource of a Windows program
(`ww1140:PENTFIX.EXE#VXDRESOURCE`). Files compressed with COMPRESS.EXE
(SZDD, KWAJ) are expanded. `local:FILE` is a text of the package
(`pkgs/<NAME>/FILE`, UTF-8, written in code page 850), `-"text"` after the
source removes that text from it (OEMSETUP.INF without WINSOCK.DLL). The
same source for several targets is stored once. `pkgs/ORDER.TXT` gives the order of the combined
setup (dependencies first; `off|NAME` is not selected at first).

| Package | Content | Language |
|---|---|---|
| CTL3D | CTL3D.DLL, CTL3DV2.DLL, CTL3D32.DLL | neutral |
| DVA | DVA.386 from Video for Windows 1.1e | neutral |
| FAT32 | R. Loew's FAT32 patch of WIN386.EXE, explained in WIN386.TXT (not selected at first) | German text |
| NETWORK | WfW 3.11 only: TCP/IP-32 3.11b and the NT 3.51 client updates for the network setup (`C:\WINDOWS\WINSETUP`), installed network drivers replaced, NDIS.386 patched for fast CPUs (patcher9x, explained in NDIS.TXT) | English |
| WINSOCK | WINSOCK.DLL from TCP/IP-32 3.11b | English |
| VBRUN | Visual Basic runtimes 1.0 to 4.0 | English |
| VSHARE | VSHARE.386 3.11.0.402 | neutral |
| WIN87EM | WIN87EM.DLL fix for 80387 (WW0548) | neutral |
| WINFILE | File Manager Y2K update (WfW 3.11 or Windows 3.1) | German |
| WING | WinG 1.0 | neutral |

## Test

`test/vm.sh PACKAGE ...` (or `ALL` for the combined setup) installs into a
copy of a Windows 3.11 hard disk image in QEMU from MS-DOS, starts Windows,
removes the packages with `INSTALL /U` and starts Windows again
(`WIN311IMG`, results in `out/vm`). `SEED=dir` copies files into
`C:\WINDOWS` first, e.g. the network drivers of a WfW installation so that
NETWORK replaces and patches them.

Copyright (C) 2026 GeosOne. GNU General Public License version 3.
