**Môn:** An toàn và bảo mật thông tin

**Sinh viên:** Phạm Thanh Sơn – MSSV K235480106099 – Lớp K59KMT

**Trường:** Trường Đại học Kỹ thuật Công nghiệp – ĐH Thái Nguyên 


# Bài tập 1: Mã hoá AES (mã hoá đối xứng)

---

## 1. Yêu cầu

Xây dựng ứng dụng (không phải web) bằng .NET, dạng console, thực hiện mã hoá đối xứng AES (mã hoá và giải mã dùng chung một khoá) với các chức năng:

1. Sinh khoá AES
2. Lưu khoá ra file
3. Import khoá từ file
4. Mã hoá một đoạn văn bản, in kết quả ra màn hình
5. Mã hoá một file, lưu kết quả vào một file khác

Yêu cầu thử nghiệm: dùng file kích thước lớn (hàng GB), tính thời gian mã hoá và giải mã, so sánh theo byte file nguồn với file sau giải mã, và rút ra kết luận dựa trên số liệu.

## 2. Môi trường

- Ngôn ngữ / nền tảng: C#, .NET (SDK 10.0, `TargetFramework = net10.0`)
- Loại ứng dụng: Console
- Thư viện: `System.Security.Cryptography` (lớp `Aes`, `CryptoStream`)
- IDE: Visual Studio Code, chạy bằng `dotnet run -c Release`

## 3. Thiết kế

| Thông số | Giá trị |
|---|---|
| Thuật toán | AES (Rijndael, khối 128 bit) |
| Độ dài khoá | 128 / 192 / 256 bit (chọn lúc sinh khoá), thử nghiệm với 128 bit |
| Chế độ | CBC |
| Padding | PKCS7 |
| IV | 16 byte, sinh ngẫu nhiên mỗi lần mã hoá, **ghi ở 16 byte đầu** của dữ liệu mã hoá |
| Lưu khoá | Base64 trong file văn bản (`aes.key`) |
| Bản mã văn bản | `Base64(IV ‖ ciphertext)` |
| Bản mã file | `IV (16 byte) ‖ ciphertext` |

Với file, chương trình xử lý theo luồng (`CryptoStream`) với bộ đệm 4 MB, nên file hàng GB vẫn chạy được mà không tốn nhiều RAM. Việc so sánh hai file cũng đọc từng khối 4 MB và so sánh từng byte, nếu khác sẽ báo vị trí byte khác đầu tiên.

### Menu chương trình

| Phím | Chức năng |
|---|---|
| 1 | Sinh khoá AES |
| 2 | Lưu khoá ra file |
| 3 | Import khoá từ file |
| 4 / 5 | Mã hoá / giải mã văn bản |
| 6 / 7 | Mã hoá / giải mã file |
| 8 | So sánh 2 file theo byte |
| 9 | Benchmark: tạo file lớn, mã hoá, giải mã, so sánh, in thời gian |

## 4. Cách chạy

```
dotnet run -c Release
```

Thứ tự dùng: sinh khoá (1) hoặc import khoá (3) trước, sau đó dùng các chức năng còn lại. Mỗi lần chạy lại chương trình phải import đúng khoá đã dùng để mã hoá thì mới giải mã được.


## 5. Kết quả thử nghiệm

### 5.1. Kiểm tra các chức năng

**Sinh khoá AES (menu 1)** – khoá 128 bit, hiển thị dạng Base64:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/852e19f8-4e19-420f-90a6-188b52e6a7ee" />


**Lưu khoá ra file (menu 2)** – khoá được lưu vào `aes.key`:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/3374efa5-088a-400f-892f-daae400d2f13" />


**Import khoá từ file (menu 3)** – đọc lại `aes.key`, chương trình xác nhận khoá 128 bit:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/d1d96789-4362-4645-8e79-1c0947b1e993" />


**Mã hoá file (menu 6)** – mã hoá `test.txt` thành `test.txt.enc`:

<img width="1911" height="1078" alt="image" src="https://github.com/user-attachments/assets/2e43e16b-aff9-4a03-ab42-d6b52f21109f" />


**Giải mã file (menu 7)** – giải mã `test.txt.enc` thành `test_giaima.txt`:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/54930b6d-d0d7-4b45-96e7-0aec9b1ce0f2" />


**So sánh theo byte (menu 8)** – file nguồn và file giải mã giống nhau hoàn toàn (32 byte):

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/f6be30fc-e5a4-4b3f-876e-0c212819a536" />


### 5.2. Thử nghiệm file lớn (menu 9, AES-128, CBC, PKCS7)

**File 100 MB**

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/c6d8e12c-ad10-47d5-9249-a0abe08f9d05" />


