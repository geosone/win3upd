// W3Build.cs - helpers of mkwin3upd.ps1: resources of Windows 3.x programs
// (NE), SHA-256 and an ISO 9660 image writer (level 1, 8.3 names, as DOS
// and MSCDEX read them).
// Copyright (C) 2026 GeosOne.  GNU General Public License version 3.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GeosOne.Win3Upd
{
	public static class Hash
	{
		public static string Sha256(string path)
		{
			using (var s = File.OpenRead(path))
			using (var h = SHA256.Create())
				return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "").ToLowerInvariant();
		}
	}

	// resources of a 16-bit Windows program (NE): FP.386 is the resource
	// VXDRESOURCE of PENTFIX.EXE, for example
	public static class NeResource
	{
		static int W(byte[] d, int at) { return d[at] | d[at + 1] << 8; }

		// the first resource of the type (a name, or "#n" for type n)
		public static byte[] Extract(byte[] d, string type)
		{
			if (d.Length < 64 || d[0] != 'M' || d[1] != 'Z') throw new InvalidDataException("not a program");
			int ne = W(d, 0x3C) | W(d, 0x3E) << 16;
			if (ne + 64 > d.Length || d[ne] != 'N' || d[ne + 1] != 'E') throw new InvalidDataException("not a Windows 3.x program");
			int table = ne + W(d, ne + 0x24);
			int shift = W(d, table);
			for (int at = table + 2; W(d, at) != 0; ) {
				int id = W(d, at), count = W(d, at + 2);
				string name;
				if ((id & 0x8000) != 0)
					name = "#" + (id & 0x7FFF);
				else
					name = Encoding.ASCII.GetString(d, table + id + 1, d[table + id]);
				if (string.Equals(name, type, StringComparison.OrdinalIgnoreCase) && count > 0) {
					int offset = W(d, at + 8) << shift, length = W(d, at + 10) << shift;
					var r = new byte[length];
					Array.Copy(d, offset, r, 0, length);
					return r;
				}
				at += 8 + 12 * count;
			}
			throw new InvalidDataException("no resource " + type);
		}

		// the translatable resources (menus, dialogs, string tables,
		// accelerators, version) of code taken from lang, the same program in
		// another language (a fix made only in English, with the texts of the
		// localized version).  One that fits stays in place, a larger one is
		// appended to the file.  set: texts the fix changed or added, in the
		// localized resource: "5:NAME#id=text" (the control id of a dialog),
		// "6:block#n=text" (the n-th string of a string table block, from 0),
		// separated by ";"
		public static byte[] Merge(byte[] code, byte[] lang, string set = null)
		{
			var r = new List<byte>(code);
			int shift, langShift;
			var c = Table(code, out shift);
			var l = Table(lang, out langShift);
			var sets = new List<string[]>();
			if (!string.IsNullOrEmpty(set))
				foreach (var s in set.Split(';')) {
					int h = s.IndexOf('#'), q = s.IndexOf('=');
					if (h < 0 || q < h) throw new InvalidDataException("invalid set: " + s);
					sets.Add(new[] { s.Substring(0, h).ToUpperInvariant(), s.Substring(h + 1, q - h - 1), s.Substring(q + 1) });
				}
			foreach (var e in c) {
				int type = int.Parse(e.Key.Split(':')[0]);
				if (type != 4 && type != 5 && type != 6 && type != 9 && type != 16) continue;
				int[] le;
				if (!l.TryGetValue(e.Key, out le)) throw new InvalidDataException("resource " + e.Key + " missing in the localized program");
				int offset = e.Value[1], length = e.Value[2];
				var data = new byte[le[2]];
				Array.Copy(lang, le[1], data, 0, data.Length);
				if (type == 16) {
					// the version texts of lang, the version numbers of code
					// (VS_FIXEDFILEINFO: 52 bytes from its signature FEEF04BD)
					int fc = Find(code, offset, length, 0xFEEF04BD), fl = Find(data, 0, data.Length, 0xFEEF04BD);
					if (fc >= 0 && fl >= 0) Array.Copy(code, fc, data, fl, 52);
				}
				foreach (var s in sets)
					if (s[0] == e.Key.ToUpperInvariant()) {
						data = type == 5 ? DialogText(data, int.Parse(s[1]), s[2])
							: type == 6 ? TableString(data, int.Parse(s[1]), s[2])
							: null;
						if (data == null) throw new InvalidDataException("set only works for dialogs and string tables: " + s[0]);
						s[0] = null;   // done
					}
				Put(r, e.Value, data, shift);
			}
			foreach (var s in sets)
				if (s[0] != null) throw new InvalidDataException("set: no resource " + s[0]);
			return r.ToArray();
		}

		// a resource (table entry, offset, length) with new data: in place if it
		// fits, else appended to the file
		static void Put(List<byte> r, int[] res, byte[] data, int shift)
		{
			int unit = 1 << shift, entry = res[0], offset = res[1], length = res[2];
			int size = (data.Length + unit - 1) / unit * unit;
			if (size > length) {
				while (r.Count % unit != 0) r.Add(0);
				offset = r.Count;
				r.AddRange(new byte[size]);
				r[entry] = (byte)(offset >> shift); r[entry + 1] = (byte)(offset >> shift >> 8);
				// no longer in the fast-load area: not PRELOAD (else the loader
				// of Windows faults)
				r[entry + 4] &= unchecked((byte)~0x40);
			}
			for (int i = 0; i < Math.Max(size, length); i++)
				r[offset + i] = i < data.Length ? data[i] : (byte)0;
			r[entry + 2] = (byte)(size >> shift); r[entry + 3] = (byte)(size >> shift >> 8);
		}

		// the font of a font file (VGASYS.FON) replaced by the first font of
		// small (the 8-point MS Sans Serif of SSERIFE.FON), set in the height
		// and on the base line of the old one: the system font of Windows 95
		// (FONT95)
		public static byte[] SystemFont(byte[] fon, byte[] small)
		{
			var r = new List<byte>(fon);
			int shift, smallShift;
			var t = Table(fon, out shift);
			int[] font = null, dir = null, sf = null;
			foreach (var e in t) {
				if (e.Key.StartsWith("8:") && font == null) font = e.Value;
				if (e.Key.StartsWith("7:")) dir = e.Value;
			}
			foreach (var e in Table(small, out smallShift))
				if (e.Key.StartsWith("8:")) { sf = e.Value; break; }
			if (font == null || dir == null || sf == null) throw new InvalidDataException("no font");
			int s = sf[1], height = W(fon, font[1] + 0x58), top = W(fon, font[1] + 0x4A) - W(small, s + 0x4A);
			int h0 = W(small, s + 0x58), n = small[s + 0x60] - small[s + 0x5F] + 2, cols = (W(small, s + 0x5D) + 7) / 8;
			if (top < 0 || top + h0 > height) throw new InvalidDataException("font does not fit");
			// header and character table, then each character in cols columns
			// of height bytes
			var o = new List<byte>();
			for (int i = 0; i < 0x76 + 4 * n; i++) o.Add(small[s + i]);
			int bits = o.Count;
			for (int i = 0; i < n; i++) {
				int w = W(small, s + 0x76 + 4 * i), at = s + W(small, s + 0x78 + 4 * i);
				int start = o.Count;
				o[0x78 + 4 * i] = (byte)start; o[0x79 + 4 * i] = (byte)(start >> 8);
				for (int c = 0; c < cols; c++)
					for (int y = 0; y < height; y++)
						o.Add(c < (w + 7) / 8 && y >= top && y < top + h0 ? small[at + c * h0 + y - top] : (byte)0);
			}
			int face = s + BitConverter.ToInt32(small, s + 0x69);
			Set32(o, 0x69, o.Count);
			for (int i = face; small[i] != 0; i++) o.Add(small[i]);
			o.Add(0);
			Set32(o, 2, o.Count);
			int ascent = W(small, s + 0x4A) + top;
			o[0x4A] = (byte)ascent; o[0x4B] = (byte)(ascent >> 8);
			o[0x58] = (byte)height; o[0x59] = (byte)(height >> 8);
			var data = o.ToArray();
			// FONTDIR: one font, its number, the header, device and face name
			var d = new List<byte> { 1, 0, fon[dir[1] + 2], fon[dir[1] + 3] };
			for (int i = 0; i < 0x71; i++) d.Add(data[i]);
			d.Add(0);
			for (int i = face; small[i] != 0; i++) d.Add(small[i]);
			d.Add(0);
			Put(r, font, data, shift);
			Put(r, dir, d.ToArray(), shift);
			return r.ToArray();
		}

		// the texts of the menus, dialogs and string tables translated with
		// table: lines "English<TAB>German" (\n, \t, \b, \\ as escapes, ";"
		// comments); texts not in the table stay
		public static byte[] Translate(byte[] ne, string table)
		{
			var tr = new Dictionary<string, string>();
			foreach (var line in table.Split('\n')) {
				var l = line.TrimEnd('\r');
				if (l.Length == 0 || l[0] == ';') continue;
				int tab = l.IndexOf('\t');
				if (tab < 0) throw new InvalidDataException("translation without a tab: " + l);
				tr[Unescape(l.Substring(0, tab))] = Unescape(l.Substring(tab + 1));
			}
			Func<string, string> map = t => { string g; return tr.TryGetValue(t, out g) ? g : t; };
			var r = new List<byte>(ne);
			int shift;
			foreach (var e in Table(ne, out shift)) {
				int type = int.Parse(e.Key.Split(':')[0]);
				if (type != 4 && type != 5 && type != 6) continue;
				var d = new byte[e.Value[2]];
				Array.Copy(ne, e.Value[1], d, 0, d.Length);
				var o = new List<byte>();
				if (type == 4) {
					o.AddRange(new[] { d[0], d[1], d[2], d[3] });
					int at = 4;
					MenuLevel(d, ref at, o, map);
				} else if (type == 5) {
					int n = d[4], at = 13;
					for (int i = 0; i < 2; i++)   // menu, class
						at = d[at] == 0xFF ? at + 3 : Zero(d, at) + 1;
					Copy(d, 0, at, o);
					at = Text(d, at, o, map);   // caption
					if ((BitConverter.ToUInt32(d, 0) & 0x40) != 0) {   // DS_SETFONT
						int f = Zero(d, at + 2) + 1;
						Copy(d, at, f, o);
						at = f;
					}
					for (int i = 0; i < n; i++) {
						int c = at + 14;
						c = (d[c] & 0x80) != 0 ? c + 1 : Zero(d, c) + 1;   // class
						Copy(d, at, c, o);
						if (d[c] == 0xFF) { Copy(d, c, c + 3, o); at = c + 3; }
						else at = Text(d, c, o, map);
						Copy(d, at, at + 1 + d[at], o);   // extra bytes
						at += 1 + d[at];
					}
				} else {
					int at = 0;
					for (int i = 0; i < 16; i++) {
						int len = at < d.Length ? d[at] : 0;
						string t = Ansi.GetString(d, Math.Min(at + 1, d.Length), len);
						at += 1 + len;
						var b = Ansi.GetBytes(t.Length > 0 ? map(t) : t);
						if (b.Length > 255) throw new InvalidDataException("string longer than 255 characters");
						o.Add((byte)b.Length);
						o.AddRange(b);
					}
				}
				Put(r, e.Value, o.ToArray(), shift);
			}
			return r.ToArray();
		}

		static string Unescape(string s)
		{
			return s.Replace("\\\\", "\u0001").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\b", "\b").Replace("\u0001", "\\");
		}

		static void Copy(byte[] d, int from, int to, List<byte> o)
		{
			for (int i = from; i < to; i++) o.Add(d[i]);
		}

		// a zero-terminated text at at, translated into o; returns the end
		static int Text(byte[] d, int at, List<byte> o, Func<string, string> map)
		{
			int end = Zero(d, at);
			string t = Ansi.GetString(d, at, end - at);
			o.AddRange(Ansi.GetBytes(t.Length > 0 ? map(t) : t));
			o.Add(0);
			return end + 1;
		}

		// the items of a 16-bit menu up to the one with MF_END, pop-ups with
		// their items
		static void MenuLevel(byte[] d, ref int at, List<byte> o, Func<string, string> map)
		{
			for (;;) {
				int flags = W(d, at);
				Copy(d, at, at + ((flags & 0x10) != 0 ? 2 : 4), o);
				at += (flags & 0x10) != 0 ? 2 : 4;
				at = Text(d, at, o, map);
				if ((flags & 0x10) != 0) MenuLevel(d, ref at, o, map);
				if ((flags & 0x80) != 0) return;
			}
		}

		static void Set32(List<byte> o, int at, int v)
		{
			for (int i = 0; i < 4; i++) o[at + i] = (byte)(v >> 8 * i);
		}

		static readonly Encoding Ansi = Encoding.GetEncoding(1252);

		static int Zero(byte[] d, int at) { while (d[at] != 0) at++; return at; }

		// a 16-bit DIALOG with the text of the control id replaced
		static byte[] DialogText(byte[] d, int id, string text)
		{
			int n = d[4], at = 13;
			for (int i = 0; i < 2; i++)   // menu, class
				at = d[at] == 0xFF ? at + 3 : Zero(d, at) + 1;
			at = Zero(d, at) + 1;   // caption
			if ((BitConverter.ToUInt32(d, 0) & 0x40) != 0) at = Zero(d, at + 2) + 1;   // DS_SETFONT
			for (int i = 0; i < n; i++) {
				int cid = W(d, at + 8);
				at += 14;
				at = (d[at] & 0x80) != 0 ? at + 1 : Zero(d, at) + 1;   // class
				int textAt = at, textEnd = d[at] == 0xFF ? at + 3 : Zero(d, at) + 1;
				if (cid == id) {
					var t = Ansi.GetBytes(text);
					var o = new byte[d.Length - (textEnd - textAt) + t.Length + 1];
					Array.Copy(d, o, textAt);
					Array.Copy(t, 0, o, textAt, t.Length);
					Array.Copy(d, textEnd, o, textAt + t.Length + 1, d.Length - textEnd);
					return o;
				}
				at = textEnd + 1 + d[textEnd];   // extra bytes
			}
			throw new InvalidDataException("dialog has no control " + id);
		}

		// a string table block (16 strings with a length byte) with string n
		// replaced
		static byte[] TableString(byte[] d, int n, string text)
		{
			var o = new List<byte>();
			int at = 0;
			for (int i = 0; i < 16; i++) {
				int len = at < d.Length ? d[at] : 0;
				byte[] t = new byte[len];
				if (len > 0) Array.Copy(d, at + 1, t, 0, len);
				at += 1 + len;
				if (i == n) t = Ansi.GetBytes(text);
				if (t.Length > 255) throw new InvalidDataException("string longer than 255 characters");
				o.Add((byte)t.Length);
				o.AddRange(t);
			}
			return o.ToArray();
		}

		static int Find(byte[] d, int from, int length, uint value)
		{
			for (int i = from; i + 4 <= from + length && i + 4 <= d.Length; i++)
				if (BitConverter.ToUInt32(d, i) == value) return i;
			return -1;
		}

		// resources of the numbered types: "type:id" (or "type:NAME") -> table
		// entry, offset, length
		static Dictionary<string, int[]> Table(byte[] d, out int shift)
		{
			if (d.Length < 64 || d[0] != 'M' || d[1] != 'Z') throw new InvalidDataException("not a program");
			int ne = W(d, 0x3C) | W(d, 0x3E) << 16;
			if (ne + 64 > d.Length || d[ne] != 'N' || d[ne + 1] != 'E') throw new InvalidDataException("not a Windows 3.x program");
			int table = ne + W(d, ne + 0x24);
			shift = W(d, table);
			var r = new Dictionary<string, int[]>();
			for (int at = table + 2; W(d, at) != 0; ) {
				int type = W(d, at), count = W(d, at + 2);
				for (int i = 0; i < count; i++) {
					int e = at + 8 + 12 * i, id = W(d, e + 6);
					string name = (id & 0x8000) != 0 ? (id & 0x7FFF).ToString()
						: Encoding.ASCII.GetString(d, table + id + 1, d[table + id]).ToUpperInvariant();
					if ((type & 0x8000) != 0)
						r[(type & 0x7FFF) + ":" + name] = new[] { e, W(d, e) << shift, W(d, e + 2) << shift };
				}
				at += 8 + 12 * count;
			}
			return r;
		}
	}

	// NET.MSG of the LAN Manager / WfW client: the number of messages at 0Bh,
	// a table of 32-bit offsets at 1Fh, then the texts ("?" = no text)
	public static class NetMsg
	{
		static List<byte[]> Read(byte[] d)
		{
			int n = d[0x0B] | d[0x0C] << 8;
			var r = new List<byte[]>();
			for (int i = 0; i < n; i++) {
				int from = BitConverter.ToInt32(d, 0x1F + 4 * i);
				int to = i + 1 < n ? BitConverter.ToInt32(d, 0x1F + 4 * (i + 1)) : d.Length;
				if (from < 0x1F + 4 * n || to < from || to > d.Length) throw new InvalidDataException("not a NET.MSG file");
				var t = new byte[to - from];
				Array.Copy(d, from, t, 0, t.Length);
				r.Add(t);
			}
			return r;
		}

		static bool Empty(byte[] t) { return t.Length == 0 || t[0] == (byte)'?'; }

		// the messages of newer with the texts of lang where it has one
		// (a newer NET.EXE with the messages of the localized version)
		public static byte[] Merge(byte[] newer, byte[] lang)
		{
			var n = Read(newer);
			var l = Read(lang);
			var o = new MemoryStream();
			o.Write(newer, 0, 0x1F);
			int at = 0x1F + 4 * n.Count;
			var texts = new List<byte[]>();
			for (int i = 0; i < n.Count; i++) {
				var t = i < l.Count && !Empty(l[i]) ? l[i] : n[i];
				texts.Add(t);
				o.Write(BitConverter.GetBytes(at), 0, 4);
				at += t.Length;
			}
			foreach (var t in texts) o.Write(t, 0, t.Length);
			return o.ToArray();
		}
	}

	// ISO 9660 level 1: the tree of a folder (names must be 8.3)
	public static class Iso9660
	{
		const int Sector = 2048;

		class Node
		{
			public string Name, Path;
			public bool Dir;
			public List<Node> Children = new List<Node>();
			public Node Parent;
			public int Number;           // directory number (path table)
			public long Lba, Size;
			public DateTime Time;
		}

		static string IsoName(Node n)
		{
			return n.Dir ? n.Name : (n.Name.Contains('.') ? n.Name : n.Name + ".") + ";1";
		}

		static void Both16(byte[] b, int at, int v)
		{
			b[at] = (byte)v; b[at + 1] = (byte)(v >> 8);
			b[at + 2] = (byte)(v >> 8); b[at + 3] = (byte)v;
		}

		static void Both32(byte[] b, int at, long v)
		{
			for (int i = 0; i < 4; i++) {
				b[at + i] = (byte)(v >> (8 * i));
				b[at + 7 - i] = (byte)(v >> (8 * i));
			}
		}

		static void Text(byte[] b, int at, int len, string s)
		{
			for (int i = 0; i < len; i++)
				b[at + i] = (byte)(i < s.Length ? s[i] : ' ');
		}

		static byte[] Record(Node n, string name, DateTime t)
		{
			var nb = name == "\0" || name == "\x01" ? new[] { (byte)name[0] } : Encoding.ASCII.GetBytes(name);
			int len = 33 + nb.Length + (nb.Length % 2 == 0 ? 1 : 0);
			var r = new byte[len];
			r[0] = (byte)len;
			Both32(r, 2, n.Lba);
			Both32(r, 10, n.Size);
			r[18] = (byte)(t.Year - 1900); r[19] = (byte)t.Month; r[20] = (byte)t.Day;
			r[21] = (byte)t.Hour; r[22] = (byte)t.Minute; r[23] = (byte)t.Second;
			r[25] = (byte)(n.Dir ? 2 : 0);
			Both16(r, 28, 1);
			r[32] = (byte)nb.Length;
			nb.CopyTo(r, 33);
			return r;
		}

		static void Check(string name, bool dir)
		{
			string b = dir ? name : Path.GetFileNameWithoutExtension(name), e = dir ? "" : Path.GetExtension(name).TrimStart('.');
			if (b.Length < 1 || b.Length > 8 || e.Length > 3 || (dir && name.Contains('.')) ||
				name.Any(c => !(c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '.')))
				throw new InvalidDataException("not an ISO 9660 level 1 name: " + name);
		}

		static Node Scan(string path, string name, Node parent)
		{
			var n = new Node { Name = name, Path = path, Dir = true, Parent = parent, Time = Directory.GetLastWriteTime(path) };
			foreach (var d in Directory.GetDirectories(path)) {
				string nm = Path.GetFileName(d).ToUpperInvariant();
				Check(nm, true);
				n.Children.Add(Scan(d, nm, n));
			}
			foreach (var f in Directory.GetFiles(path)) {
				string nm = Path.GetFileName(f).ToUpperInvariant();
				Check(nm, false);
				n.Children.Add(new Node { Name = nm, Path = f, Parent = n, Size = new FileInfo(f).Length, Time = File.GetLastWriteTime(f) });
			}
			n.Children.Sort((a, b) => string.CompareOrdinal(IsoName(a), IsoName(b)));
			return n;
		}

		public static void Write(string folder, string iso, string volume)
		{
			var root = Scan(folder, "", null);
			// directories in path table order: level by level, children by name
			var dirs = new List<Node> { root };
			for (int i = 0; i < dirs.Count; i++)
				dirs.AddRange(dirs[i].Children.Where(c => c.Dir));
			if (dirs.Count > 65535) throw new InvalidDataException("too many directories");
			for (int i = 0; i < dirs.Count; i++) dirs[i].Number = i + 1;
			// path table
			var pt = new MemoryStream();
			foreach (var d in dirs) {
				int nl = d == root ? 1 : d.Name.Length;
				pt.WriteByte((byte)nl);
				pt.WriteByte(0);
				pt.Write(new byte[4], 0, 4);
				pt.Write(new byte[2], 0, 2);
				pt.Write(d == root ? new byte[] { 0 } : Encoding.ASCII.GetBytes(d.Name), 0, nl);
				if (nl % 2 == 1) pt.WriteByte(0);
			}
			int ptSize = (int)pt.Length, ptSectors = (ptSize + Sector - 1) / Sector;
			long lba = 18 + 2 * ptSectors;
			// directory sizes (a record never crosses a sector)
			foreach (var d in dirs) {
				int size = 0;
				foreach (int len in new[] { 34, 34 }.Concat(d.Children.Select(c => Record(c, IsoName(c), c.Time).Length))) {
					if (size % Sector + len > Sector) size += Sector - size % Sector;
					size += len;
				}
				d.Size = (size + Sector - 1) / Sector * Sector;
				d.Lba = lba;
				lba += d.Size / Sector;
			}
			var files = new List<Node>();
			foreach (var d in dirs)
				foreach (var f in d.Children.Where(c => !c.Dir)) {
					f.Lba = f.Size == 0 ? 0 : lba;
					lba += (f.Size + Sector - 1) / Sector;
					files.Add(f);
				}
			using (var o = File.Create(iso)) {
				o.Write(new byte[16 * Sector], 0, 16 * Sector);
				var pvd = new byte[Sector];
				pvd[0] = 1; Text(pvd, 1, 5, "CD001"); pvd[6] = 1;
				Text(pvd, 8, 32, "");
				Text(pvd, 40, 32, volume.ToUpperInvariant());
				Both32(pvd, 80, lba);
				Both16(pvd, 120, 1); Both16(pvd, 124, 1); Both16(pvd, 128, Sector);
				Both32(pvd, 132, ptSize);
				BitConverter.GetBytes(18).CopyTo(pvd, 140);
				var m = BitConverter.GetBytes(18 + ptSectors); Array.Reverse(m); m.CopyTo(pvd, 148);
				Record(root, "\0", root.Time).CopyTo(pvd, 156);
				Text(pvd, 190, 128 * 4, "");
				Text(pvd, 318, 128, "GEOSONE");
				Text(pvd, 574, 128, "MKWIN3UPD");
				Text(pvd, 702, 37 * 3, "");
				string now = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "00";
				Text(pvd, 813, 16, now); Text(pvd, 830, 16, now);
				Text(pvd, 847, 16, "0000000000000000"); Text(pvd, 864, 16, "0000000000000000");
				pvd[881] = 1;
				o.Write(pvd, 0, Sector);
				var term = new byte[Sector];
				term[0] = 255; Text(term, 1, 5, "CD001"); term[6] = 1;
				o.Write(term, 0, Sector);
				// path tables: little and big endian
				for (int big = 0; big < 2; big++) {
					var t = pt.ToArray();
					int at = 0;
					foreach (var d in dirs) {
						var l = BitConverter.GetBytes((uint)d.Lba);
						var p = BitConverter.GetBytes((ushort)(d.Parent == null ? 1 : d.Parent.Number));
						if (big == 1) { Array.Reverse(l); Array.Reverse(p); }
						l.CopyTo(t, at + 2); p.CopyTo(t, at + 6);
						at += 8 + t[at] + t[at] % 2;
					}
					var s = new byte[ptSectors * Sector];
					t.CopyTo(s, 0);
					o.Write(s, 0, s.Length);
				}
				foreach (var d in dirs) {
					var buf = new byte[d.Size];
					int pos = 0;
					var recs = new List<byte[]> { Record(d, "\0", d.Time), Record(d.Parent ?? d, "\x01", (d.Parent ?? d).Time) };
					recs.AddRange(d.Children.Select(c => Record(c, IsoName(c), c.Time)));
					foreach (var r in recs) {
						if (pos % Sector + r.Length > Sector) pos += Sector - pos % Sector;
						r.CopyTo(buf, pos);
						pos += r.Length;
					}
					o.Write(buf, 0, buf.Length);
				}
				foreach (var f in files) {
					var data = File.ReadAllBytes(f.Path);
					o.Write(data, 0, data.Length);
					int pad = (int)((Sector - data.Length % Sector) % Sector);
					o.Write(new byte[pad], 0, pad);
				}
			}
		}
	}
}
