using System.Numerics;
using System.Text;

// Đọc file PEM, giải mã Base64 -> DER, rồi giải thích ý nghĩa từng nhóm byte (cấu trúc ASN.1 TLV).
public static class PemInspector
{
    sealed class Node
    {
        public int Offset, HeaderLen, Len;
        public byte Tag;
        public List<Node> Kids = new();
        public bool Nested;            // BIT STRING / OCTET STRING chứa một khối DER lồng bên trong
        public string Desc = "";
    }

    // ---------- phân tích DER ----------
    static Node Parse(byte[] d, int pos, int limit)
    {
        if (pos + 2 > limit) throw new FormatException("Dữ liệu DER bị cụt.");
        var n = new Node { Offset = pos, Tag = d[pos] };
        int b = d[pos + 1], len, hdr;
        if (b < 0x80) { len = b; hdr = 2; }
        else
        {
            int nb = b & 0x7F;
            if (nb == 0 || nb > 4 || pos + 2 + nb > limit) throw new FormatException("Độ dài DER không hợp lệ.");
            len = 0;
            for (int i = 0; i < nb; i++) len = (len << 8) | d[pos + 2 + i];
            hdr = 2 + nb;
        }
        if (len < 0 || pos + hdr + len > limit) throw new FormatException("Độ dài vượt quá dữ liệu.");
        n.HeaderLen = hdr;
        n.Len = len;
        int start = pos + hdr, end = start + len;

        if (n.Tag == 0x30)
            n.Kids = ParseList(d, start, end);
        else if (n.Tag == 0x03 && len > 1 && d[start] == 0 && d[start + 1] == 0x30)
        {
            var k = TryList(d, start + 1, end);
            if (k != null) { n.Kids = k; n.Nested = true; }
        }
        else if (n.Tag == 0x04 && len > 0 && d[start] == 0x30)
        {
            var k = TryList(d, start, end);
            if (k != null) { n.Kids = k; n.Nested = true; }
        }
        return n;
    }

    static List<Node> ParseList(byte[] d, int start, int end)
    {
        var list = new List<Node>();
        int pos = start;
        while (pos < end)
        {
            var c = Parse(d, pos, end);
            list.Add(c);
            pos = c.Offset + c.HeaderLen + c.Len;
        }
        return list;
    }

    static List<Node>? TryList(byte[] d, int start, int end)
    {
        try { return ParseList(d, start, end); }
        catch (FormatException) { return null; }
    }

    static string Oid(byte[] b)
    {
        var sb = new StringBuilder();
        sb.Append(b[0] / 40).Append('.').Append(b[0] % 40);
        long v = 0;
        for (int i = 1; i < b.Length; i++)
        {
            v = (v << 7) | (uint)(b[i] & 0x7F);
            if ((b[i] & 0x80) == 0) { sb.Append('.').Append(v); v = 0; }
        }
        return sb.ToString();
    }

    // ---------- gắn mô tả cho từng nút ----------
    static readonly string[] PrivNames =
    {
        "version", "n – modulus (n = p·q)", "e – publicExponent", "d – privateExponent",
        "p – prime1", "q – prime2", "dP = d mod (p−1)", "dQ = d mod (q−1)", "qInv = q⁻¹ mod p"
    };
    static readonly string[] PubNames = { "n – modulus", "e – publicExponent" };

