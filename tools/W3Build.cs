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
