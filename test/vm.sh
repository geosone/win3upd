#!/bin/bash
# Installs packages of out/CD in a Windows 3.1x hard disk image (QEMU, from
# MS-DOS), starts Windows, removes them again with INSTALL /U and starts
# Windows again.  Screens and the changed files go to out/vm.
#   test/vm.sh CTL3D WINFILE ...   each with its own INSTALL.EXE
#   test/vm.sh ALL                  the combined INSTALL.EXE of the CD
# Environment: WIN311IMG (hard disk image, FAT partition at sector 63,
# Windows in C:\WINDOWS; AUTOEXEC.BAT is replaced), KEYS (install keys per
# package, default: 5x Enter), UNKEYS, SEED (a directory copied into
# C:\WINDOWS first, e.g. network files to be updated), WINARGS (a program
# Windows starts, e.g. a Win32s test), WINTIME (seconds until Windows is
# ended cleanly by test/exitwin/EXITWIN.EXE, default 40), START, TIMEOUT.
# At the end the files, INI files and the registry (test/regdat.py) after
# the removal are compared with the ones before.
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
mcopy -o -i "$P" "$ROOT/test/exitwin/EXITWIN.EXE" ::/ || exit 1
[ -n "${SEED:-}" ] && { mcopy -s -o -i "$P" "$SEED"/* ::/WINDOWS/ || exit 1; }
ALL=
if [ "$1" = ALL ]; then
    ALL=1
    mcopy -s -o -i "$P" "$ROOT"/out/CD/* ::/CD/ || exit 1
    # the packages in the order of the combined setup (removed in reverse)
    set -- $(tr -d '\r' < "$ROOT/out/CD/INSTALL.INI" | sed -n 's/^Inst[0-9]*=\(off|\)\{0,1\}//p')
else
    for p in "$@"; do mcopy -s -o -i "$P" "$ROOT/out/CD/$p" ::/CD/ || exit 1; done
fi

run() {   # AUTOEXEC.BAT lines ($1, | separated), screens $2-N.png, keys $3,
          # $4: a screen $2-app.png after that many seconds
    echo "$1" | tr '|' '\n' | crlf > "$OUT/AUTOEXEC.BAT"
    mcopy -o -i "$P" "$OUT/AUTOEXEC.BAT" ::/
    {
        sleep ${START:-6}; n=0
        for k in $3; do n=$((n+1)); echo "screendump $OUT/$2-$n.ppm"; sleep 1; echo "sendkey $k"; sleep ${STEP:-2}; done
        [ -n "${4:-}" ] && { sleep "$4"; echo "screendump $OUT/$2-app.ppm"; }
        sleep ${TIMEOUT:-25}; echo "screendump $OUT/$2-end.ppm"; sleep 1; echo quit
    } | qemu-system-i386 -machine pc -accel kvm -m 32 -display none -vga std -monitor stdio \
        -drive if=ide,index=0,format=raw,file="$OUT/hd.img" -boot c -nic none > "$OUT/monitor.log" 2>&1
    for f in "$OUT"/$2-*.ppm; do [ -f "$f" ] && magick "$f" "${f%.ppm}.png" && rm -f "$f"; done
}
fetch() {
    d="$OUT/files/$1"; mkdir -p "$d"
    for f in WINDOWS/WIN.INI WINDOWS/SYSTEM.INI WINDOWS/PROGMAN.INI WINDOWS/REG.DAT GDI.INI; do mcopy -o -i "$P" "::/$f" "$d/" 2>/dev/null; done
    mcopy -o -s -i "$P" ::/WINDOWS/UPDATES "$d/" 2>/dev/null
    mdir -b -s -i "$P" ::/WINDOWS > "$d/dir.txt" 2>/dev/null
}
fetch 0-before
# Windows is ended cleanly: WfW 3.11 writes cached data and WIN.INI only then
WINTIME=${WINTIME:-40}
WIN="@ECHO OFF|PATH C:\;C:\WINDOWS|C:\WINDOWS\WIN C:\EXITWIN.EXE $WINTIME ${WINARGS:-}"
if [ -n "$ALL" ]; then
    run 'C:\CD\INSTALL.EXE' install "${KEYS:-ret ret ret ret ret ret ret}"
else
    for p in "$@"; do run "C:\\CD\\$p\\INSTALL.EXE" "install-$p" "${KEYS:-ret ret ret ret ret}"; done
fi
fetch 1-installed
run "$WIN" windows "" $((WINTIME - 5))
fetch 2-windows
# every registry plan must have been applied by its runner
pending=$(find "$OUT/files/2-windows" -iname REGISTRY.PLN)
[ -z "$pending" ] && echo "registry plans: applied" || echo "registry plans NOT applied: $pending"
# removed in reverse order: a package removes the registry keys and parent
# directories it created only when no later package uses them any more
rev=(); for p in "$@"; do rev=("$p" "${rev[@]}"); done
for p in "${rev[@]}"; do run "C:\\WINDOWS\\UPDATES\\$p\\INSTALL.EXE /U" "uninst-$p" "${UNKEYS:-left ret ret ret}"; done
fetch 3-removed
run "$WIN" windows2 "" $((WINTIME - 5))
fetch 4-windows

# what the removal left different from before
cd "$OUT/files"
for f in WIN.INI SYSTEM.INI PROGMAN.INI; do
    diff <(tr -d '\r' < 0-before/$f) <(tr -d '\r' < 4-windows/$f) > /dev/null && echo "$f: same" ||
        { echo "$f: DIFFERENT"; diff <(tr -d '\r' < 0-before/$f) <(tr -d '\r' < 4-windows/$f); }
done
if [ -f 0-before/REG.DAT ]; then
    diff <("$ROOT/test/regdat.py" 0-before/REG.DAT) <("$ROOT/test/regdat.py" 4-windows/REG.DAT) > /dev/null && echo "registry: same" ||
        { echo "registry: DIFFERENT"; diff <("$ROOT/test/regdat.py" 0-before/REG.DAT) <("$ROOT/test/regdat.py" 4-windows/REG.DAT); }
fi
files() { grep -v -i -E '\.G[0-9][0-9]$|\.SWP$|/BOOTLOG\.TXT$|/DOSAPP\.INI$' "$1" | sort; }
diff <(files 0-before/dir.txt) <(files 4-windows/dir.txt) > /dev/null && echo "files: same" ||
    { echo "files: DIFFERENT"; diff <(files 0-before/dir.txt) <(files 4-windows/dir.txt); }