    static void Annotate(Node n, byte[] d, string role = "")
    {
        int start = n.Offset + n.HeaderLen;
        switch (n.Tag)
        {
            case 0x30:
            {
                string kind = "";
                var k = n.Kids;
                string[]? names = null;
                if (k.Count == 9 && k.All(x => x.Tag == 0x02)) { kind = "RSAPrivateKey (PKCS#1)"; names = PrivNames; }
                else if (k.Count == 2 && k.All(x => x.Tag == 0x02)) { kind = "RSAPublicKey (PKCS#1)"; names = PubNames; }
                else if (k.Count == 2 && k[0].Tag == 0x30 && k[1].Tag == 0x03) kind = "SubjectPublicKeyInfo (X.509)";
                else if (k.Count == 3 && k[0].Tag == 0x02 && k[1].Tag == 0x30 && k[2].Tag == 0x04) kind = "PrivateKeyInfo (PKCS#8)";
                else if (k.Count >= 1 && k[0].Tag == 0x06) kind = "AlgorithmIdentifier (thuật toán)";
                n.Desc = $"SEQUENCE (tag 30){(kind == "" ? "" : " = " + kind)}, dài {n.Len} byte";
                for (int i = 0; i < k.Count; i++)
                    Annotate(k[i], d, names != null ? names[i] : "");
                break;
            }
            case 0x02:
            {
                byte[] bytes = d.AsSpan(start, n.Len).ToArray();
                var v = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
                string val = n.Len <= 4 ? $" = {v}" : $", {v.GetBitLength()} bit";
                string pad = bytes.Length > 1 && bytes[0] == 0 ? "; byte 00 đầu: đệm để số luôn dương" : "";
                n.Desc = $"INTEGER (tag 02){(role == "" ? "" : " → " + role)}{val}{pad}";
                break;
            }
            case 0x03:
            {
                int unused = n.Len > 0 ? d[start] : 0;
                n.Desc = $"BIT STRING (tag 03), dài {n.Len} byte; byte đầu = {unused} bit đệm" +
                         (n.Nested ? "; nội dung là một khối DER lồng bên trong ↓" : "");
                foreach (var c in n.Kids) Annotate(c, d);
                break;
            }
            case 0x04:
                n.Desc = $"OCTET STRING (tag 04), dài {n.Len} byte" +
                         (n.Nested ? "; nội dung là một khối DER lồng bên trong ↓" : "");
                foreach (var c in n.Kids) Annotate(c, d);
                break;
            case 0x05:
                n.Desc = "NULL (tag 05), không có dữ liệu";
                break;
            case 0x06:
            {
                string oid = Oid(d.AsSpan(start, n.Len).ToArray());
                string name = oid == "1.2.840.113549.1.1.1" ? " = rsaEncryption (thuật toán RSA)" : "";
                n.Desc = $"OBJECT IDENTIFIER (tag 06) = {oid}{name}";
                break;
            }
            default:
                n.Desc = $"tag {n.Tag:X2}, dài {n.Len} byte";
                break;
        }
    }

    static void Print(Node n, byte[] d, int depth, StringBuilder sb)
    {
        string pad = new string(' ', depth * 3);
        string hdr = BitConverter.ToString(d, n.Offset, n.HeaderLen).Replace('-', ' ');
        string val = "";
        int vs = n.Offset + n.HeaderLen;
        if (n.Tag != 0x30 && n.Len > 0)
        {
            int show = Math.Min(n.Len, 8);
            val = " | " + BitConverter.ToString(d, vs, show).Replace('-', ' ') +
                  (n.Len > show ? $" … (+{n.Len - show} byte)" : "");
        }
        sb.AppendLine($"{pad}[byte {n.Offset,4}] {hdr}{val}");
        sb.AppendLine($"{pad}            ↳ {n.Desc}");
        foreach (var k in n.Kids) Print(k, d, depth + 1, sb);
    }

