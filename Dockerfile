# Stage 1: Build the application
# Sử dụng .NET SDK image để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file solution và các file project (.csproj)
# Điều này giúp tận dụng Docker layer caching hiệu quả hơn
# Chỉ restore khi các file .csproj hoặc .sln thay đổi
COPY ["QuantumBandsAI.sln", "./"]
COPY ["QuantumBands.API/QuantumBands.API.csproj", "QuantumBands.API/"]
COPY ["QuantumBands.Application/QuantumBands.Application.csproj", "QuantumBands.Application/"]
COPY ["QuantumBands.Domain/QuantumBands.Domain.csproj", "QuantumBands.Domain/"]
COPY ["QuantumBands.Infrastructure/QuantumBands.Infrastructure.csproj", "QuantumBands.Infrastructure/"]
# Nếu có thêm project, hãy thêm dòng COPY tương ứng ở đây

# Restore dependencies cho API project và các project nó tham chiếu đến
RUN dotnet restore "QuantumBands.API/QuantumBands.API.csproj"

# Sao chép toàn bộ mã nguồn còn lại
COPY . .

# Publish API project
# Chuyển vào thư mục của API project
WORKDIR "/src/QuantumBands.API"
RUN dotnet publish "QuantumBands.API.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Create the runtime image
# Sử dụng ASP.NET runtime image, nhỏ hơn SDK image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Sao chép các file đã publish từ build stage
COPY --from=build /app/publish .

# Mở port mà ứng dụng sẽ lắng nghe bên trong container
# Mặc định cho ASP.NET Core 8 trong Docker là 8080 (HTTP) và 8081 (HTTPS)
# Kiểm tra file Program.cs hoặc launchSettings.json của bạn để biết port chính xác
# Nếu bạn cấu hình Kestrel lắng nghe trên port 80, hãy EXPOSE 80
# Các image .NET chính thức thường đặt ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Lệnh để chạy ứng dụng khi container khởi động
ENTRYPOINT ["dotnet", "QuantumBands.API.dll"]