**File 1 GB**

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/b7b28164-92b3-49db-aaa5-1f5570532c1e" />


**File 4 GB**

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/4d2d26c4-a573-4e80-b8d1-cf89464884c3" />


### 5.3. Bảng tổng hợp số liệu

| Kích thước nguồn | Kích thước file mã hoá | T mã hoá | Tốc độ mã hoá | T giải mã | Tốc độ giải mã | T so sánh byte | Kết quả so sánh |
|---|---|---|---|---|---|---|---|
| 100 MB (104.857.600 B) | 104.857.632 B | 0,169 s | 590,9 MB/s | 0,090 s | 1110,5 MB/s | 0,053 s | Giống 100% |
| 1 GB (1.073.741.824 B) | 1.073.741.856 B | 1,186 s | 863,2 MB/s | 0,744 s | 1377,1 MB/s | 0,408 s | Giống 100% |
| 4 GB (4.294.967.296 B) | 4.294.967.328 B | 21,692 s | 188,8 MB/s | 19,739 s | 207,5 MB/s | 3,485 s | Giống 100% |

## 6. Kết luận

1. **Đúng đắn:** ở cả ba kích thước (100 MB, 1 GB, 4 GB), file sau giải mã trùng khớp từng byte với file nguồn và có cùng kích thước. Chương trình mã hoá và giải mã đúng với cả file hàng GB.
2. **Kích thước tăng thêm:** file mã hoá luôn lớn hơn file nguồn đúng 32 byte, gồm 16 byte IV và 16 byte padding PKCS7 (kích thước nguồn chia hết cho 16 nên phải thêm một khối padding đầy đủ). Phần tăng thêm này không đáng kể.
3. **Thời gian theo kích thước:**
   - Từ 100 MB lên 1 GB (gấp khoảng 10,2 lần), thời gian mã hoá tăng khoảng 7,0 lần và giải mã tăng khoảng 8,3 lần.
   - Từ 1 GB lên 4 GB (gấp 4 lần), thời gian mã hoá tăng khoảng 18,3 lần và giải mã tăng khoảng 26,5 lần; tốc độ giảm từ hơn 860 MB/s xuống còn khoảng 190 MB/s.
   - Thời gian không tăng tuyến tính ở file rất lớn. Nhiều khả năng với file 100 MB và 1 GB dữ liệu còn nằm trong bộ nhớ đệm của hệ điều hành nên đọc ghi nhanh, còn với 4 GB (ba file, tổng khoảng 12 GB) thì vượt bộ đệm và tốc độ bị giới hạn bởi tốc độ ổ đĩa chứ không phải CPU. Đây là nhận định từ số liệu, chưa được kiểm chứng bằng phép đo riêng.
4. **Giải mã nhanh hơn mã hoá** ở cả ba lần (gấp khoảng 1,9; 1,6 và 1,1 lần). Một lý do có thể là trong chế độ CBC, mỗi khối khi mã hoá phụ thuộc kết quả khối trước nên khó song song, còn khi giải mã các khối độc lập nhau. Ở file 4 GB chênh lệch nhỏ vì thời gian bị chi phối chủ yếu bởi ổ đĩa.
5. **Lần chạy đầu** (100 MB) có tốc độ mã hoá thấp hơn lần 1 GB, có thể do chi phí khởi động và biên dịch JIT lần đầu.
6. **So sánh byte** nhanh hơn nhiều so với mã hoá (0,053 s; 0,408 s; 3,485 s) vì chỉ đọc file và so sánh bộ nhớ, không tính toán mật mã.
7. **Hạn chế:** thử nghiệm chỉ thực hiện với khoá 128 bit và mỗi kích thước chạy một lần, nên số liệu có thể dao động giữa các lần chạy. Chưa so sánh độ dài khoá 192/256 bit.

## 7. Cấu trúc thư mục

```
Aesapp/
├── Program.cs        # mã nguồn chương trình
├── AesApp.csproj     # file dự án .NET
├── .gitignore        # bỏ qua bin/, obj/, aes.key, *.enc ...
├── test.txt          # file thử nhỏ
├── README.md         # báo cáo này
└── images/           # ảnh minh chứng
```

# Bài tập 2: Mã hoá RSA, băm SHA-256 và chữ ký số