    // ---------- hàm chính ----------
    public static string Explain(string pem)
    {
        var lines = pem.Replace("\r", "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        int bi = lines.FindIndex(l => l.StartsWith("-----BEGIN "));
        int ei = lines.FindIndex(l => l.StartsWith("-----END "));
        if (bi < 0 || ei <= bi) throw new FormatException("Không phải file PEM hợp lệ.");
        string label = lines[bi][11..^5];
        var bodyLines = lines.Skip(bi + 1).Take(ei - bi - 1).ToList();
        if (bodyLines.Any(l => l.Contains(':')))
            throw new FormatException("PEM có tiêu đề phụ (khoá bị mã hoá bằng mật khẩu) – chưa hỗ trợ.");
        string body = string.Concat(bodyLines);
        byte[] der = Convert.FromBase64String(body);

        var sb = new StringBuilder();
        sb.AppendLine("============ PHẦN 1: CẤU TRÚC FILE PEM ============");
        sb.AppendLine($"Dòng đầu  : -----BEGIN {label}-----   → nhãn cho biết loại dữ liệu và định dạng bên trong:");
        sb.AppendLine("              PUBLIC KEY      = khoá công khai, định dạng X.509 SubjectPublicKeyInfo");
        sb.AppendLine("              RSA PUBLIC KEY  = khoá công khai, định dạng PKCS#1 (chỉ gồm n, e)");
        sb.AppendLine("              PRIVATE KEY     = khoá bí mật, định dạng PKCS#8 (bọc thêm thông tin thuật toán)");
        sb.AppendLine("              RSA PRIVATE KEY = khoá bí mật, định dạng PKCS#1 (n, e, d, p, q, dP, dQ, qInv)");
        sb.AppendLine($"Phần thân : {body.Length} ký tự Base64 chia thành {bodyLines.Count} dòng (mỗi dòng tối đa 64 ký tự).");
        sb.AppendLine($"            Mỗi 4 ký tự Base64 mã hoá 3 byte → giải mã được {der.Length} byte dữ liệu DER (nhị phân).");
        if (body.Length >= 4 && der.Length >= 3)
            sb.AppendLine($"            Ví dụ: 4 ký tự đầu \"{body[..4]}\" ↔ 3 byte đầu {BitConverter.ToString(der, 0, 3).Replace('-', ' ')}");
        sb.AppendLine($"Dòng cuối : -----END {label}-----");
        sb.AppendLine();

        sb.AppendLine("============ PHẦN 2: CÁCH ĐỌC DER (ASN.1) ============");
        sb.AppendLine("Dữ liệu DER là chuỗi các khối TLV nối nhau:  Tag (1 byte) | Length (1+ byte) | Value");
        sb.AppendLine("  Tag    : cho biết kiểu dữ liệu: 30=SEQUENCE (nhóm), 02=INTEGER (số nguyên lớn),");
        sb.AppendLine("           03=BIT STRING, 04=OCTET STRING, 05=NULL, 06=OBJECT IDENTIFIER (mã thuật toán)");
        sb.AppendLine("  Length : nếu < 0x80 thì chính là độ dài; nếu byte đầu dạng 0x8N thì N byte kế tiếp");
        sb.AppendLine("           (số nguyên lớn, big-endian) mới là độ dài của Value.");
        if (der.Length >= 2)
        {
            int b1 = der[1];
            if (b1 >= 0x80)
            {
                int nb = b1 & 0x7F;
                int len = 0;
                for (int i = 0; i < nb; i++) len = (len << 8) | der[2 + i];
                sb.AppendLine($"Ví dụ với byte đầu của file: {BitConverter.ToString(der, 0, 2 + nb).Replace('-', ' ')}");
                sb.AppendLine($"  {der[0]:X2} = tag {(der[0] == 0x30 ? "SEQUENCE" : "?")};  {b1:X2} = 0x80 + {nb} → độ dài nằm trong {nb} byte kế tiếp;");
                sb.AppendLine($"  {BitConverter.ToString(der, 2, nb).Replace('-', ' ')} = {len} → phần Value dài {len} byte (tổng cộng {2 + nb + len} byte).");
            }
            else
                sb.AppendLine($"Ví dụ: {der[0]:X2} {der[1]:X2} → tag {der[0]:X2}, Value dài {der[1]} byte.");
        }
        sb.AppendLine("Lưu ý INTEGER: lưu dạng big-endian có dấu; nếu bit cao nhất của byte đầu là 1 thì phải thêm byte 00");
        sb.AppendLine("phía trước để số không bị hiểu là số âm (vì vậy n 2048 bit thường dài 257 byte, bắt đầu bằng 00).");
        sb.AppendLine();

        sb.AppendLine("============ PHẦN 3: CÂY CẤU TRÚC VÀ Ý NGHĨA TỪNG NHÓM BYTE ============");
        sb.AppendLine("(mỗi nút: [vị trí byte] byte tiêu đề (Tag+Length) | vài byte đầu của Value)");
        var root = Parse(der, 0, der.Length);
        Annotate(root, der);
        Print(root, der, 0, sb);
        return sb.ToString();
    }
}
