using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

// Các hàm xử lý RSA / băm / chữ ký dùng chung cho chương trình.
public static class RsaCore
{
    // ---------- tiện ích ----------
    public static BigInteger Big(byte[] b) => new BigInteger(b, isUnsigned: true, isBigEndian: true);
    public static string Hex(byte[] b) => Convert.ToHexString(b);

    static string Wrap(string s, int w = 64, string indent = "        ")
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i += w)
        {
            if (i > 0) sb.Append('\n').Append(indent);
            sb.Append(s, i, Math.Min(w, s.Length - i));
        }
        return sb.ToString();
    }

    // ---------- 1. sinh khoá ----------
    public static RSA Generate(int bits) => RSA.Create(bits);

    // ---------- 4. mô tả các thành phần của khoá ----------
    public static string Describe(RSAParameters p, bool hasPrivate)
    {
        var sb = new StringBuilder();
        BigInteger n = Big(p.Modulus!), e = Big(p.Exponent!);
        sb.AppendLine($"n (modulus)       : {n.GetBitLength()} bit ({p.Modulus!.Length} byte)");
        sb.AppendLine("    hex: " + Wrap(Hex(p.Modulus), 64, "         "));
        sb.AppendLine("    dec: " + Wrap(n.ToString(), 64, "         "));
        sb.AppendLine($"e (public exponent): {e}  (hex {Hex(p.Exponent!)}, {e.GetBitLength()} bit)");
        if (!hasPrivate) return sb.ToString();

        BigInteger d = Big(p.D!), P = Big(p.P!), Q = Big(p.Q!);
        sb.AppendLine($"d (private exponent): {d.GetBitLength()} bit");
        sb.AppendLine("    hex: " + Wrap(Hex(p.D!), 64, "         "));
        sb.AppendLine($"p (prime1)        : {P.GetBitLength()} bit");
        sb.AppendLine("    hex: " + Wrap(Hex(p.P!), 64, "         "));
        sb.AppendLine($"q (prime2)        : {Q.GetBitLength()} bit");
        sb.AppendLine("    hex: " + Wrap(Hex(p.Q!), 64, "         "));
        sb.AppendLine($"dP = d mod (p-1)  : {Big(p.DP!).GetBitLength()} bit");
        sb.AppendLine("    hex: " + Wrap(Hex(p.DP!), 64, "         "));
        sb.AppendLine($"dQ = d mod (q-1)  : {Big(p.DQ!).GetBitLength()} bit");
        sb.AppendLine("    hex: " + Wrap(Hex(p.DQ!), 64, "         "));
        sb.AppendLine($"qInv = q^-1 mod p : {Big(p.InverseQ!).GetBitLength()} bit");
        sb.AppendLine("    hex: " + Wrap(Hex(p.InverseQ!), 64, "         "));

        // kiểm tra các quan hệ toán học
        BigInteger lambda = (P - 1) * (Q - 1) / BigInteger.GreatestCommonDivisor(P - 1, Q - 1);
        sb.AppendLine("--- Kiểm tra quan hệ toán học ---");
        sb.AppendLine($"p * q == n               : {(P * Q == n ? "ĐÚNG" : "SAI")}");
        sb.AppendLine($"e * d ≡ 1 (mod λ(n))     : {((e * d) % lambda == 1 ? "ĐÚNG" : "SAI")}   (λ(n) = bội chung nhỏ nhất của p-1 và q-1)");
        sb.AppendLine($"dP == d mod (p-1)        : {(d % (P - 1) == Big(p.DP!) ? "ĐÚNG" : "SAI")}");
        sb.AppendLine($"dQ == d mod (q-1)        : {(d % (Q - 1) == Big(p.DQ!) ? "ĐÚNG" : "SAI")}");
        sb.AppendLine($"q * qInv ≡ 1 (mod p)     : {((Q * Big(p.InverseQ!)) % P == 1 ? "ĐÚNG" : "SAI")}");
        return sb.ToString();
    }

    // ---------- 5, 6. mã hoá / giải mã (RSA-OAEP, SHA-256) ----------
    public static readonly RSAEncryptionPadding Oaep = RSAEncryptionPadding.OaepSHA256;

    // số byte tối đa mã hoá được một lần: k - 2*hLen - 2  (k = số byte của n, hLen = 32)
    public static int MaxPlain(int keyBits) => keyBits / 8 - 2 * 32 - 2;

    public static byte[] Encrypt(RSA rsa, byte[] data) => rsa.Encrypt(data, Oaep);
    public static byte[] Decrypt(RSA rsa, byte[] cipher) => rsa.Decrypt(cipher, Oaep);

    // ---------- 7. đo tốc độ ----------
    public static double AvgMs(Action a, int minMs = 400, int minIter = 3)
    {
        a(); // chạy khởi động
        var sw = Stopwatch.StartNew();
        int n = 0;
        do { a(); n++; } while (sw.ElapsedMilliseconds < minMs || n < minIter);
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds / n;
    }

    public record BenchRow(int Bits, double GenMs, int EBits, int DBits,
                           double RawPub, double RawPriv, double LibEnc, double LibDec);

    public static BenchRow Benchmark(int bits, Action<string>? log = null)
    {
        var sw = Stopwatch.StartNew();
        using var rsa = RSA.Create(bits);
        var prm = rsa.ExportParameters(true);   // tính cả thời gian khởi tạo khoá
        double gen = sw.Elapsed.TotalMilliseconds;

        BigInteger n = Big(prm.Modulus!), e = Big(prm.Exponent!), d = Big(prm.D!);
        int k = prm.Modulus!.Length;
        BigInteger m = Big(RandomNumberGenerator.GetBytes(k - 1)); // m < n

        BigInteger c = BigInteger.ModPow(m, e, n);
        if (BigInteger.ModPow(c, d, n) != m) throw new InvalidOperationException("Luỹ thừa thô không khôi phục được m");

        log?.Invoke($"  RSA-{bits}: đo luỹ thừa thô...");
        double rawPub = AvgMs(() => BigInteger.ModPow(m, e, n));
        double rawPriv = AvgMs(() => BigInteger.ModPow(c, d, n), 400, 3);

        log?.Invoke($"  RSA-{bits}: đo OAEP của thư viện...");
        byte[] msg = RandomNumberGenerator.GetBytes(32);
        byte[] ct = rsa.Encrypt(msg, Oaep);
        double libEnc = AvgMs(() => rsa.Encrypt(msg, Oaep));
        double libDec = AvgMs(() => rsa.Decrypt(ct, Oaep));

        return new BenchRow(bits, gen, (int)e.GetBitLength(), (int)d.GetBitLength(), rawPub, rawPriv, libEnc, libDec);
    }

    // ---------- 8. băm SHA-256 ----------
    public static byte[] Sha256(byte[] data) => SHA256.HashData(data);

    public static int DiffBits(byte[] a, byte[] b)
    {
        int c = 0;
        for (int i = 0; i < a.Length; i++) c += BitOperations.PopCount((uint)(a[i] ^ b[i]));
        return c;
    }

    public static string ToBits(byte[] b)
    {
        var sb = new StringBuilder();
        foreach (byte x in b) sb.Append(Convert.ToString(x, 2).PadLeft(8, '0'));
        return sb.ToString();
    }

    // ---------- 9. chữ ký số ----------
    // method 1: PKCS#1 v1.5 (xác định: cùng dữ liệu + cùng khoá => cùng chữ ký)
    // method 2: RSA-PSS (thêm salt ngẫu nhiên => mỗi lần ký một chữ ký khác nhau)
    // method 3: băm ngẫu nhiên hoá: nonce 16 byte || ký PKCS#1 v1.5 của SHA-256(dữ liệu || nonce)
    public const int NonceLen = 16;

    public static byte[] SignData(RSA rsa, byte[] data, int method)
    {
        switch (method)
        {
            case 1:
                return rsa.SignHash(Sha256(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            case 2:
                return rsa.SignHash(Sha256(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            case 3:
                byte[] nonce = RandomNumberGenerator.GetBytes(NonceLen);
                byte[] h = Sha256(Concat(data, nonce));
                byte[] s = rsa.SignHash(h, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Concat(nonce, s);
            default:
                throw new ArgumentException("method không hợp lệ");
        }
    }

    public static bool VerifyData(RSA rsa, byte[] data, byte[] sig, int method)
    {
        try
        {
            switch (method)
            {
                case 1:
                    return rsa.VerifyHash(Sha256(data), sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                case 2:
                    return rsa.VerifyHash(Sha256(data), sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                case 3:
                    if (sig.Length <= NonceLen) return false;
                    byte[] nonce = sig[..NonceLen];
                    byte[] s = sig[NonceLen..];
                    return rsa.VerifyHash(Sha256(Concat(data, nonce)), s, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                default:
                    return false;
            }
        }
        catch (CryptographicException) { return false; }
    }

    public static byte[] Concat(byte[] a, byte[] b)
    {
        var r = new byte[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }
}
