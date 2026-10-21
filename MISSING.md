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
| NETWORK | **WFWFILES deutsch** (Update-Dateien aus `\CLIENTS\WFW\UPDATE` der deutschen Windows-NT-Server-3.51-CD): `NET.EXE`, `NET.MSG`, `NDIS.386`, `VREDIR.386`, `VSERVER.386`, `VNETSUP.386`, `NWNBLINK.386`, `NETAPI.DLL` | Jetzt englisch (`wfwfiles.exe` von conradshome.com). NET.MSG bekommt beim Bauen die deutschen Texte der WfW-Disketten (nur eine neue Meldung und wenige Texte in NET.EXE bleiben englisch). |
| NETWORK | Deutsche Fassungen von WG0970 (VNETBIOS.386), WG0971 (NDIS2SUP.386), WG0973 (RMM.D32), WG1001 (SERIAL.386), WG1027 (NWLINK.386) | Wahrscheinlich sprachneutral; zu prüfen, ob es DE-Fassungen gab. |
| VBRUN | Deutsche Visual-Basic-Laufzeit (`VBRUN100.DLL`, `VBRUN200.DLL`, `VBRUN300.DLL`, `VB40016.DLL` deutsch) | Jetzt englisch. Kein deutscher Download gefunden (Suche Okt. 2026: nur DLL-Seiten und [archive.org/details/VBRUN300_EXE](https://archive.org/details/VBRUN300_EXE), englisch 3.0.5.38). Deutsche VB-3.0/4.0-Laufzeit nur von der deutschen VB-CD. |

## 2. Microsoft-Pakete, die noch fehlen

| Paket | Inhalt | Englische Quelle der Vorlage | Deutsch gesucht |
|---|---|---|---|
| W32S_125 | Win32s 1.25a | Disketten von WinWorld | Win32s 1.25a deutsch |
| IE3 | Internet Explorer 3.03 SP1 16 Bit | `dlful30f.exe` (Spiegel) | IE 3.03 16 Bit deutsch |
| 310TO311 | Windows-3.11-Update für 3.10 (WW0981) | `ww0981.exe` (conradshome.com) | Deutsche Fassung (`WW0981` DE) |
| CALC | Rechner-Update (WW1138) | `…/win95upg/update/6/w95/en-us/ww1138.exe` | Deutsche Fassung |
| FAX | At Work PC Fax: WG0992, WG1126 | conradshome.com | Deutsche Fassungen |
| EXTRAS3X | Fehlende Zubehörprogramme für WfW (WFWADD) | `wfwadd.exe` (conradshome.com) | Deutsch; sonst aus deutschem Windows 3.1 |
| FIDVFIX | Pentium-FDIV-Fix (WW1140, FP.386) | `ww1140.exe`, schon in `downloads.txt` | Zurückgestellt: Das Entfernen von `*vmcpd` lässt sich nicht sauber rückgängig machen |
| MMP | MCI-Treiber für Multimedia Movie Player | Windows 3.0 MME (Creative OEM) | Freie Quelle gesucht |


## 3. Erledigt oder nicht nötig

- TCP/IP-32 3.11b (TCP32B) und damit WINSOCK.DLL gab es nur englisch.
- OLE2 (16 Bit) gibt es nur im englischen Win32s-Paket (PW1118); WIN32S
  nimmt dazu die deutschen Win32s-Dateien von gaby.de (`deu.zip`).
- CALC32, WORDPAD, MSPAINT, FONTVIEW und FREECELL kommen von der
  deutschen Windows-95-CD (OSR 2.5, WinWorld), CTL3DME von der deutschen
  Windows-ME-CD (WinWorld).
- VFW: deutsche Video-für-Windows-1.1-Laufzeit von gaby.de (`vfw11.zip`);
  ihre OLE2.REG liefert die deutschen Texte für das Paket OLE2.
- MSVCRT: die Win32s-Fassungen aus Visual C++ 2.2 (WinWorld, \WIN32S\REDIST);
  die MSVCRT20.DLL 2.11 aus Windows 95/NT läuft unter Win32s nicht.
- PINBALL: deutsche Windows-NT-4.0-CD (WinWorld).
- IE5: deutsche Setup-Dateien des IE 5.0 (16 Bit) von gaby.de; der deutsche
  Satz ist ohne PRESU.CAB vollständig (laut gaby.de und FILELIST.DAT).
- REGEDIT: REGEDT16 aus der deutschen IECORE.CAB von gaby.de (die Kopien im
  Internet Archive sind englisch).
- EURO: deutsches WIN3EURO eingebaut (Paket EURO).
- NETWORK: die 22 Dateien der WfW-Disketten kommen aus der deutschen WfW 3.11
  (OEM) von WinWorld (Disketten 8 und 9).

## 4. Bewusst weggelassen (andere Hersteller oder geänderte Microsoft-Dateien)

ALL3D, CALMICFG, CALMIRA3, CALMIRA5, COMMDLG (gepatcht), DOSBAR, FONT95,
FTASKMAN, KFRAME, LIEVERSN, MAKEOVER, NOTEPAD (inoffiziell), OSFRAMER, QT
(Apple), RGDT_ALT, SHELL32 (gepatcht), THEMES95, USER94, USER98, VBX.
