# Was noch fehlt

Suchliste für die deutsche Update-CD. Gesucht werden **Original-Downloads
oder Original-Disketten/CDs von Microsoft in deutscher Sprache**, am besten
mit einer Adresse im Internet Archive (`web.archive.org/web/<Zeit>id_/…`),
damit `downloads.txt` sie mit SHA-256 laden kann. Programme anderer
Hersteller kommen nicht auf die CD.

Typische Adressen der deutschen Microsoft-Downloads (zum Suchen in der
[CDX-Suche des Archivs](https://web.archive.org/cdx/search/cdx?url=download.microsoft.com/download/win31/*&fl=timestamp,original,length&filter=statuscode:200&collapse=urlkey)):

- `download.microsoft.com/download/<produkt>/<art>/<version>/<os>/DE/<datei>`
- `msdl.microsoft.com/msdownload/<produkt>/…/de/…`
- Softlib-Nummern (`WW…`, `WG…`) gab es meist nur englisch; deutsche
  Fassungen standen manchmal auf der deutschen TechNet-CD.

## 1. Lücken in den fertigen Paketen

| Paket | Fehlt | Stand / Hinweis |
|---|---|---|
| NETWORK | **Dateien der deutschen WfW-3.11-Installationsdisketten** für `C:\WINDOWS\WINSETUP`: `LMSCRIPT.EX_`, `LMSCRIPT.PI_`, `LOGONOFF.EX_`, `NDDENB.DL_`, `NDISHLP.SY_`, `NETBEUI.38_`, `NETDDE.EX_`, `NETH.MS_`, `NETWATCH.EX_`, `NETWATCH.HL_`, `PMSPL.DL_`, `PROTMAN.DO_`, `PROTMAN.EX_`, `WFWAUDIT.DL_`, `WFWNETCP.CP_`, `WFWNET.DR_`, `WFWNET.HL_`, `WINCHAT.EX_`, `WINCHAT.HL_`, `WINMETER.EX_`, `WINPOPUP.EX_`, `WINPOPUP.HL_` | Kein Download bekannt. Ohne sie fragt die Netzwerkeinrichtung nach der WfW-Diskette. Ein frei ladbares deutsches Update, das diese Dateien enthält, wäre ideal. |
| NETWORK | **TCP/IP-32 3.11b deutsch** (`TCP32B.EXE`, DE) | Microsoft hatte nur `…/wfw311/update/3.11b/wfw/en-us/tcp32b.exe`. Vielleicht auf der deutschen TechNet-CD oder als `TCP32BDE.EXE`. |
| NETWORK | **WFWFILES deutsch** (Update-Dateien aus `\CLIENTS\WFW\UPDATE` der deutschen Windows-NT-Server-3.51-CD): `NET.EXE`, `NET.MSG`, `NDIS.386`, `VREDIR.386`, `VSERVER.386`, `VNETSUP.386`, `NWNBLINK.386`, `NETAPI.DLL` | Jetzt englisch (`wfwfiles.exe` von conradshome.com); NET.EXE/NET.MSG deshalb nur auf Wunsch. |
| NETWORK | Deutsche Fassungen von WG0970 (VNETBIOS.386), WG0971 (NDIS2SUP.386), WG0973 (RMM.D32), WG1001 (SERIAL.386), WG1027 (NWLINK.386) | Wahrscheinlich sprachneutral; zu prüfen, ob es DE-Fassungen gab. |
| WINSOCK | WINSOCK.DLL deutsch | Gehört zu TCP32B (siehe oben). |
| VBRUN | Deutsche Visual-Basic-Laufzeit (`VBRUN100.DLL`, `VBRUN200.DLL`, `VBRUN300.DLL`, `VB40016.DLL` deutsch) | Jetzt englisch. Deutsche VB-3.0/4.0-Laufzeit (z. B. von der VB-CD oder `VB3DE…`) gesucht. |

## 2. Microsoft-Pakete, die noch fehlen

| Paket | Inhalt | Englische Quelle der Vorlage | Deutsch gesucht |
|---|---|---|---|
| EURO | Euro-Zeichen für Windows 3.1 (WIN3EURO) | `msdl.microsoft.com/msdownload/euro/31/en/WIN3EURO.EXE` | **Bekannt:** `https://web.archive.org/web/20021101153836/http://msdl.microsoft.com:80/msdownload/euro/31/de/WIN3EURO.EXE`, nur noch einbauen |
| VFW | Video für Windows 1.1e Laufzeit | `WV1160.EXE` (jmcgowan.com, englisch) | Deutsche Video-für-Windows-Laufzeit 1.1 (z. B. `WV1160DE`, deutsche TechNet-CD) |
| OLE2 | OLE 2.03 | aus `PW1118.EXE` (Win32s 1.30c) | Deutsches OLE 2.0x |
| W32S_130 | Win32s 1.30c | `PW1118.EXE` (pcworld.com) | Win32s 1.30c deutsch (`PW1118` DE, `W32S130D…`) |
| W32S_125 | Win32s 1.25a | Disketten von WinWorld | Win32s 1.25a deutsch |
| IE5 | Internet Explorer 5 16 Bit | `msdl.microsoft.com/msdownload/ie5/rtw/win16/en/cab/*.cab` (BROWSER, IECORE, MSHTML16, PRESU) | Gleiche CABs mit `/de/`: im Archiv bisher nicht gefunden. Alternativ IE5 16 Bit von einer deutschen Zeitschriften- oder Windows-CD |
| REGEDIT | 16-Bit-Registrierungseditor aus IE5 (`REGEDT16.EXE`) | `iecore.cab` (en) | Kommt mit IE5 deutsch |
| IE3 | Internet Explorer 3.03 SP1 16 Bit | `dlful30f.exe` (Spiegel) | IE 3.03 16 Bit deutsch |
| 310TO311 | Windows-3.11-Update für 3.10 (WW0981) | `ww0981.exe` (conradshome.com) | Deutsche Fassung (`WW0981` DE) |
| CALC | Rechner-Update (WW1138) | `…/win95upg/update/6/w95/en-us/ww1138.exe` | Deutsche Fassung |
| FAX | At Work PC Fax: WG0992, WG1126 | conradshome.com | Deutsche Fassungen |
| EXTRAS3X | Fehlende Zubehörprogramme für WfW (WFWADD) | `wfwadd.exe` (conradshome.com) | Deutsch; sonst aus deutschem Windows 3.1 |
| FIDVFIX | Pentium-FDIV-Fix (WW1140, FP.386) | `ww1140.exe`, schon in `downloads.txt` | Zurückgestellt: Das Entfernen von `*vmcpd` lässt sich nicht sauber rückgängig machen |
| MSVCRT | Visual-C++-2.x/4.1-Laufzeit (32 Bit, Win32s) | VC++-CDs, `Vc41b.exe` | Sprachneutral, aber nur von CDs: frei ladbare Quelle gesucht |
| MMP | MCI-Treiber für Multimedia Movie Player | Windows 3.0 MME (Creative OEM) | Freie Quelle gesucht |
| FREECELL | FreeCell (Entertainment Pack 2) | Entertainment-Pack-CD | Deutsch, frei ladbar? |
| CTL3DME | CTL3D aus Windows ME | `WIN_10.CAB` der ME-CD | Nur von CD; eher weglassen |

**Aus Windows 95/NT** (CALC32, MSPAINT, WORDPAD, FONTVIEW, PINBALL): Die
Vorlage nimmt sie von den CDs. Ohne frei ladbaren deutschen Download kommen
sie nicht auf die CD.

## 3. Bewusst weggelassen (andere Hersteller oder geänderte Microsoft-Dateien)

ALL3D, CALMICFG, CALMIRA3, CALMIRA5, COMMDLG (gepatcht), DOSBAR, FONT95,
FTASKMAN, KFRAME, LIEVERSN, MAKEOVER, NOTEPAD (inoffiziell), OSFRAMER, QT
(Apple), RGDT_ALT, SHELL32 (gepatcht), THEMES95, USER94, USER98, VBX.
