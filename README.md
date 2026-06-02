# 🚀 Hệ Thống Quản Lý Nhân Sự Thông Minh - Atelier HRM

<p align="center">
  <img src="https://skillicons.dev/icons?i=dotnet,cs,postgres,supabase,docker,tailwind,bootstrap,js,html,css,git,github,visualstudio,vscode" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/ASP.NET_Core-Web_API-blue?style=for-the-badge&logo=.net">
  <img src="https://img.shields.io/badge/Supabase-Database-3ECF8E?style=for-the-badge&logo=supabase">
  <img src="https://img.shields.io/badge/PostgreSQL-Database-336791?style=for-the-badge&logo=postgresql">
  <img src="https://img.shields.io/badge/Docker-Container-2496ED?style=for-the-badge&logo=docker">
  <img src="https://img.shields.io/badge/Gemini-AI-orange?style=for-the-badge&logo=google">
  <img src="https://img.shields.io/badge/TailwindCSS-UI-38BDF8?style=for-the-badge&logo=tailwindcss">
</p>

---

# 📖 Giới thiệu

Atelier HRM là hệ thống quản lý nhân sự hiện đại được phát triển nhằm hỗ trợ doanh nghiệp tối ưu hóa quy trình quản lý nhân viên, chấm công và giao tiếp nội bộ.

Ngoài các chức năng quản lý truyền thống, hệ thống còn tích hợp AI hỗ trợ nhân sự và hệ thống chat nội bộ realtime.

Dự án được xây dựng theo định hướng ứng dụng AI vào quản lý doanh nghiệp, giúp nâng cao trải nghiệm người dùng và tự động hóa quy trình nội bộ.

---

# 🛠 Công Nghệ Sử Dụng

## 🔹 Backend
- ASP.NET Core Web API
- Entity Framework Core
- RESTful API
- JWT Authentication

## 🎨 Frontend
- HTML5 / CSS3
- JavaScript
- Bootstrap
- Tailwind CSS
- Responsive Design
- Component-based UI

## 🗄 Database
- PostgreSQL
- Supabase Cloud Database
- Vector Database

## 🐳 DevOps & Deployment
- Docker
- Docker Compose
- Cloud Deployment

## 🤖 AI & Machine Learning
- Gemini API
- Model Chat AI: `gemini-2.5-flash`
- Model Embedding AI: `gemini-embedding-001`
- Vector Search
- Semantic Search
- Face Embedding Recognition

---

# ✨ Các Chức Năng Chính

## 👨‍💼 Quản Lý Nhân Sự
- Quản lý hồ sơ nhân viên
- Quản lý phòng ban
- Quản lý chức vụ
- Quản lý tài khoản
- Phân quyền người dùng
- Theo dõi thông tin nhân sự

---

## 🧠 Chấm Công Thông Minh
- Check-in / Check-out
- Chấm công bằng FaceID
- Lưu vector khuôn mặt bằng AI Embedding
- Đối chiếu khuôn mặt bằng vector similarity
- Theo dõi lịch sử chấm công
- Thống kê thời gian làm việc

### ⚙ Công nghệ sử dụng
- AI Face Embedding
- Vector Database
- Gemini Embedding Model

---

## 💬 Chat Nội Bộ Realtime
- Nhắn tin giữa nhân viên
- Tạo phòng chat nội bộ
- Gửi hình ảnh / tài liệu
- Realtime communication
- Hỗ trợ trao đổi công việc nhanh chóng

---

# 🤖 AI Assistant Hỗ Trợ Nhân Sự

Hệ thống tích hợp AI Assistant hỗ trợ nhân viên tra cứu thông tin nội bộ công ty.

## 🔍 Chức năng AI
- Trả lời quy định công ty
- Giải đáp nội quy
- Hỗ trợ tra cứu thông tin nhân sự
- Tìm kiếm dữ liệu thông minh bằng AI Embedding
- Truy xuất dữ liệu theo ngữ nghĩa
- Hỗ trợ nhân viên tự động

---

# 🧩 Thiết Lập Vai Trò AI

