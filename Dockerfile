# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file solution và các file project (.csproj)
COPY ["QuantumBandsAI.sln", "./"]
COPY ["QuantumBands.API/QuantumBands.API.csproj", "QuantumBands.API/"]
COPY ["QuantumBands.Application/QuantumBands.Application.csproj", "QuantumBands.Application/"]
COPY ["QuantumBands.Domain/QuantumBands.Domain.csproj", "QuantumBands.Domain/"]
COPY ["QuantumBands.Infrastructure/QuantumBands.Infrastructure.csproj", "QuantumBands.Infrastructure/"]

RUN dotnet restore "QuantumBands.API/QuantumBands.API.csproj"

# Sao chép toàn bộ mã nguồn còn lại
COPY . .

# Publish API project
WORKDIR "/src/QuantumBands.API"
RUN dotnet publish "QuantumBands.API.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# --- THÊM CÁC LỆNH SAU ĐỂ CÀI ĐẶT CÔNG CỤ MẠNG ---
# Chuyển sang user root để có quyền cài đặt packages
USER root

# Cập nhật danh sách package và cài đặt các công cụ mạng
# Thêm -qq để giảm output, thêm dọn dẹp apt cache
RUN apt-get update -qq && \
    apt-get install -y --no-install-recommends \
        iputils-ping \
        telnet \
        procps \
        net-tools \
        dnsutils \
    # Kiểm tra xem ping đã được cài đặt và có trong PATH của root chưa
    && which ping \
    && nslookup -version \
    # Dọn dẹp apt cache để giảm kích thước image
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# iputils-ping: cho lệnh ping
# telnet: cho lệnh telnet để kiểm tra port
# procps: cho các công cụ như ps (xem tiến trình)
# net-tools: cho các công cụ như netstat
# dnsutils: cho các công cụ như nslookup, dig (kiểm tra DNS)

# Chuyển về user mặc định 'app' của image aspnet sau khi cài đặt xong
# User 'app' được tạo sẵn trong image base mcr.microsoft.com/dotnet/aspnet:8.0
USER app
# --- KẾT THÚC CÀI ĐẶT CÔNG CỤ MẠNG ---

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
ENTRYPOINT ["dotnet", "QuantumBands.API.dll"]
