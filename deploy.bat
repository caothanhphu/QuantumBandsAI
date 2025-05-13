@echo OFF
ECHO Starting CI/CD Deployment Process...

REM Bước 1: Kiểm tra Docker (Tùy chọn, vì workflow đã có thể làm)
ECHO Verifying Docker installation...
docker --version
IF %ERRORLEVEL% NEQ 0 (
    ECHO Docker command failed. Exiting.
    EXIT /B 1
)

REM Bước 2: Build Docker image
ECHO Building Docker image...
docker build -t quantumbands-api-image -f Dockerfile .
IF %ERRORLEVEL% NEQ 0 (
    ECHO Docker build failed. Exiting.
    EXIT /B 1
)

REM Bước 3: Dừng và xóa container cũ
ECHO Stopping and Removing existing container...
SET "containerName=quantumbands-api-container"
FOR /F "tokens=*" %%i IN ('docker ps -a -q --filter "name=%containerName%"') DO (
    ECHO Stopping and removing existing container: %containerName% (%%i)
    docker stop %containerName%
    docker rm %containerName%
)
IF NOT ERRORLEVEL 1 (
    ECHO No existing container named %containerName% found or it was successfully removed.
)


REM Bước 4: Chạy container mới
ECHO Running new Docker container...
REM Các biến môi trường DB_PASSWORD và JWT_SECRET sẽ được truyền từ GitHub Actions workflow
REM và được runner thiết lập thành biến môi trường cho tiến trình chạy file batch này.
docker run -d ^
    -p 6020:8080 ^
    --name %containerName% ^
    -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=FinixAI;User ID=finix;Password=%DB_PASSWORD%;TrustServerCertificate=True;MultipleActiveResultSets=true" ^
    -e JwtSettings__Secret="%JWT_SECRET%" ^
    -e ASPNETCORE_ENVIRONMENT=Docker ^
    quantumbands-api-image

IF %ERRORLEVEL% NEQ 0 (
    ECHO Failed to run new Docker container. Exiting.
    EXIT /B 1
)

ECHO New container '%containerName%' started. Access it on host at http://localhost:6020
ECHO Deployment process completed successfully.
EXIT /B 0