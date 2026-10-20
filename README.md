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