### 1. Yêu cầu đề bài và các chức năng đã làm
Xây dựng ứng dụng .NET cho mã hoá bất đối xứng RSA (mã hoá bằng một khoá, giải mã bằng khoá còn lại).
Câu	Yêu cầu	Chức năng trong chương trình	Kết quả
1	Sinh cặp khoá	Menu 1	5.1
2	Xuất cặp khoá ra file PEM	Menu 2	5.2
3	Xem PEM và ý nghĩa các byte trong định dạng PEM	Menu 3	5.3
4	Nhập PEM public (được n, e) và PEM private (được n, e, d, ...)	Menu 4	5.4
5	Mã hoá mảng byte không quá 190 byte (ví dụ khoá AES-256 = 32 byte), kết quả Base64 lưu file	Menu 5	5.5
6	Giải mã từ PEM và file Base64, so sánh với dữ liệu gốc	Menu 6	5.6
7	Đánh giá tốc độ khoá public (n, e bé) và khoá private (n, d lớn)	Menu 7	5.7
8	Giao diện sinh chuỗi băm SHA-256, đánh giá thay đổi của giá trị băm khi đầu vào thay đổi rất ít	Menu 8	5.8
9	Chữ ký số; đề xuất phương pháp để cùng dữ liệu, cùng người ký vẫn cho chữ ký khác nhau ở các lần ký	Menu 9	5.9 và phần 6
2. Môi trường và cách chạy
Ngôn ngữ / nền tảng: C#, .NET (SDK 10, `TargetFramework = net10.0-windows`)
Ứng dụng console kèm một cửa sổ WinForms cho phần SHA-256
Thư viện: `System.Security.Cryptography` (lớp `RSA`, `SHA256`), `System.Numerics.BigInteger`, `System.Windows.Forms`
IDE: Visual Studio Code
```
cd Bai2_RSA
dotnet run -c Release
```
Thứ tự dùng gợi ý: 1 (sinh khoá), 2 (xuất PEM), 3 (xem PEM), 4 (nhập PEM), 5 (mã hoá), 6 (giải mã), 7 (tốc độ), 8 (SHA-256), 9 (chữ ký số). Mỗi lần chạy lại chương trình, khoá trong bộ nhớ bị mất, vì vậy phải nạp lại bằng menu 4 từ file PEM đã lưu.
### 2. Cơ sở lý thuyết
### 2.1. RSA
Chọn hai số nguyên tố lớn p, q; n = p·q (modulus). λ(n) = bội chung nhỏ nhất của (p−1) và (q−1).
Chọn số mũ công khai e nguyên tố cùng nhau với λ(n). Thông dụng nhất là e = 65537 (0x010001, chỉ 17 bit).
Số mũ bí mật d thoả e·d ≡ 1 (mod λ(n)). d có độ dài xấp xỉ n.
Khoá công khai là (n, e), khoá bí mật gồm d và các thành phần hỗ trợ p, q, dP = d mod (p−1), dQ = d mod (q−1), qInv = q⁻¹ mod p.
Mã hoá: c = m^e mod n. Giải mã: m = c^d mod n.
Giải mã dùng CRT (định lý số dư Trung Hoa): tính hai luỹ thừa trên số có nửa độ dài (mod p và mod q) rồi ghép lại, nhanh hơn tính trực tiếp m = c^d mod n.

### 2.2. Đệm OAEP và giới hạn 190 byte

RSA không mã hoá trực tiếp dữ liệu thô mà phải đệm. Chương trình dùng RSA-OAEP với SHA-256: thông điệp được trộn với một "seed" ngẫu nhiên, nên cùng một dữ liệu mã hoá hai lần cho hai bản mã khác nhau. Độ dài dữ liệu tối đa là
> k − 2·hLen − 2, với k = số byte của n, hLen = 32 (SHA-256).
Với khoá 2048 bit: k = 256 nên tối đa 256 − 64 − 2 = 190 byte, khớp với đề bài. Một khoá AES-256 chỉ 32 byte nên mã hoá được trong một lần. Đây là cách dùng RSA điển hình: RSA mã hoá khoá AES (trao đổi khoá), còn AES (bài tập 1) mã hoá dữ liệu lớn.

### 2.3. Định dạng PEM và DER

