using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

// ====== BÀI 1: MÃ HOÁ AES (đối xứng) - Console App ======
// AES-CBC + PKCS7, IV ngẫu nhiên được ghi ở đầu dữ liệu mã hoá (16 byte đầu).
// Mã hoá file dùng streaming (CryptoStream) nên file hàng GB vẫn chạy tốt, không tốn RAM.

class Program
{
    static byte[]? key;
    const int BufSize = 4 * 1024 * 1024; // 4 MB

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        while (true)
        {
            Console.WriteLine("\n===== AES ENCRYPTION =====");
            Console.WriteLine($"Khoá hiện tại: {(key == null ? "chưa có" : key.Length * 8 + " bit")}");
            Console.WriteLine("1. Sinh khoá AES");
            Console.WriteLine("2. Lưu khoá ra file");
            Console.WriteLine("3. Import khoá từ file");
            Console.WriteLine("4. Mã hoá văn bản (kết quả in ra màn hình)");
            Console.WriteLine("5. Giải mã văn bản");
            Console.WriteLine("6. Mã hoá file");
            Console.WriteLine("7. Giải mã file");
            Console.WriteLine("8. So sánh 2 file theo byte");
            Console.WriteLine("9. Benchmark file lớn (tạo file -> mã hoá -> giải mã -> so sánh)");
            Console.WriteLine("0. Thoát");
            Console.Write("Chọn: ");
            try
            {
                switch (Console.ReadLine()?.Trim())
                {
                    case "1": GenKey(); break;
                    case "2": SaveKey(); break;
                    case "3": LoadKey(); break;
                    case "4": EncText(); break;
                    case "5": DecText(); break;
                    case "6": EncFileMenu(); break;
                    case "7": DecFileMenu(); break;
                    case "8": CompareMenu(); break;
                    case "9": Benchmark(); break;
                    case "0": return;
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi: " + ex.Message); }
        }
    }

    static string Ask(string msg) { Console.Write(msg); return (Console.ReadLine() ?? "").Trim().Trim('"'); }
    static void NeedKey() { if (key == null) throw new InvalidOperationException("Chưa có khoá. Hãy sinh khoá (1) hoặc import khoá (3)."); }

    // ---------- 1. Sinh khoá ----------
    static void GenKey()
    {
        string s = Ask("Độ dài khoá (128/192/256) [256]: ");
        int bits = s == "" ? 256 : int.Parse(s);
        if (bits != 128 && bits != 192 && bits != 256) throw new ArgumentException("Chỉ hỗ trợ 128/192/256.");
        using var aes = Aes.Create();
        aes.KeySize = bits;
        aes.GenerateKey();
        key = aes.Key;
        Console.WriteLine($"Đã sinh khoá {bits} bit: {Convert.ToBase64String(key)}");
    }

    // ---------- 2. Lưu khoá ----------
    static void SaveKey()
    {
        NeedKey();
        string path = Ask("Đường dẫn file lưu khoá [aes.key]: ");
        if (path == "") path = "aes.key";
        File.WriteAllText(path, Convert.ToBase64String(key!));
        Console.WriteLine("Đã lưu khoá vào: " + Path.GetFullPath(path));
    }

    // ---------- 3. Import khoá ----------
    static void LoadKey()
    {
        string path = Ask("Đường dẫn file khoá [aes.key]: ");
        if (path == "") path = "aes.key";
        byte[] k = Convert.FromBase64String(File.ReadAllText(path).Trim());
        if (k.Length != 16 && k.Length != 24 && k.Length != 32) throw new InvalidDataException("Khoá không hợp lệ.");
        key = k;
        Console.WriteLine($"Đã import khoá {k.Length * 8} bit.");
    }

    // ---------- 4, 5. Văn bản ----------
    public static string EncryptText(string plain, byte[] k)
    {
        using var aes = Aes.Create();
        aes.Key = k; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        byte[] ct = aes.EncryptCbc(Encoding.UTF8.GetBytes(plain), aes.IV, PaddingMode.PKCS7);
        byte[] all = new byte[16 + ct.Length];
        aes.IV.CopyTo(all, 0); ct.CopyTo(all, 16);
        return Convert.ToBase64String(all);
    }

    public static string DecryptText(string b64, byte[] k)
    {
        byte[] all = Convert.FromBase64String(b64);
        using var aes = Aes.Create();
        aes.Key = k;
        byte[] iv = all[..16], ct = all[16..];
        return Encoding.UTF8.GetString(aes.DecryptCbc(ct, iv, PaddingMode.PKCS7));
    }

    static void EncText()
    {
        NeedKey();
        string plain = Ask("Nhập văn bản cần mã hoá: ");
        Console.WriteLine("Bản mã (Base64): " + EncryptText(plain, key!));
    }

    static void DecText()
    {
        NeedKey();
        string b64 = Ask("Nhập bản mã (Base64): ");
        Console.WriteLine("Bản rõ: " + DecryptText(b64, key!));
    }

    // ---------- 6, 7. File (streaming, hợp với file hàng GB) ----------
    public static void EncryptFile(string src, string dst, byte[] k)
    {
        using var aes = Aes.Create();
        aes.Key = k; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        using var fin = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, BufSize, FileOptions.SequentialScan);
        using var fout = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None, BufSize);
        fout.Write(aes.IV, 0, 16);
        using var cs = new CryptoStream(fout, aes.CreateEncryptor(), CryptoStreamMode.Write);
        fin.CopyTo(cs, BufSize);
    }

    public static void DecryptFile(string src, string dst, byte[] k)
    {
        using var fin = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, BufSize, FileOptions.SequentialScan);
        byte[] iv = new byte[16];
        fin.ReadExactly(iv, 0, 16);
        using var aes = Aes.Create();
        aes.Key = k; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        using var cs = new CryptoStream(fin, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var fout = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None, BufSize);
        cs.CopyTo(fout, BufSize);
    }

    static void EncFileMenu()
    {
        NeedKey();
        string src = Ask("File nguồn: ");
        string dst = Ask($"File mã hoá đầu ra [{src}.enc]: "); if (dst == "") dst = src + ".enc";
        var sw = Stopwatch.StartNew();
        EncryptFile(src, dst, key!);
        sw.Stop();
        Console.WriteLine($"Mã hoá xong trong {sw.Elapsed.TotalSeconds:F3} s -> {dst}");
    }

    static void DecFileMenu()
    {
        NeedKey();
        string src = Ask("File mã hoá: ");
        string dst = Ask($"File giải mã đầu ra [{src}.dec]: "); if (dst == "") dst = src + ".dec";
        var sw = Stopwatch.StartNew();
        DecryptFile(src, dst, key!);
        sw.Stop();
        Console.WriteLine($"Giải mã xong trong {sw.Elapsed.TotalSeconds:F3} s -> {dst}");
    }

    // ---------- So sánh theo byte ----------
    public static bool CompareFiles(string a, string b, out long firstDiff)
    {
        firstDiff = -1;
        using var fa = new FileStream(a, FileMode.Open, FileAccess.Read, FileShare.Read, BufSize, FileOptions.SequentialScan);
        using var fb = new FileStream(b, FileMode.Open, FileAccess.Read, FileShare.Read, BufSize, FileOptions.SequentialScan);
        if (fa.Length != fb.Length) { firstDiff = Math.Min(fa.Length, fb.Length); return false; }
        byte[] ba = new byte[BufSize], bb = new byte[BufSize];
        long pos = 0;
        while (true)
        {
            int na = fa.Read(ba, 0, BufSize);
            if (na == 0) return true;
            fb.ReadExactly(bb, 0, na);
            if (!ba.AsSpan(0, na).SequenceEqual(bb.AsSpan(0, na)))
            {
                for (int i = 0; i < na; i++) if (ba[i] != bb[i]) { firstDiff = pos + i; break; }
                return false;
            }
            pos += na;
        }
    }

    static void CompareMenu()
    {
        string a = Ask("File 1 (nguồn): ");
        string b = Ask("File 2 (giải mã): ");
        var sw = Stopwatch.StartNew();
        bool same = CompareFiles(a, b, out long diff);
        sw.Stop();
        Console.WriteLine(same
            ? $"GIỐNG NHAU hoàn toàn theo byte ({new FileInfo(a).Length:N0} bytes). Thời gian so sánh: {sw.Elapsed.TotalSeconds:F3} s"
            : $"KHÁC NHAU tại byte thứ {diff:N0}");
    }

    // ---------- 9. Benchmark ----------
    static void Benchmark()
    {
        NeedKey();
        string s = Ask("Kích thước file test (MB) [1024]: ");
        long mb = s == "" ? 1024 : long.Parse(s);
        string dir = Path.Combine(Path.GetTempPath(), "aes_bench");
        Directory.CreateDirectory(dir);
        string src = Path.Combine(dir, "source.bin"), enc = Path.Combine(dir, "source.enc"), dec = Path.Combine(dir, "source.dec");

        Console.WriteLine($"Đang tạo file {mb} MB ngẫu nhiên...");
        using (var f = new FileStream(src, FileMode.Create, FileAccess.Write, FileShare.None, BufSize))
        {
            byte[] buf = new byte[BufSize];
            long remain = mb * 1024 * 1024;
            while (remain > 0)
            {
                int n = (int)Math.Min(BufSize, remain);
                RandomNumberGenerator.Fill(buf.AsSpan(0, n));
                f.Write(buf, 0, n);
                remain -= n;
            }
        }

        var sw = Stopwatch.StartNew();
        EncryptFile(src, enc, key!);
        double tEnc = sw.Elapsed.TotalSeconds;

        sw.Restart();
        DecryptFile(enc, dec, key!);
        double tDec = sw.Elapsed.TotalSeconds;

        sw.Restart();
        bool same = CompareFiles(src, dec, out long diff);
        double tCmp = sw.Elapsed.TotalSeconds;

        long sizeSrc = new FileInfo(src).Length, sizeEnc = new FileInfo(enc).Length, sizeDec = new FileInfo(dec).Length;
        Console.WriteLine("\n===== KẾT QUẢ =====");
        Console.WriteLine($"Độ dài khoá        : {key!.Length * 8} bit (AES-CBC, PKCS7)");
        Console.WriteLine($"Kích thước nguồn   : {sizeSrc:N0} bytes ({sizeSrc / 1048576.0:F1} MB)");
        Console.WriteLine($"Kích thước mã hoá  : {sizeEnc:N0} bytes (+{sizeEnc - sizeSrc} bytes: IV + padding)");
        Console.WriteLine($"Kích thước giải mã : {sizeDec:N0} bytes");
        Console.WriteLine($"Thời gian mã hoá   : {tEnc:F3} s  ({sizeSrc / 1048576.0 / tEnc:F1} MB/s)");
        Console.WriteLine($"Thời gian giải mã  : {tDec:F3} s  ({sizeSrc / 1048576.0 / tDec:F1} MB/s)");
        Console.WriteLine($"So sánh byte       : {(same ? "GIỐNG NHAU 100%" : "KHÁC tại byte " + diff)}  ({tCmp:F3} s)");

        if (Ask("Xoá file test? (y/n) [y]: ").ToLower() != "n")
        { File.Delete(src); File.Delete(enc); File.Delete(dec); }
    }
}