```csharp
string systemPrompt = $@"Bạn là trợ lý Nhân sự (HR) thân thiện và chuyên nghiệp của công ty Atelier.
Thời gian hệ thống hiện tại (Bây giờ): {thoiGianHienTai}

Nhiệm vụ của bạn là hỗ trợ nhân viên dựa trên 2 quy tắc phân định nghiêm ngặt sau:

1. ĐỐI VỚI CÁC CÂU HỎI VỀ QUY ĐỊNH, CHÍNH SÁCH NỘI BỘ CỦA CÔNG TY ATELIER:
- Bạn BẮT BUỘC phải dựa vào khối thông tin 'THÔNG TIN QUY ĐỊNH NỘI BỘ' được cung cấp dưới đây để trả lời.
- Tuyệt đối KHÔNG BỊA ĐẶT ra các điều luật hoặc chính sách không xuất hiện trong tài liệu này.
- Nếu tài liệu nội bộ không nhắc đến, hãy trả lời chính xác câu sau: 'Tôi chưa tìm thấy thông tin này trong quy định hiện hành, vui lòng liên hệ phòng HR để được giải đáp'.

2. ĐỐI VỚI CÁC CÂU HỎI VỀ KIẾN THỨC BÊN NGOÀI, THỜI SỰ, TIN TỨC, GIẢI TRÍ HOẶC CHÀO HỎI TÁN GẪU:
- Khi câu hỏi của nhân viên nằm ngoài phạm vi quy định của công ty, bạn được phép tự do sử dụng công cụ Google Search (đã được tích hợp sẵn) để tra cứu dữ liệu Internet và cập nhật câu trả lời mới nhất, chính xác nhất theo mốc thời gian thực hiện tại.
- Trả lời một cách thông minh, hữu ích, tự nhiên. Giữ vững phong thái của một người làm HR (lịch sự, hòa nhã, cởi mở).
- Tuyệt đối KHÔNG áp dụng câu từ chối nội bộ vào các câu hỏi kiến thức xã hội thông thường này.

THÔNG TIN QUY ĐỊNH NỘI BỘ:
{thongTinHoTro}

CÂU HỎI NHÂN VIÊN:
{request.Message}";

```

---

# 📚 AI Training Với Embedding

Hệ thống sử dụng mô hình embedding để xây dựng thư viện tri thức nội bộ.

## ⚡ Quy trình hoạt động
1. Đọc tài liệu nội bộ công ty
2. Chuyển dữ liệu thành vector embedding
3. Lưu vector vào database Supabase
4. Tìm kiếm semantic search theo ngữ nghĩa
5. AI truy xuất dữ liệu phù hợp trước khi trả lời

---

# 🏗 Kiến Trúc Hệ Thống

```text
Frontend Client
       ↓
ASP.NET Core API
       ↓
Authentication JWT
       ↓
Business Service Layer
       ↓
Gemini AI Service
       ↓
Supabase PostgreSQL + Vector Database
```
<img width="1024" height="559" alt="2f00e1e9-a256-408e-ad4c-c354c40f4b5f" src="https://github.com/user-attachments/assets/97796d9f-fe3f-4546-b51d-34c1cde902b5" />
---

# 🗃 Database

Database được deploy trên nền tảng Supabase Cloud:

- PostgreSQL Database
- Cloud Hosted
- Realtime Support
- Vector Storage

---

# 🎨 Giao Diện Hệ Thống

Hệ thống được thiết kế với giao diện hiện đại sử dụng:

- Tailwind CSS cho custom UI
- Bootstrap hỗ trợ responsive layout
- Responsive Design
- Hỗ trợ đa thiết bị
- UI hiện đại và tối ưu trải nghiệm người dùng
- Giao diện realtime cho chat nội bộ

---

# 🌟 Các Điểm Nổi Bật

- AI HR Assistant tích hợp Gemini
- Chấm công bằng FaceID AI
- Lưu trữ vector khuôn mặt
- Semantic Search bằng Embedding
- Chat nội bộ realtime
- Tailwind CSS UI hiện đại
- Database Cloud Supabase
- Hệ thống phân quyền
- REST API Architecture
- Docker Deployment
- Khả năng mở rộng cao

---

# 🧠 Mô Hình AI Sử Dụng

| Chức năng | Model |
|---|---|
| AI Chat | gemini-2.5-flash |
| AI Embedding | gemini-embedding-001 |

---

# 🎯 Mục Tiêu Dự Án

- Tự động hóa quản lý nhân sự
- Tăng hiệu quả vận hành nội bộ
- Ứng dụng AI vào doanh nghiệp
- Xây dựng hệ thống HRM hiện đại
- Hỗ trợ giao tiếp và quản lý tập trung

---

# 🚀 Tương Lai Phát Triển

- AI đánh giá hiệu suất nhân viên
- Dashboard thống kê AI
- Voice Assistant nội bộ
- AI phân tích dữ liệu nhân sự
- Mobile App
- Notification realtime
- OCR tài liệu nhân sự

---

# 👨‍💻 Tác Giả

<p align="center">
  <img src="https://img.shields.io/badge/Developer-Huy_Gia-blue?style=for-the-badge&logo=github">
</p>

## 📌 Thông Tin
- 👤 Developer: **Huy Gia**
- 💻 Role: Fullstack Developer
- 🚀 Chuyên ngành: AI Integration & Web Development
- 🧠 Hướng nghiên cứu:
  - AI Integration
  - Vector Database
  - Face Recognition
  - Semantic Search
  - Enterprise HRM System

---

# ⭐ Support

Nếu bạn thấy dự án hữu ích, hãy ⭐ repository để hỗ trợ dự án phát triển hơn nữa.