PEM là văn bản: dòng `-----BEGIN <nhãn>-----`, phần thân là Base64 (64 ký tự mỗi dòng), dòng `-----END <nhãn>-----`. Nhãn cho biết loại dữ liệu: `PUBLIC KEY` (X.509 SubjectPublicKeyInfo), `RSA PUBLIC KEY` (PKCS#1), `PRIVATE KEY` (PKCS#8), `RSA PRIVATE KEY` (PKCS#1).
Giải mã Base64 (mỗi 4 ký tự thành 3 byte) được dữ liệu nhị phân DER, là một dạng mã hoá của cấu trúc ASN.1.
DER là chuỗi các khối TLV: Tag (1 byte) | Length (1 hoặc nhiều byte) | Value.
Tag: `30` SEQUENCE, `02` INTEGER, `03` BIT STRING, `04` OCTET STRING, `05` NULL, `06` OBJECT IDENTIFIER.
Length: nếu nhỏ hơn 0x80 thì chính là độ dài; nếu byte đầu dạng `0x8N` thì N byte kế tiếp mới là độ dài.
INTEGER lưu dạng big-endian có dấu, nếu bit cao nhất của byte đầu là 1 thì phải thêm byte `00` đứng trước để số không bị hiểu là âm.

### 2.4. SHA-256 và hiệu ứng thác lũ

SHA-256 cho giá trị băm 256 bit (32 byte, 64 ký tự hex). Một hàm băm tốt có hiệu ứng thác lũ (avalanche): đổi 1 bit đầu vào làm mỗi bit đầu ra đổi với xác suất 1/2. Số bit khác nhau khi đó có kỳ vọng 256 × 0,5 = 128 bit (50%), độ lệch chuẩn lý thuyết √(256 × 0,5 × 0,5) = 8 bit.

### 2.5. Chữ ký số

Chữ ký số của dữ liệu là kết quả áp dụng RSA bằng khoá bí mật lên giá trị băm của dữ liệu. Ai có khoá công khai đều xác minh được: tính lại giá trị băm và đối chiếu với giá trị khôi phục từ chữ ký. Dữ liệu khác nhau thì giá trị băm khác nhau nên chữ ký khác nhau.

### 3. Thiết kế chương trình

File	Vai trò
`Program.cs`	Menu console, nhập/xuất, điều phối các chức năng
`RsaCore.cs`	Sinh khoá, mô tả và kiểm tra tham số khoá, mã hoá/giải mã OAEP, đo tốc độ, SHA-256, ký và xác minh
`PemInspector.cs`	Đọc PEM, giải mã Base64 sang DER, phân tích cây ASN.1 và giải thích từng nhóm byte
`HashForm.cs`	Cửa sổ WinForms hiển thị SHA-256 của hai dữ liệu A, B và so sánh
`RsaApp.csproj`	File dự án (.NET, bật WinForms)
Các lựa chọn thiết kế chính:
Khoá mặc định 2048 bit, e = 65537; hỗ trợ 1024, 2048, 3072, 4096 bit.
Xuất khoá theo PKCS#8 / X.509 (mặc định) hoặc PKCS#1.
Mã hoá bằng RSA-OAEP SHA-256; bản mã lưu dạng Base64 trong file văn bản.
Dữ liệu gốc cũng được lưu ra file Base64 (`data_goc.b64`) để so sánh với kết quả giải mã.
Đo tốc độ gồm hai nhóm: (a) luỹ thừa modulo "thô" bằng `BigInteger.ModPow` để thấy ảnh hưởng thuần tuý của độ lớn số mũ, (b) hàm `Encrypt`/`Decrypt` của thư viện.
Chữ ký hỗ trợ ba phương pháp: PKCS#1 v1.5, RSA-PSS, băm ngẫu nhiên hoá bằng nonce.

### 4. Kết quả thực nghiệm

### 4.1. Sinh cặp khoá (menu 1)

Sinh cặp khoá RSA 2048 bit: n có 2048 bit, e = 65537.

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/b3824bfb-b1f7-478e-ad44-188454f21276" />

### 4.2. Xuất khoá ra file PEM (menu 2)

Chọn định dạng 1 (PKCS#8 cho khoá bí mật và X.509 cho khoá công khai). Chương trình tạo hai file `rsa_public.pem` và `rsa_private.pem`, đồng thời cảnh báo không đưa file private lên GitHub.

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/c6b0a00a-aa92-4183-bf08-a388cf2f4792" />

### 4.3. Xem file PEM và ý nghĩa các byte (menu 3)

Kết quả với `rsa_public.pem`:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/81df6d60-5367-4b5f-8879-834f98bfeb3d" />

Quan sát từ kết quả:
Nhãn `PUBLIC KEY` cho biết đây là khoá công khai định dạng X.509 SubjectPublicKeyInfo.
Phần thân gồm 392 ký tự Base64 chia 7 dòng, giải mã được 294 byte DER (392 ký tự × 3/4 = 294, thân Base64 không có dấu `=` đệm).
4 ký tự Base64 đầu `MIIB` ứng với 3 byte đầu `30 82 01`.
Byte đầu của file: `30 82 01 22`. `30` là tag SEQUENCE; `82` = 0x80 + 2 nghĩa là độ dài nằm trong 2 byte kế tiếp; `01 22` = 290, nên phần Value dài 290 byte (tổng 4 + 290 = 294 byte).
Cấu trúc DER của một khoá công khai RSA 2048 bit (các byte tiêu đề và vị trí giống nhau với mọi khoá 2048 bit, chỉ phần giá trị của n khác nhau):
Vị trí byte	Byte tiêu đề	Ý nghĩa
0	`30 82 01 22`	SEQUENCE ngoài cùng (SubjectPublicKeyInfo), dài 290 byte
4	`30 0D`	SEQUENCE AlgorithmIdentifier, dài 13 byte
6	`06 09 2A 86 48 86 F7 0D 01 01 01`	OBJECT IDENTIFIER 1.2.840.113549.1.1.1 = `rsaEncryption`
17	`05 00`	NULL (tham số của thuật toán, rỗng)
19	`03 82 01 0F 00`	BIT STRING dài 271 byte; byte `00` = 0 bit đệm; nội dung là khối DER lồng bên trong
24	`30 82 01 0A`	SEQUENCE RSAPublicKey (PKCS#1), dài 266 byte
28	`02 82 01 01 00 ...`	INTEGER n dài 257 byte: byte `00` đệm cho số dương + 256 byte (2048 bit)
289	`02 03 01 00 01`	INTEGER e dài 3 byte = 0x010001 = 65537
Khoá bí mật dạng PKCS#8 (`BEGIN PRIVATE KEY`) có cấu trúc: `SEQUENCE { INTEGER version = 0; SEQUENCE { OID rsaEncryption; NULL }; OCTET STRING { SEQUENCE RSAPrivateKey } }`. Bên trong RSAPrivateKey là 9 số nguyên theo thứ tự: version, n, e, d, p, q, dP, dQ, qInv. Trong đó p, q, dP, dQ, qInv dài khoảng 1024 bit nên mỗi số chiếm khoảng 128 đến 129 byte, còn n và d dài khoảng 256 byte.

### 4.4. Nhập file PEM (menu 4)

Nhập `rsa_private.pem`, chương trình hiển thị n (dạng hex và thập phân), e, d, và các thành phần p, q, dP, dQ, qInv:

<img width="1916" height="1078" alt="image" src="https://github.com/user-attachments/assets/824b9994-8d74-4568-903f-cdf2a78a98f7" />

Kết quả: n dài 2048 bit (256 byte), e = 65537 (hex `010001`, 17 bit), d dài 2047 bit. So sánh độ dài: e chỉ 17 bit còn d xấp xỉ bằng n, đây là cơ sở cho kết quả tốc độ ở mục 5.7.
Chương trình kiểm tra thêm các quan hệ toán học của khoá, cả 5 điều kiện đều ĐÚNG:

<img width="1817" height="1035" alt="image" src="https://github.com/user-attachments/assets/106f4540-8c17-44e3-b9f5-d9b2a283f617" />

Điều kiện	Ý nghĩa
p · q = n	n đúng là tích hai số nguyên tố
e · d ≡ 1 (mod λ(n))	d là nghịch đảo của e, nên giải mã đúng là phép ngược của mã hoá
dP = d mod (p−1), dQ = d mod (q−1)	Các tham số CRT khớp với d
q · qInv ≡ 1 (mod p)	qInv là nghịch đảo của q theo modulo p
Khi nhập file `rsa_public.pem`, chương trình báo đây là khoá công khai và chỉ có n, e (không có d, p, q), tức là chỉ dùng để mã hoá hoặc xác minh chữ ký.

### 4.5. Mã hoá dữ liệu (menu 5)

Dữ liệu cần mã hoá là một khoá AES-256 ngẫu nhiên dài 32 byte (đúng ví dụ trong đề). Với khoá 2048 bit, RSA-OAEP SHA-256 mã hoá được tối đa 190 byte nên 32 byte là hợp lệ. Dữ liệu gốc được lưu vào `data_goc.b64`, bản mã lưu vào `data_mahoa.b64`.

<img width="1881" height="1071" alt="image" src="https://github.com/user-attachments/assets/166f0d72-5661-4179-95ac-6d878c7e30ea" />

Khoá AES-256 (Base64): `vhmNoUWWSm4zJzYTrhmwKPsAzb2ffhuBVm+ddXEngUU=`
Bản mã dài 256 byte, đúng bằng độ dài của n (kết quả RSA luôn nằm trong khoảng 0 đến n−1 nên dài bằng n).
Do đệm OAEP có thành phần ngẫu nhiên, mã hoá lại cùng một dữ liệu sẽ cho bản mã khác.

### 4.6. Giải mã và so sánh với nguồn (menu 6)

Giải mã bằng `rsa_private.pem` từ file `data_mahoa.b64`, rồi so sánh với dữ liệu gốc `data_goc.b64`:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/1e6a7e13-3596-47a7-980e-2bf34daaa44e" />

Chuỗi Base64 giải mã được `vhmNoUWWSm4zJzYTrhmwKPsAzb2ffhuBVm+ddXEngUU=` trùng với khoá AES-256 ở bước mã hoá, và chương trình báo GIỐNG NHAU hoàn toàn theo byte (32 byte). Vậy chu trình mã hoá bằng khoá công khai và giải mã bằng khoá bí mật hoạt động đúng.

### 4.7. Đánh giá tốc độ (menu 7)

Chương trình đo thời gian trung bình của từng phép tính (chạy lặp nhiều lần rồi lấy trung bình):
Luỹ thừa "thô" bằng `BigInteger.ModPow`: m^e mod n (khoá công khai, e bé) và m^d mod n (khoá bí mật, d lớn). Phép đo này chỉ phụ thuộc vào độ lớn số mũ.
Hàm của thư viện: `Encrypt` (dùng khoá công khai) và `Decrypt` (dùng khoá bí mật, có CRT), cùng đệm OAEP.
Kết quả đo trên máy thực hành với RSA-1024:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/7f471028-f918-409c-b777-6b6ece545750" />

Khoá	e (bit)	d (bit)	m^e mod n	m^d mod n	d / e	Encrypt (thư viện)	Decrypt (thư viện)	Decrypt / Encrypt
RSA-1024	17	1023	0,0682 ms	3,2584 ms	chậm gấp 48 lần	0,0043 ms	0,0793 ms	chậm gấp 18,2 lần
Nhận xét:
Khoá công khai (n, e bé) nhanh hơn rất nhiều so với khoá bí mật (n, d lớn). Luỹ thừa với e = 65537 (17 bit) cần khoảng 16 phép bình phương và 1 phép nhân modulo. Luỹ thừa với d (khoảng 1023 bit) cần khoảng 1022 phép bình phương cộng thêm trung bình khoảng 511 phép nhân, tổng cỡ 1500 phép nhân modulo so với 17 phép, tức là nhiều hơn khoảng 90 lần. Ở phép đo thô, chênh lệch thực tế là 48 lần, cùng bậc độ lớn với dự đoán (số lần chênh nhỏ hơn dự đoán có thể do cách cài đặt luỹ thừa của thư viện và chi phí cố định mỗi lần gọi).
Nguyên nhân chậm nằm ở độ lớn của số mũ, không phải ở việc khoá đó là "private". Nếu dùng một số mũ công khai cỡ bằng n thì phép mã hoá cũng chậm như giải mã. Việc chọn e nhỏ cố định là một lựa chọn thiết kế để mã hoá và xác minh chữ ký nhanh.
Với hàm của thư viện, giải mã vẫn chậm hơn mã hoá 18,2 lần dù đã dùng CRT. CRT chia một luỹ thừa trên số 1024 bit thành hai luỹ thừa trên số 512 bit, giúp giải mã nhanh hơn đáng kể so với tính trực tiếp.
Các hàm `Encrypt`/`Decrypt` của thư viện nhanh hơn `BigInteger.ModPow` thô, nhiều khả năng vì thư viện dùng cài đặt mật mã gốc đã được tối ưu; so sánh có ý nghĩa là so sánh trong cùng một cách cài đặt (m^e với m^d; Encrypt với Decrypt).
Hệ quả thực tế: các phép dùng khoá bí mật (giải mã, ký số) tốn tài nguyên hơn nhiều so với các phép dùng khoá công khai (mã hoá, xác minh chữ ký). Vì vậy RSA thường chỉ dùng để mã hoá khoá phiên (như khoá AES 32 byte), còn dữ liệu lớn dùng mã đối xứng AES .

### 4.8. Băm SHA-256 (menu 8)

Giao diện SHA-256 (cửa sổ WinForms). Cửa sổ có hai ô dữ liệu A (gốc) và B (đã sửa), hiển thị giá trị băm của từng ô ở dạng hex (64 ký tự) và dạng nhị phân (256 bit). Ký tự nào của B khác với A được tô đỏ, và phía dưới hiển thị số bit, số ký tự hex khác nhau. Giá trị băm tự cập nhật mỗi khi nội dung ô thay đổi. Có hai nút hỗ trợ: "Chép A → B" và "Đảo 1 bit ký tự cuối của B".

<img width="1908" height="1077" alt="image" src="https://github.com/user-attachments/assets/17fdb426-61ff-4ea1-8d84-44571165f0c6" />

Trong ảnh, A và B giống nhau (hai chuỗi đầu vào giống hệt, khác nhau 0 bit) nên hai giá trị băm hoàn toàn giống nhau (0/256 bit khác nhau, 0/64 ký tự hex khác nhau). Điều này cho thấy hàm băm có tính xác định: cùng đầu vào thì luôn cho cùng giá trị băm.
Thống kê hiệu ứng thác lũ (menu 8 chọn 3). Chương trình lấy một thông điệp ngẫu nhiên 64 byte (512 bit), rồi đảo lần lượt từng bit một (512 lần thử, mỗi lần chỉ đổi đúng 1 bit đầu vào) và đo số bit của giá trị băm bị thay đổi:

<img width="1917" height="1078" alt="image" src="https://github.com/user-attachments/assets/bdfd7502-05a2-4c8e-bb49-8e791a2d83ff" />

Chỉ tiêu	Kết quả
Số lần thử (mỗi lần đảo 1 bit đầu vào)	512
Số bit băm thay đổi, trung bình	128,15 / 256 (50,1%)
Ít nhất / nhiều nhất	101 / 154 bit
Nhận xét: chỉ cần đổi 1 bit ở đầu vào, trung bình khoảng một nửa số bit của giá trị băm thay đổi (128,15, gần với kỳ vọng lý thuyết 128). Giá trị nhỏ nhất (101) và lớn nhất (154) lệch khỏi 128 khoảng 3,4 và 3,3 lần độ lệch chuẩn lý thuyết (8 bit), phù hợp với phân phối nhị thức mà hiệu ứng thác lũ lý tưởng dự đoán. Kết luận: SHA-256 có hiệu ứng thác lũ tốt, giá trị băm của hai dữ liệu chỉ khác nhau rất ít vẫn khác nhau hoàn toàn và không có quy luật, nên không thể suy ra dữ liệu gần giống nhau từ giá trị băm.

### 4.9. Chữ ký số (menu 9)

Chương trình hỗ trợ ba phương pháp ký (xem phần 6) và hai thao tác: ký dữ liệu lưu chữ ký ra file Base64, và xác minh chữ ký bằng khoá công khai. Ví dụ ký chuỗi `son` bằng RSA-PSS và lưu chữ ký vào file:

<img width="1903" height="1078" alt="image" src="https://github.com/user-attachments/assets/fb8bdea2-32ce-46b9-a0de-6a579dc708be" />

Chữ ký dài 256 byte (bằng độ dài n). Menu 9 mục 1 chạy bản demo ký cùng một dữ liệu nhiều lần với cả ba phương pháp và so sánh các chữ ký thu được.

### 5. Đề xuất cho câu 9

Mong muốn: cùng một dữ liệu, cùng một người ký (cùng khoá bí mật) nhưng các lần ký khác nhau cho chữ ký khác nhau.

### 5.1. Vì sao ký thông thường cho chữ ký giống nhau

Với RSA PKCS#1 v1.5, chữ ký là s = EM^d mod n, trong đó EM = `00 01 FF...FF 00 || DigestInfo(H)` được tạo hoàn toàn từ giá trị băm H, không có thành phần ngẫu nhiên. Cùng dữ liệu thì cùng H, cùng EM, cùng khoá thì cùng s. Phép ký là hàm xác định.

### 5.2. Phương pháp đề xuất

Để chữ ký khác nhau mỗi lần ký thì phải đưa một giá trị ngẫu nhiên vào trước phép luỹ thừa bằng khoá bí mật, sao cho người xác minh vẫn kiểm tra được. Hai cách đã cài đặt:
Cách 1 (khuyến nghị): RSA-PSS (Probabilistic Signature Scheme, RFC 8017).
Mỗi lần ký chọn một salt ngẫu nhiên (ở đây dài bằng giá trị băm, 32 byte). Các bước: tính H = SHA-256(dữ liệu); tính M' = `00...00 (8 byte) || H || salt` rồi băm được H'; ghép salt vào khối DB rồi che bằng hàm sinh mặt nạ MGF1 dựa trên H'; được EM = `DB đã che || H' || BC`; chữ ký s = EM^d mod n. Khi xác minh, người nhận tính EM = s^e mod n, tách salt ra từ DB, tính lại H' từ dữ liệu và salt, so sánh với H' trong EM. Salt khác nhau thì EM khác nhau nên s khác nhau, còn salt nằm bên trong chữ ký nên việc xác minh không cần thêm thông tin nào. PSS là chuẩn công nghiệp, có chứng minh an toàn.
Cách 2: băm ngẫu nhiên hoá bằng nonce.
Mỗi lần ký sinh nonce ngẫu nhiên 16 byte, tính H = SHA-256(dữ liệu || nonce), ký H bằng PKCS#1 v1.5, rồi gửi gói `nonce || chữ ký`. Người xác minh tách nonce ra, tính lại SHA-256(dữ liệu || nonce) và kiểm tra chữ ký. Nonce khác nhau thì H khác nhau nên chữ ký khác nhau. Cách này đơn giản, dễ hiểu, nhưng không phải chuẩn nên dùng cho mục đích học tập, còn thực tế nên dùng PSS.

### 5.3. So sánh ba phương pháp

Tiêu chí	PKCS#1 v1.5	RSA-PSS	Băm ngẫu nhiên hoá (nonce)
Thành phần ngẫu nhiên	Không có	Salt 32 byte	Nonce 16 byte
Cùng dữ liệu, cùng khoá, ký nhiều lần	Giống nhau (không đạt mong muốn)	Khác nhau (đạt)	Khác nhau (đạt)
Độ dài chữ ký	256 byte	256 byte	272 byte (16 byte nonce + 256 byte)
Xác minh chữ ký	Dựa trên H	Tách salt từ chữ ký	Tách nonce từ gói
Tính chuẩn hoá	Chuẩn, đã cũ	Chuẩn (RFC 8017), nên dùng	Không chuẩn, mang tính minh hoạ
Với cả ba phương pháp, nếu sửa dù 1 ký tự của dữ liệu hoặc 1 bit của chữ ký thì xác minh đều thất bại, nhờ tính chất thác lũ của SHA-256 (mục 5.8). Chương trình cài đặt trong hàm `RsaCore.SignData` và `RsaCore.VerifyData` (file `RsaCore.cs`), và minh hoạ ở menu 9 mục 1.

### 6. Kết luận

Đã xây dựng ứng dụng .NET thực hiện đủ các chức năng của đề: sinh cặp khoá RSA, xuất và xem file PEM, nhập khoá để lấy n, e, d, p, q, mã hoá và giải mã mảng byte, đo tốc độ, băm SHA-256 với giao diện, và chữ ký số.
Về PEM: file PEM là Base64 của dữ liệu DER (cấu trúc TLV). Với khoá công khai 2048 bit, DER dài 294 byte và chứa n (257 byte có `00` đệm) và e (65537). Khoá bí mật chứa thêm d, p, q, dP, dQ, qInv. Các quan hệ toán học của khoá (p·q = n, e·d ≡ 1 mod λ(n), tham số CRT) đều được kiểm tra đúng.
Về mã hoá: RSA-OAEP SHA-256 với khoá 2048 bit mã hoá được tối đa 190 byte, đủ cho khoá AES-256 (32 byte). Bản mã dài đúng 256 byte, giải mã khôi phục giống nhau 100% theo byte so với dữ liệu gốc.
Về tốc độ: khoá công khai (e = 65537, 17 bit) nhanh hơn khoá bí mật (d cỡ độ dài n) rất nhiều: ở phép đo thô, chậm gấp 48 lần với RSA-1024, và với hàm thư viện giải mã chậm hơn mã hoá 18,2 lần. Nguyên nhân là độ lớn của số mũ. Vì vậy RSA phù hợp để mã hoá khoá phiên nhỏ và xác minh chữ ký nhanh, còn dữ liệu lớn dùng AES.
Về SHA-256: đảo 1 bit đầu vào làm thay đổi trung bình 128,15 trên 256 bit (50,1%) của giá trị băm qua 512 lần thử, đúng với hiệu ứng thác lũ lý tưởng. Do đó chữ ký số của hai dữ liệu khác nhau (dù chỉ khác rất ít) là hoàn toàn khác nhau.
Về chữ ký số: ký PKCS#1 v1.5 là xác định nên cùng dữ liệu cùng khoá cho cùng chữ ký. Để mỗi lần ký ra chữ ký khác nhau, cần thêm thành phần ngẫu nhiên: dùng RSA-PSS (salt ngẫu nhiên, chuẩn) hoặc băm ngẫu nhiên hoá bằng nonce (minh hoạ). Cả hai vẫn xác minh được bằng khoá công khai.
Hạn chế: phần đo tốc độ trong báo cáo này thực hiện với khoá RSA-1024 và mỗi phép đo là trung bình nhiều lần lặp, nên chưa khảo sát ảnh hưởng của độ dài khoá lên tốc độ. Số liệu thời gian phụ thuộc vào cấu hình máy.

### 7. Cấu trúc thư mục

```
Bai2_RSA/
├── Program.cs        # menu console và điều phối
├── RsaCore.cs        # RSA, OAEP, đo tốc độ, SHA-256, chữ ký số
├── PemInspector.cs   # đọc PEM và giải thích byte (ASN.1/DER)
├── HashForm.cs       # giao diện WinForms SHA-256
├── RsaApp.csproj     # file dự án .NET
├── .gitignore        # bỏ qua bin/, obj/, *.pem, *.b64
├── README.md         # báo cáo này
└── images/           # ảnh minh chứng
```
