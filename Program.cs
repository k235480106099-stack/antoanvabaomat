using System.Numerics;
using System.Security.Cryptography;
using System.Text;

// ===== BÀI 2: RSA (mã hoá bất đối xứng), băm SHA-256, chữ ký số =====
class Program
{
    static RSA? priv;   // khoá có phần bí mật (vừa sinh hoặc import file PEM private)
    static RSA? pub;    // khoá công khai vừa import (nếu có)

    static RSA? PubKey() => pub ?? priv;

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        while (true)
        {
            Console.WriteLine("\n===== RSA / SHA-256 / CHỮ KÝ SỐ =====");
            Console.WriteLine($"Khoá bí mật: {(priv == null ? "chưa có" : priv.KeySize + " bit")} | Khoá công khai: {(PubKey() == null ? "chưa có" : PubKey()!.KeySize + " bit")}");
            Console.WriteLine("1. Sinh cặp khoá RSA");
            Console.WriteLine("2. Xuất cặp khoá ra file PEM");
            Console.WriteLine("3. Xem file PEM + ý nghĩa các byte");
            Console.WriteLine("4. Nhập (import) file PEM -> hiện n, e (và d, p, q, ...)");
            Console.WriteLine("5. Mã hoá dữ liệu (RSA-OAEP SHA-256) -> lưu file Base64");
            Console.WriteLine("6. Giải mã từ file Base64 + so sánh với dữ liệu gốc");
            Console.WriteLine("7. Đánh giá tốc độ: khoá công khai (e bé) và khoá bí mật (d to)");
            Console.WriteLine("8. Băm SHA-256 (giao diện) / đánh giá hiệu ứng thay đổi");
            Console.WriteLine("9. Chữ ký số (ký, xác minh, so sánh các phương pháp)");
            Console.WriteLine("0. Thoát");
            Console.Write("Chọn: ");
            string? choice = Console.ReadLine();
            if (choice == null) return;
            try
            {
                switch (choice.Trim())
                {
                    case "1": GenKey(); break;
                    case "2": ExportPem(); break;
                    case "3": ViewPem(); break;
                    case "4": ImportPem(); break;
                    case "5": EncryptMenu(); break;
                    case "6": DecryptMenu(); break;
                    case "7": BenchMenu(); break;
                    case "8": HashMenu(); break;
                    case "9": SignMenu(); break;
                    case "0": return;
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi: " + ex.Message); }
        }
    }

    static string Ask(string msg)
    {
        Console.Write(msg);
        return (Console.ReadLine() ?? "").Trim().Trim('"');
    }

    static string AskDef(string msg, string def)
    {
        string s = Ask($"{msg} [{def}]: ");
        return s == "" ? def : s;
    }

    static RSA NeedPriv() => priv ?? throw new InvalidOperationException("Chưa có khoá bí mật. Hãy sinh khoá (1) hoặc import file PEM private (4).");
    static RSA NeedPub() => PubKey() ?? throw new InvalidOperationException("Chưa có khoá. Hãy sinh khoá (1) hoặc import file PEM (4).");

    static RSA LoadPem(string path, out bool hasPrivate)
    {
        string text = File.ReadAllText(path);
        hasPrivate = text.Contains("PRIVATE KEY");
        var r = RSA.Create();
        r.ImportFromPem(text);
        return r;
    }

    // ---------- 1 ----------
    static void GenKey()
    {
        int bits = int.Parse(AskDef("Độ dài khoá (1024/2048/3072/4096)", "2048"));
        if (bits != 1024 && bits != 2048 && bits != 3072 && bits != 4096) throw new ArgumentException("Chỉ hỗ trợ 1024/2048/3072/4096.");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        priv = RsaCore.Generate(bits);
        pub = null;
        var p = priv.ExportParameters(false);   // buộc thư viện sinh khoá xong rồi mới dừng đồng hồ
        sw.Stop();
        Console.WriteLine($"Đã sinh cặp khoá RSA {bits} bit trong {sw.Elapsed.TotalMilliseconds:F0} ms.");
        Console.WriteLine($"n có {RsaCore.Big(p.Modulus!).GetBitLength()} bit, e = {RsaCore.Big(p.Exponent!)}");
        Console.WriteLine("Dùng menu 2 để lưu ra file PEM, menu 4 để xem đầy đủ n, e, d, p, q.");
    }

    // ---------- 2 ----------
    static void ExportPem()
    {
        var r = NeedPriv();
        Console.WriteLine("Định dạng: 1 = PKCS#8 (BEGIN PRIVATE KEY) + X.509 (BEGIN PUBLIC KEY)   2 = PKCS#1 (BEGIN RSA ...)");
        string f = AskDef("Chọn", "1");
        string prefix = AskDef("Tên file (tiền tố)", "rsa");
        string pubPath = prefix + "_public.pem", privPath = prefix + "_private.pem";
        if (f == "2")
        {
            File.WriteAllText(pubPath, r.ExportRSAPublicKeyPem());
            File.WriteAllText(privPath, r.ExportRSAPrivateKeyPem());
        }
        else
        {
            File.WriteAllText(pubPath, r.ExportSubjectPublicKeyInfoPem());
            File.WriteAllText(privPath, r.ExportPkcs8PrivateKeyPem());
        }
        Console.WriteLine("Đã lưu khoá công khai: " + Path.GetFullPath(pubPath));
        Console.WriteLine("Đã lưu khoá bí mật   : " + Path.GetFullPath(privPath));
        Console.WriteLine("CHÚ Ý: file private chứa khoá bí mật, không được đưa lên GitHub (thêm *.pem vào .gitignore).");
    }

    // ---------- 3 ----------
    static void ViewPem()
    {
        string path = AskDef("File PEM cần xem", "rsa_public.pem");
        string text = File.ReadAllText(path);
        Console.WriteLine("----- Nội dung file -----");
        Console.WriteLine(text.TrimEnd());
        Console.WriteLine();
        Console.WriteLine(PemInspector.Explain(text));
    }

    // ---------- 4 ----------
    static void ImportPem()
    {
        string path = AskDef("File PEM cần import", "rsa_private.pem");
        var r = LoadPem(path, out bool hasPriv);
        if (hasPriv) { priv = r; pub = r; } else pub = r;
        Console.WriteLine(hasPriv
            ? $"Đã import KHOÁ BÍ MẬT ({r.KeySize} bit). Có thể mã hoá, giải mã, ký."
            : $"Đã import KHOÁ CÔNG KHAI ({r.KeySize} bit). Chỉ có n và e: dùng để mã hoá hoặc xác minh chữ ký.");
        Console.WriteLine(RsaCore.Describe(r.ExportParameters(hasPriv), hasPriv));
    }

    // ---------- 5 ----------
    static void EncryptMenu()
    {
        var r = NeedPub();
        int max = RsaCore.MaxPlain(r.KeySize);
        Console.WriteLine($"Khoá {r.KeySize} bit, RSA-OAEP SHA-256: mã hoá được tối đa {max} byte mỗi lần.");
        Console.WriteLine("Dữ liệu: 1 = nhập chuỗi văn bản   2 = sinh ngẫu nhiên một khoá AES-256 (32 byte)");
        byte[] data;
        if (AskDef("Chọn", "1") == "2")
        {
            data = RandomNumberGenerator.GetBytes(32);
            Console.WriteLine("Khoá AES-256 ngẫu nhiên (Base64): " + Convert.ToBase64String(data));
        }
        else
        {
            data = Encoding.UTF8.GetBytes(Ask("Nhập văn bản: "));
        }
        Console.WriteLine($"Độ dài dữ liệu: {data.Length} byte");
        if (data.Length == 0 || data.Length > max)
            throw new ArgumentException($"Dữ liệu phải từ 1 đến {max} byte.");

        string srcPath = AskDef("File lưu dữ liệu gốc (Base64, để so sánh sau)", "data_goc.b64");
        string outPath = AskDef("File lưu bản mã (Base64)", "data_mahoa.b64");
        File.WriteAllText(srcPath, Convert.ToBase64String(data));

        byte[] ct = RsaCore.Encrypt(r, data);
        File.WriteAllText(outPath, Convert.ToBase64String(ct));
        Console.WriteLine($"Bản mã dài {ct.Length} byte (= độ dài n), Base64:");
        Console.WriteLine(Convert.ToBase64String(ct));
        Console.WriteLine($"Đã lưu: {Path.GetFullPath(outPath)}");
        Console.WriteLine("Mã hoá lại cùng dữ liệu sẽ cho bản mã khác (OAEP có thành phần ngẫu nhiên).");
    }

    // ---------- 6 ----------
    static void DecryptMenu()
    {
        string pemPath = AskDef("File PEM khoá bí mật", "rsa_private.pem");
        string ctPath = AskDef("File bản mã (Base64)", "data_mahoa.b64");
        string srcPath = AskDef("File dữ liệu gốc (Base64) để so sánh", "data_goc.b64");

        var r = LoadPem(pemPath, out bool hasPriv);
        if (!hasPriv) throw new InvalidOperationException("File PEM này là khoá công khai, không giải mã được.");
        byte[] ct = Convert.FromBase64String(File.ReadAllText(ctPath).Trim());
        byte[] plain;
        try { plain = RsaCore.Decrypt(r, ct); }
        catch (CryptographicException) { throw new InvalidOperationException("Giải mã thất bại: sai khoá hoặc bản mã bị sửa."); }

        Console.WriteLine($"Giải mã được {plain.Length} byte. Base64: {Convert.ToBase64String(plain)}");
        try
        {
            string s = new UTF8Encoding(false, true).GetString(plain);
            Console.WriteLine("Dạng văn bản UTF-8: " + s);
        }
        catch (ArgumentException) { Console.WriteLine("(dữ liệu nhị phân, không phải văn bản UTF-8)"); }

        byte[] src = Convert.FromBase64String(File.ReadAllText(srcPath).Trim());
        bool same = plain.AsSpan().SequenceEqual(src);
        Console.WriteLine(same
            ? $"SO SÁNH VỚI NGUỒN: GIỐNG NHAU hoàn toàn theo byte ({src.Length} byte)."
            : "SO SÁNH VỚI NGUỒN: KHÁC NHAU!");
    }

    // ---------- 7 ----------
    static void BenchMenu()
    {
        string s = AskDef("Các độ dài khoá, cách nhau dấu phẩy (4096 sinh khoá khá lâu)", "1024,2048,3072");
        var sizes = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToList();
        var rows = new List<RsaCore.BenchRow>();
        foreach (int bits in sizes)
        {
            Console.WriteLine($"Đang đo RSA-{bits} ...");
            rows.Add(RsaCore.Benchmark(bits, Console.WriteLine));
        }

        Console.WriteLine("\n===== KẾT QUẢ (thời gian trung bình mỗi phép tính, đơn vị ms) =====");
        foreach (var x in rows)
        {
            Console.WriteLine($"\nRSA-{x.Bits}: sinh khoá {x.GenMs:F0} ms | e dài {x.EBits} bit | d dài {x.DBits} bit");
            Console.WriteLine($"  Luỹ thừa thô m^e mod n  (khoá công khai, e bé)      : {x.RawPub,10:F4} ms");
            Console.WriteLine($"  Luỹ thừa thô m^d mod n  (khoá bí mật, d to)        : {x.RawPriv,10:F4} ms   -> chậm gấp {x.RawPriv / x.RawPub:F0} lần");
            Console.WriteLine($"  RSA-OAEP Encrypt (thư viện, dùng khoá công khai)   : {x.LibEnc,10:F4} ms");
            Console.WriteLine($"  RSA-OAEP Decrypt (thư viện, khoá bí mật, có CRT)   : {x.LibDec,10:F4} ms   -> chậm gấp {x.LibDec / x.LibEnc:F1} lần");
        }
        Console.WriteLine("\n--- Bảng tổng hợp ---");
        Console.WriteLine($"{"Khoá",-9}{"e(bit)",7}{"d(bit)",8}{"m^e mod n",13}{"m^d mod n",13}{"d/e",7}{"Encrypt",11}{"Decrypt",11}{"Dec/Enc",9}");
        foreach (var x in rows)
            Console.WriteLine($"{"RSA-" + x.Bits,-9}{x.EBits,7}{x.DBits,8}{x.RawPub,13:F4}{x.RawPriv,13:F4}{x.RawPriv / x.RawPub,7:F0}{x.LibEnc,11:F4}{x.LibDec,11:F4}{x.LibDec / x.LibEnc,9:F1}");
        Console.WriteLine("Đơn vị thời gian: ms. 'Luỹ thừa thô' tự tính bằng BigInteger.ModPow; Encrypt/Decrypt là hàm của thư viện.");
    }

    // ---------- 8 ----------
    static void HashMenu()
    {
        Console.WriteLine("1 = Mở giao diện SHA-256 (cửa sổ)   2 = So sánh hai chuỗi trên console   3 = Thống kê: đảo từng bit đầu vào");
        switch (AskDef("Chọn", "1"))
        {
            case "2": HashConsole(); break;
            case "3": HashStats(); break;
            default:
                Console.WriteLine("Đang mở cửa sổ... (đóng cửa sổ để quay lại menu)");
                HashGui.Show();
                break;
        }
    }

    static void HashConsole()
    {
        string a = Ask("Dữ liệu A: ");
        string b = Ask("Dữ liệu B (sửa rất ít so với A): ");
        byte[] ha = RsaCore.Sha256(Encoding.UTF8.GetBytes(a)), hb = RsaCore.Sha256(Encoding.UTF8.GetBytes(b));
        Console.WriteLine("SHA-256(A) = " + Convert.ToHexString(ha).ToLowerInvariant());
        Console.WriteLine("SHA-256(B) = " + Convert.ToHexString(hb).ToLowerInvariant());
        int bits = RsaCore.DiffBits(ha, hb);
        Console.WriteLine($"Số bit khác nhau: {bits} / 256 ({bits * 100.0 / 256:F1}%)");
    }

    static void HashStats()
    {
        byte[] msg = RandomNumberGenerator.GetBytes(64);
        byte[] h0 = RsaCore.Sha256(msg);
        int total = 0, min = int.MaxValue, max = 0, n = msg.Length * 8;
        for (int bit = 0; bit < n; bit++)
        {
            byte[] m2 = (byte[])msg.Clone();
            m2[bit / 8] ^= (byte)(1 << (bit % 8));
            int d = RsaCore.DiffBits(h0, RsaCore.Sha256(m2));
            total += d;
            min = Math.Min(min, d);
            max = Math.Max(max, d);
        }
        Console.WriteLine($"Thông điệp ngẫu nhiên {msg.Length} byte ({n} bit). Đảo lần lượt từng bit một ({n} lần thử):");
        Console.WriteLine($"  Số bit băm thay đổi: trung bình {total / (double)n:F2} / 256 ({total * 100.0 / n / 256:F1}%), ít nhất {min}, nhiều nhất {max}");
        Console.WriteLine("  Lý tưởng là 128 (50%): đổi 1 bit đầu vào làm đổi khoảng một nửa số bit của giá trị băm.");
    }

    // ---------- 9 ----------
    static void SignMenu()
    {
        Console.WriteLine("1 = Demo: ký cùng một dữ liệu nhiều lần với 3 phương pháp");
        Console.WriteLine("2 = Ký dữ liệu -> lưu chữ ký (Base64)");
        Console.WriteLine("3 = Xác minh chữ ký");
        switch (AskDef("Chọn", "1"))
        {
            case "2": SignOne(); break;
            case "3": VerifyOne(); break;
            default: SignDemo(); break;
        }
    }

    static string MethodName(int m) => m switch
    {
        1 => "PKCS#1 v1.5 (xác định)",
        2 => "RSA-PSS (salt ngẫu nhiên)",
        _ => "Băm ngẫu nhiên hoá (nonce || chữ ký)"
    };

    static string Short(byte[] b)
    {
        string s = Convert.ToBase64String(b);
        return s.Length > 44 ? s[..44] + "..." : s;
    }

    static void SignDemo()
    {
        var r = NeedPriv();
        string text = AskDef("Dữ liệu cần ký", "Hop dong so 001: chuyen 10.000.000 VND");
        byte[] data = Encoding.UTF8.GetBytes(text);
        Console.WriteLine("SHA-256(dữ liệu) = " + Convert.ToHexString(RsaCore.Sha256(data)).ToLowerInvariant());
        Console.WriteLine("Chữ ký = RSA dùng khoá bí mật trên giá trị băm của dữ liệu.\n");

        for (int m = 1; m <= 3; m++)
        {
            Console.WriteLine($"--- Phương pháp {m}: {MethodName(m)} ---");
            var sigs = new List<byte[]>();
            for (int i = 1; i <= 3; i++)
            {
                byte[] s = RsaCore.SignData(r, data, m);
                sigs.Add(s);
                bool ok = RsaCore.VerifyData(r, data, s, m);
                Console.WriteLine($"  Lần ký {i}: {Short(s)}  (dài {s.Length} byte, xác minh: {(ok ? "ĐÚNG" : "SAI")})");
            }
            bool allSame = sigs.All(x => x.AsSpan().SequenceEqual(sigs[0]));
            Console.WriteLine($"  => 3 chữ ký {(allSame ? "GIỐNG NHAU (không đạt mong muốn)" : "KHÁC NHAU (đạt mong muốn)")}");

            byte[] tampered = Encoding.UTF8.GetBytes(text + ".");
            bool t1 = RsaCore.VerifyData(r, tampered, sigs[0], m);
            byte[] badSig = (byte[])sigs[0].Clone();
            badSig[badSig.Length - 1] ^= 1;
            bool t2 = RsaCore.VerifyData(r, data, badSig, m);
            Console.WriteLine($"  Sửa dữ liệu 1 ký tự -> xác minh: {(t1 ? "ĐÚNG (lỗi!)" : "SAI (đúng như mong đợi)")}");
            Console.WriteLine($"  Sửa 1 bit của chữ ký -> xác minh: {(t2 ? "ĐÚNG (lỗi!)" : "SAI (đúng như mong đợi)")}\n");
        }
    }

    static byte[] ReadData()
    {
        Console.WriteLine("Dữ liệu: 1 = nhập chuỗi   2 = đọc từ file");
        if (AskDef("Chọn", "1") == "2") return File.ReadAllBytes(Ask("Đường dẫn file: "));
        return Encoding.UTF8.GetBytes(Ask("Nhập chuỗi: "));
    }

    static void SignOne()
    {
        var r = NeedPriv();
        byte[] data = ReadData();
        Console.WriteLine("Phương pháp: 1 = PKCS#1 v1.5   2 = RSA-PSS   3 = băm ngẫu nhiên hoá (nonce)");
        int m = int.Parse(AskDef("Chọn", "2"));
        string outPath = AskDef("File lưu chữ ký (Base64)", "signature.b64");
        byte[] sig = RsaCore.SignData(r, data, m);
        File.WriteAllText(outPath, Convert.ToBase64String(sig));
        Console.WriteLine($"Đã ký bằng {MethodName(m)}. Chữ ký dài {sig.Length} byte, lưu tại {Path.GetFullPath(outPath)}");
        Console.WriteLine(Convert.ToBase64String(sig));
    }

    static void VerifyOne()
    {
        string pemPath = AskDef("File PEM khoá công khai", "rsa_public.pem");
        var r = LoadPem(pemPath, out _);
        byte[] data = ReadData();
        byte[] sig = Convert.FromBase64String(File.ReadAllText(AskDef("File chữ ký (Base64)", "signature.b64")).Trim());
        int m = int.Parse(AskDef("Phương pháp đã dùng khi ký (1/2/3)", "2"));
        Console.WriteLine(RsaCore.VerifyData(r, data, sig, m)
            ? "CHỮ KÝ HỢP LỆ: dữ liệu nguyên vẹn và đúng chủ khoá."
            : "CHỮ KÝ KHÔNG HỢP LỆ: dữ liệu hoặc chữ ký đã bị sửa, hoặc sai khoá/phương pháp.");
    }
}
