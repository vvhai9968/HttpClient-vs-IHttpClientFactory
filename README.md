# So sánh HttpClient và IHttpClientFactory trong ASP.NET Core

Dự án này minh họa sự khác biệt quan trọng giữa việc sử dụng `new HttpClient()` trực tiếp và sử dụng `IHttpClientFactory` để quản lý các yêu cầu HTTP trong một ứng dụng ASP.NET Core. Mục tiêu chính là để chỉ ra vấn đề **cạn kiệt socket (socket exhaustion)** khi `HttpClient` được sử dụng không đúng cách.

## 🧐 Vấn đề với `new HttpClient()`

Một anti-pattern phổ biến là khởi tạo và hủy `HttpClient` cho mỗi yêu cầu. Mặc dù `HttpClient` triển khai `IDisposable`, việc tạo mới nó liên tục sẽ gây ra vấn đề nghiêm trọng.

Khi một instance `HttpClient` bị hủy, socket bên dưới không được giải phóng ngay lập tức. Thay vào đó, nó chuyển sang trạng thái `TIME_WAIT` trong một khoảng thời gian để đảm bảo tất cả dữ liệu đã được truyền đi. Nếu ứng dụng của bạn thực hiện một số lượng lớn các yêu cầu ra bên ngoài trong một thời gian ngắn, bạn sẽ nhanh chóng tích lũy một số lượng lớn các socket ở trạng thái `TIME_WAIT`. Điều này làm cạn kiệt các port có sẵn của hệ điều hành, dẫn đến `SocketException` và làm ứng dụng của bạn không thể tạo kết nối mới.

Trong dự án này, endpoint `/start-httpClient` mô phỏng vấn đề này:

```csharp
app.MapGet("/start-httpClient", async () =>
{
    // ...
    for (var i = 0; i < 100000; i++)
    {
        // ANTI-PATTERN: Tạo một HttpClient mới trong mỗi lần lặp
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("http://localhost:5235/weatherforecast");
        // ...
    }
    // ...
});
```

## ✨ Giải pháp: `IHttpClientFactory`

`IHttpClientFactory` được giới thiệu trong ASP.NET Core 2.1 để giải quyết các vấn đề liên quan đến `HttpClient`. Nó hoạt động như một "nhà máy" quản lý vòng đời của các `HttpMessageHandler` (thành phần xử lý các yêu cầu HTTP).

**Cách `IHttpClientFactory` hoạt động:**

1. **Quản lý pool (Pooling):** `IHttpClientFactory` quản lý một pool các `HttpMessageHandler`. Khi bạn yêu cầu một `HttpClient`, nó sẽ tái sử dụng một `HttpMessageHandler` từ pool này.
2. **Tái sử dụng kết nối:** Bằng cách tái sử dụng `HttpMessageHandler`, các kết nối TCP bên dưới cũng được tái sử dụng, tránh việc phải tạo socket mới cho mỗi yêu cầu.
3. **Tránh cạn kiệt socket:** Điều này ngăn chặn việc tạo ra hàng ngàn socket ở trạng thái `TIME_WAIT`, giúp ứng dụng hoạt động ổn định và hiệu quả.

Endpoint `/start-IHttpClientFactory` sử dụng cách tiếp cận đúng đắn:

```csharp
app.MapGet("/start-IHttpClientFactory", async (IHttpClientFactory httpClientFactory) =>
{
    // ...
    for (var i = 0; i < 100; i++)
    {
        // CÁCH LÀM ĐÚNG: Lấy một HttpClient từ factory
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync("http://localhost:5235/weatherforecast");
        // ...
    }
    // ...
});

```

## 🚀 Cách chạy Demo

Làm theo các bước sau để thấy sự khác biệt.

### Bước 1: Chạy API Demo

1. Mở project `API demo`.
2. Chạy project. Một API sẽ khởi động và lắng nghe trên `http://localhost:5235`.

### Bước 2: Chạy Project Compare

1. Mở project `compare`.
2. Chạy project này.

### Bước 3: Thử nghiệm với `new HttpClient()` (Cách làm sai)

1. Sử dụng trình duyệt hoặc một công cụ API (như Postman) để gửi yêu cầu `GET` đến endpoint: `/start-httpClient`.
2. Sau một lúc, bạn sẽ thấy ứng dụng so sánh bị lỗi và trả về một `SocketException`.
3. Mở **Command Prompt (cmd)** và chạy lệnh sau để kiểm tra các kết nối mạng:
    
    ```
    netstat -ano | findstr 5235
    ```
    
4. **Kết quả:** Bạn sẽ thấy một danh sách rất dài các kết nối đến port `5235` đang ở trạng thái `TIME_WAIT`, cho thấy các socket không được tái sử dụng.
<img width="687" height="262" alt="image" src="https://github.com/user-attachments/assets/1d09b757-1434-489b-ba59-95814fb56411" />


### Bước 4: Thử nghiệm với `IHttpClientFactory` (Cách làm đúng)

1. Gửi yêu cầu `GET` đến endpoint: `/start-IHttpClientFactory`.
2. Yêu cầu sẽ hoàn thành thành công mà không có lỗi.
3. Chạy lại lệnh `netstat` trong cmd:
    
    ```
    netstat -ano | findstr 5235
    ```
    
4. **Kết quả:** Bạn sẽ thấy chỉ có một vài kết nối ở trạng thái `ESTABLISHED`. `IHttpClientFactory` đã quản lý và tái sử dụng các kết nối một cách hiệu quả.
<img width="710" height="120" alt="image" src="https://github.com/user-attachments/assets/d0a7e3b9-cdf3-4cec-bf52-a1da8c4c0b14" />
