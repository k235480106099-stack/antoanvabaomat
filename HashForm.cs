using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

// Giao diện sinh chuỗi băm SHA-256 và so sánh khi đầu vào thay đổi rất ít.
public static class HashGui
{
    static bool styled;

    public static void Show()
    {
        // WinForms cần luồng STA nên chạy form trên một luồng riêng
        var t = new Thread(() =>
        {
            if (!styled)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                styled = true;
            }
            Application.Run(new HashForm());
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
    }
}

sealed class HashForm : Form
{
    readonly TextBox txtA = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical };
    readonly TextBox txtB = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical };
    readonly RichTextBox hexA, hexB, binA, binB;
    readonly Label lblInfo = new Label();
    readonly Font boldFont;

    RichTextBox MakeBox(int x, int y, int w, int h)
    {
        var r = new RichTextBox
        {
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 10.5f),
            ScrollBars = RichTextBoxScrollBars.None,
            WordWrap = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        r.SetBounds(x, y, w, h);
        Controls.Add(r);
        return r;
    }

    Label MakeLabel(string text, int x, int y)
    {
        var l = new Label { Text = text, Left = x, Top = y, AutoSize = true };
        Controls.Add(l);
        return l;
    }

    public HashForm()
    {
        Text = "SHA-256 – hiệu ứng thay đổi dữ liệu đầu vào";
        ClientSize = new Size(960, 740);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        boldFont = new Font("Consolas", 10.5f, FontStyle.Bold);

        int x = 12, w = 936;

        MakeLabel("Dữ liệu A (gốc):", x, 10);
        txtA.SetBounds(x, 34, w, 56);
        Controls.Add(txtA);

        MakeLabel("Dữ liệu B (sửa rất ít so với A):", x, 98);
        txtB.SetBounds(x, 122, w, 56);
        Controls.Add(txtB);

        var btnCopy = new Button { Text = "Chép A → B", Left = x, Top = 186, Width = 140, Height = 30 };
        var btnFlip = new Button { Text = "Đảo 1 bit ký tự cuối của B", Left = x + 150, Top = 186, Width = 240, Height = 30 };
        Controls.Add(btnCopy);
        Controls.Add(btnFlip);

        MakeLabel("SHA-256(A) – dạng hex (64 ký tự):", x, 226);
        hexA = MakeBox(x, 250, w, 32);
        MakeLabel("SHA-256(B) – dạng hex:", x, 288);
        hexB = MakeBox(x, 312, w, 32);
        MakeLabel("SHA-256(A) – dạng nhị phân (256 bit, ngăn cách từng byte):", x, 352);
        binA = MakeBox(x, 376, w, 76);
        MakeLabel("SHA-256(B) – dạng nhị phân (ký tự đỏ = khác với A):", x, 460);
        binB = MakeBox(x, 484, w, 76);

        lblInfo.SetBounds(x, 574, w, 150);
        lblInfo.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        Controls.Add(lblInfo);

        txtA.Text = "An toan va bao mat thong tin";
        txtB.Text = "An toan va bao mat thong tin.";

        txtA.TextChanged += (s, e) => Recalc();
        txtB.TextChanged += (s, e) => Recalc();
        btnCopy.Click += (s, e) => { txtB.Text = txtA.Text; };
        btnFlip.Click += (s, e) =>
        {
            string t = txtB.Text;
            if (t.Length == 0) return;
            char last = t[t.Length - 1];
            txtB.Text = t.Substring(0, t.Length - 1) + (char)(last ^ 1);
        };

        Recalc();
    }

    static string Group(string bits)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < bits.Length; i += 8)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(bits, i, 8);
        }
        return sb.ToString();
    }

    void Highlight(RichTextBox r, string own, string other)
    {
        r.Text = own;
        for (int i = 0; i < own.Length && i < other.Length; i++)
        {
            if (own[i] != other[i])
            {
                r.Select(i, 1);
                r.SelectionColor = Color.Red;
                r.SelectionFont = boldFont;
            }
        }
        r.Select(0, 0);
    }

    void Recalc()
    {
        byte[] a = Encoding.UTF8.GetBytes(txtA.Text);
        byte[] b = Encoding.UTF8.GetBytes(txtB.Text);
        byte[] ha = SHA256.HashData(a), hb = SHA256.HashData(b);
        string xa = Convert.ToHexString(ha).ToLowerInvariant();
        string xb = Convert.ToHexString(hb).ToLowerInvariant();
        string ga = Group(RsaCore.ToBits(ha)), gb = Group(RsaCore.ToBits(hb));

        Highlight(hexA, xa, xb);
        Highlight(hexB, xb, xa);
        Highlight(binA, ga, gb);
        Highlight(binB, gb, ga);

        int bits = RsaCore.DiffBits(ha, hb);
        int hexDiff = 0;
        for (int i = 0; i < xa.Length; i++) if (xa[i] != xb[i]) hexDiff++;

        string input = a.Length == b.Length
            ? $"Số bit khác nhau của dữ liệu đầu vào: {RsaCore.DiffBits(a, b)} / {a.Length * 8}"
            : $"Độ dài đầu vào khác nhau (A = {a.Length} byte, B = {b.Length} byte)";

        lblInfo.Text =
            input + "\n" +
            $"Số bit khác nhau của hai giá trị băm: {bits} / 256 ({bits * 100.0 / 256:F1}%)\n" +
            $"Số ký tự hex khác nhau: {hexDiff} / 64 ({hexDiff * 100.0 / 64:F1}%)\n" +
            (bits == 0 ? "A và B giống nhau nên hai giá trị băm giống nhau.\n" : "") +
            "Hiệu ứng thác lũ (avalanche): đầu vào chỉ đổi rất ít nhưng khoảng một nửa số bit của giá trị băm đổi (≈128 / 256).";
    }
}
