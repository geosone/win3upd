#!/bin/bash
# Installs packages of out/CD in a Windows 3.1x hard disk image (QEMU, from
# MS-DOS), starts Windows, removes them again with INSTALL /U and starts
# Windows again.  Screens and the changed files go to out/vm.
#   test/vm.sh CTL3D WINFILE ...   each with its own INSTALL.EXE
#   test/vm.sh ALL                  the combined INSTALL.EXE of the CD
# Environment: WIN311IMG (hard disk image, FAT partition at sector 63,
# Windows in C:\WINDOWS; AUTOEXEC.BAT is replaced), KEYS (install keys per
# package, default: 5x Enter), UNKEYS, SEED (a directory copied into
# C:\WINDOWS first, e.g. network files to be updated), START, TIMEOUT
set -uo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
OUT=$ROOT/out/vm
IMG=${WIN311IMG:-$HOME/mars/ahciaspi/build/win311/win311.img}
[ -f "$IMG" ] || { echo "WIN311IMG not found: $IMG"; exit 1; }
[ $# -gt 0 ] || { echo "usage: test/vm.sh PACKAGE ..."; exit 1; }
rm -rf "$OUT"; mkdir -p "$OUT/files"
cp "$IMG" "$OUT/hd.img"; P="$OUT/hd.img@@32256"
crlf() { sed 's/$/\r/'; }
mmd -i "$P" ::/CD 2>/dev/null
[ -n "${SEED:-}" ] && { mcopy -s -o -i "$P" "$SEED"/* ::/WINDOWS/ || exit 1; }
ALL=
if [ "$1" = ALL ]; then
    ALL=1
    mcopy -s -o -i "$P" "$ROOT"/out/CD/* ::/CD/ || exit 1
    set -- $(cd "$ROOT/out/CD" && ls -d */ | tr -d /)
else
    for p in "$@"; do mcopy -s -o -i "$P" "$ROOT/out/CD/$p" ::/CD/ || exit 1; done
fi

run() {   # AUTOEXEC.BAT lines ($1, | separated), screens $2-N.png, keys $3
    echo "$1" | tr '|' '\n' | crlf > "$OUT/AUTOEXEC.BAT"
    mcopy -o -i "$P" "$OUT/AUTOEXEC.BAT" ::/
    {
        sleep ${START:-6}; n=0
        for k in $3; do n=$((n+1)); echo "screendump $OUT/$2-$n.ppm"; sleep 1; echo "sendkey $k"; sleep ${STEP:-2}; done
        sleep ${TIMEOUT:-25}; echo "screendump $OUT/$2-end.ppm"; sleep 1; echo quit
    } | qemu-system-i386 -machine pc -accel kvm -m 32 -display none -vga std -monitor stdio \
        -drive if=ide,index=0,format=raw,file="$OUT/hd.img" -boot c -nic none > "$OUT/monitor.log" 2>&1
    for f in "$OUT"/$2-*.ppm; do [ -f "$f" ] && magick "$f" "${f%.ppm}.png" && rm -f "$f"; done
}
fetch() {
    d="$OUT/files/$1"; mkdir -p "$d"
    for f in WINDOWS/WIN.INI WINDOWS/SYSTEM.INI WINDOWS/PROGMAN.INI GDI.INI; do mcopy -o -i "$P" "::/$f" "$d/" 2>/dev/null; done
    mcopy -o -s -i "$P" ::/WINDOWS/UPDATES "$d/" 2>/dev/null
    mdir -b -s -i "$P" ::/WINDOWS > "$d/dir.txt" 2>/dev/null
}
fetch 0-before
WIN='@ECHO OFF|PATH C:\;C:\WINDOWS|C:\WINDOWS\WIN'
if [ -n "$ALL" ]; then
    run 'C:\CD\INSTALL.EXE' install "${KEYS:-ret ret ret ret ret ret ret}"
else
    for p in "$@"; do run "C:\\CD\\$p\\INSTALL.EXE" "install-$p" "${KEYS:-ret ret ret ret ret}"; done
fi
fetch 1-installed
run "$WIN" windows ""
fetch 2-windows
for p in "$@"; do run "C:\\WINDOWS\\UPDATES\\$p\\INSTALL.EXE /U" "uninst-$p" "${UNKEYS:-left ret ret ret}"; done
fetch 3-removed
run "$WIN" windows2 ""
fetch 4-windows
