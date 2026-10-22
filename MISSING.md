# Was noch fehlt

Suchliste für die deutsche Update-CD. Gesucht werden **Original-Downloads
oder Original-Disketten/CDs von Microsoft in deutscher Sprache**, am besten
mit einer Adresse im Internet Archive (`web.archive.org/web/<Zeit>id_/…`),
damit `downloads.txt` sie mit SHA-256 laden kann. Programme anderer
Hersteller kommen nur als unveränderte Freeware auf die CD (ALL3D, FTASKMAN,
KFRAME).

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
| NETWORK | Deutsche VNETBIOS.386 (WG0970) | Sechs englische Meldungen (z. B. „This application should not be terminated because it is currently communicating on the network“). Im deutschen WfW steckt VNETBIOS mit deutschen Texten in WIN386.EXE; die längeren deutschen Texte lassen sich nicht einfach in die englische Datei setzen. |
| MSVCRT | Deutsche MFC 3.0 (`MFC30.DLL`, `MFCO30.DLL`, `MFCD30.DLL`, `MFCUIA32.DLL`) | Englische Standardtexte der MFC (Dialoge „Insert Object“, „Save Copy As“, Druckmeldungen), sichtbar in Programmen, die keine eigenen Texte dafür haben. Deutsch nur von der deutschen Visual-C++-2.x-CD. |
| VBRUN | Deutsche Visual-Basic-Laufzeit (`VBRUN100.DLL`, `VBRUN200.DLL`, `VBRUN300.DLL`, `VB40016.DLL` deutsch) | Jetzt englisch. Kein deutscher Download gefunden (Suche Okt. 2026: nur DLL-Seiten und [archive.org/details/VBRUN300_EXE](https://archive.org/details/VBRUN300_EXE), englisch 3.0.5.38). Deutsche VB-3.0/4.0-Laufzeit nur von der deutschen VB-CD. |

## 2. Microsoft-Pakete, die noch fehlen

| Paket | Inhalt | Englische Quelle der Vorlage | Deutsch gesucht |
|---|---|---|---|
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
- IE3: nicht nötig, IE5 ersetzt ihn.
- CALC: der Rechner von WW1138 (nur englisch) mit den deutschen Ressourcen
  des WfW-3.11-Rechners (`res=`), getestet: 2,01 - 2 = 0,01.
- NETAPI.DLL (WFWFILES, NT 3.51): mit den deutschen Ressourcen der
  WfW-3.11-Disketten (`res=`), Version 3.11.0.305 bleibt.
- 310TO311 (WW0981): Paket aus dem deutschen Windows 3.11 (WinWorld), das
  alle acht Dateien von WW0981 in denselben Versionen deutsch hat.
- Ressourcen-Prüfung der übrigen englischen Fixes: EFAXPUMP.DLL und
  FAXCOVER.DLL (WG0992) haben die deutschen Texte im Datensegment, nicht in
  den Ressourcen (bleiben englisch); FAXOPT.DLL hat die deutschen
  Ressourcen mit den zwei Änderungen von WG0992 (Schaltfläche „Abbrechen“
  statt „Sicherheit deaktivieren“, zwei neue Einstellungen in Stringtabelle
  11, Papierformat A4; `set=`); AWFAXIO.DLL hat auch deutsch nur
  englische Ressourcen.
- FAX: Paket mit WG1126 (sprachneutral) und WG0992 (englisch; Deutsch gesucht).
- Softlib-Durchsicht (gaby.de, alle WW/WG, Okt. 2026): dazu kamen WG0988,
  WG0990, WG0991 (Netzwerkkartentreiber, Paket NETWORK) und WG0992/WG1126
  (FAX). Nicht übernommen: Texte ohne Dateien, Windows-3.0-Dateien
  (WW0314, WW0340, WW0407, WW0440), ältere Drucker-/Plottertreiber als auf
  den deutschen Disketten (WW0505, WW0511, WW0512, WW0553), WG0904/WG1049/
  WG1129 (durch WFWFILES überholt), WW1116 (älter als OLE2 aus PW1118),
  WG1002 (älter als die deutsche SCHEDMSG.DLL), Banyan VINES (WG0768/72/73),
  Sonderhardware (WG0670 Intel SoftSet, WG0974 Compaq VKDA, WW0764
  Windows Sound System, WW0634 LAN Manager 2.1, WW0863 NetWare-VIPX).
- Übrige Softlib (Inhaltsverzeichnis MSLINDEX.XLS, Okt. 2026): dazu kam
  WFWVSRVR (VSERVER.386 3.11.0.310, sprachneutral, Paket NETWORK). Schon auf
  der CD oder deutsch vorhanden: VBRUN100/200/300, CTL3D, W31FILUP/WFWFILUP
  (deutsche Y2K-Fassungen), WW1000, DISPDIB (neuer über VFW). Nicht
  übernommen: WFWPTP (RAS-Einwählserver für WfW 3.11 mit RAS 3.11.0.301; eine
  neue Funktion, die das deutsche RAS englisch machen würde; RASPHONE,
  RASCONF, RASMON und RASSTART haben deutschen Code, keine
  Ressourcen-Übertragung möglich), WFWDLC (DLC-Protokoll für Großrechner),
  MSWGCN (Netzwerk-Client für DOS), GA0363 (MSD, englisch; WfW hat das
  deutsche), ENC97FIX (nur für Encarta 97), Gerätetreiber für Windows NT,
  Programmierbeispiele, Dokumentation und Updates für Anwendungen.
- OS2FIX (R. Buschmann, `osr2fix.exe`): nicht nötig. Sein Teil WINSTART
  ändert die IO.SYS von DOS 7.1, damit Windows 3.x startet; das DOS 7.1 ist
  dafür schon gepatcht. Den FAT32-Fix für WIN386.EXE bringt das Paket FAT32.
- Sprachprüfung der englischen und „neutralen“ Pakete (Okt. 2026, Texte aus
  den Ressourcen: Stringtabellen, Menüs, Dialoge, Versionssprache):
  ohne sichtbare Texte sind CTL3D, CTL3DME, DVA, VSHARE, WIN87EM, WING,
  WINSOCK.DLL, MSVCRT20.DLL und die OLE-2-DLLs. OLE2 installiert OLECLI.DLL 1.32
  jetzt deutsch von den WfW-3.11-Disketten statt englisch aus PW1118
  (deutsches Windows 3.1 von WinWorld: OLECLI 1.10). Englisch bleiben wenige NET.EXE-Texte
  (WFWFILES), TCP32UI.DLL, TELNET.EXE, PING.EXE usw. (TCP/IP-32 3.11b gab
  es nur englisch), VNETBIOS.386, MFC 3.0 und VBRUN (oben).
- W32S_125 (Win32s 1.25a): nicht nötig. WinWorld hat 1.25/1.25a nur
  englisch, und die deutschen Win32s-Dateien (`deu.zip`) sind 1.30.172; das
  Paket WIN32S installiert damit das neuere Win32s 1.30c deutsch.
- IE5: deutsche Setup-Dateien des IE 5.0 (16 Bit) von gaby.de; der deutsche
  Satz ist ohne PRESU.CAB vollständig (laut gaby.de und FILELIST.DAT).
- REGEDIT: REGEDT16 aus der deutschen IECORE.CAB von gaby.de (die Kopien im
  Internet Archive sind englisch).
- EURO: deutsches WIN3EURO eingebaut (Paket EURO).
- NETWORK: die 22 Dateien der WfW-Disketten kommen aus der deutschen WfW 3.11
  (OEM) von WinWorld (Disketten 8 und 9).

## 4. Bewusst weggelassen

Von den Paketen der englischen Vorlage übernommen: ALL3D, FTASKMAN (deutsche
Fassung des Autors), KFRAME, FONT95 (aus den Microsoft-Schriften des
deutschen Windows 3.11 neu gebaut), NOTEPAD (selbst übersetzt mit
NOTEPAD.DE; MFCOLEUI.DLL bleibt englisch; öffnet Dateien bis knapp 64 KB,
nicht beliebig große), USER98 als USERME (die deutsche USER.EXE aus MINI.CAB
der deutschen ME-CD). Weggelassen: CALMICFG, CALMIRA3, CALMIRA5, COMMDLG
(gepatcht), DOSBAR, LIEVERSN, MAKEOVER, OSFRAMER, QT (Apple), RGDT_ALT,
SHELL32 (gepatcht), THEMES95, USER94, VBX.
