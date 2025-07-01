@echo OFF
ECHO Starting CI/CD Deployment Process...

REM Kiểm tra biến môi trường để bỏ qua Docker check nếu cần
IF "%SKIP_DOCKER_CHECK%"=="true" (
    ECHO ⚠️ SKIP_DOCKER_CHECK is enabled. Skipping Docker daemon verification.
    ECHO WARNING: This may cause deployment to fail if Docker is not running.
    GOTO :SKIP_DOCKER_VERIFICATION
)

REM Kiểm tra biến môi trường để force Docker ready nếu biết Docker đang chạy
IF "%FORCE_DOCKER_READY%"=="true" (
    ECHO ⚠️ FORCE_DOCKER_READY is enabled. Assuming Docker is ready.
    ECHO Proceeding with deployment...
    GOTO :DOCKER_READY
)

REM Bước 1: Kiểm tra và khởi động Docker daemon
ECHO Checking Docker daemon status...

REM Function để kiểm tra Docker daemon
:CHECK_DOCKER
ECHO Checking Docker daemon connectivity...

REM Method 1: Check với docker version
docker version --format "{{.Server.Version}}" >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    ECHO ✅ Docker daemon is running (docker version succeeded)
    GOTO :DOCKER_READY
)

REM Method 2: Check với docker info
docker info >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    ECHO ✅ Docker daemon is running (docker info succeeded)
    GOTO :DOCKER_READY
)

REM Method 3: Check với docker ps
docker ps >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    ECHO ✅ Docker daemon is running (docker ps succeeded)
    GOTO :DOCKER_READY
)

REM Method 4: Parse docker info output để check nếu có "Server Version"
FOR /F "tokens=*" %%i IN ('docker info 2^>nul ^| findstr "Server Version"') DO (
    IF NOT "%%i"=="" (
        ECHO ✅ Docker daemon is running (found Server Version in docker info)
        GOTO :DOCKER_READY
    )
)

REM Method 5: Fallback - check if docker command itself is available and try simple command
docker --version >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    ECHO ⚠️ Docker CLI is available, attempting to proceed despite daemon check failures...
    ECHO This might work if Docker is running but has warnings/context issues.
    GOTO :DOCKER_READY
)

ECHO ⚠️ Docker daemon is not running or not accessible. Attempting to start Docker Desktop...

REM Kiểm tra xem Docker Desktop có được cài đặt không
IF EXIST "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" (
    ECHO Starting Docker Desktop...
    START "" "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
) ELSE IF EXIST "%ProgramFiles(x86)%\Docker\Docker\Docker Desktop.exe" (
    ECHO Starting Docker Desktop...
    START "" "%ProgramFiles(x86)%\Docker\Docker\Docker Desktop.exe"
) ELSE (
    ECHO ERROR: Docker Desktop not found in Program Files.
    ECHO Please install Docker Desktop manually from: https://www.docker.com/products/docker-desktop
    EXIT /B 1
)

REM Đợi Docker daemon khởi động với retry logic
SET RETRY_COUNT=0
SET MAX_RETRIES=12
ECHO Waiting for Docker daemon to start (max %MAX_RETRIES% retries, ~2 minutes)...

:RETRY_DOCKER
ECHO Checking Docker daemon (attempt %RETRY_COUNT%/%MAX_RETRIES%)...

REM Thử docker version trước
docker version >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    ECHO ✅ Docker daemon is now running (detected via docker version)
    GOTO :DOCKER_READY
)

REM Thử docker info
docker info >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    ECHO ✅ Docker daemon is now running (detected via docker info)
    GOTO :DOCKER_READY
)

REM Thử docker ps để check daemon
docker ps >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    ECHO ✅ Docker daemon is now running (detected via docker ps)
    GOTO :DOCKER_READY
)

SET /A RETRY_COUNT=%RETRY_COUNT%+1
ECHO Attempt %RETRY_COUNT%/%MAX_RETRIES%: Docker daemon still not responding...

IF %RETRY_COUNT% GEQ %MAX_RETRIES% (
    ECHO ❌ ERROR: Docker daemon failed to start after %MAX_RETRIES% attempts.
    ECHO.
    ECHO === DEBUGGING INFORMATION ===
    ECHO Trying docker info to get more details:
    docker info 2>&1
    ECHO.
    ECHO Trying docker version to check connectivity:
    docker version 2>&1
    ECHO.
    ECHO Please try the following:
    ECHO 1. Start Docker Desktop manually
    ECHO 2. Check if virtualization is enabled in BIOS
    ECHO 3. Ensure Hyper-V or WSL 2 is properly configured
    ECHO 4. Run 'docker info' manually to diagnose the issue
    ECHO 5. Check if Docker context is correct with 'docker context ls'
    EXIT /B 1
)

ECHO Waiting 10 seconds before next retry...
timeout /t 10 >nul 2>&1
GOTO :RETRY_DOCKER

:DOCKER_READY
ECHO Docker daemon is ready for deployment.

:SKIP_DOCKER_VERIFICATION

REM Bước 1.1: Kiểm tra phiên bản Docker (nếu không skip Docker check)
IF NOT "%SKIP_DOCKER_CHECK%"=="true" (
    ECHO Verifying Docker installation...
    docker --version
    IF %ERRORLEVEL% NEQ 0 (
        ECHO Docker command failed. Exiting.
        EXIT /B 1
    )
) ELSE (
    ECHO Skipping Docker version check.
)

REM Bước 2: Build Docker image
ECHO Building Docker image...
docker build -t quantumbands-api-image -f Dockerfile .
IF %ERRORLEVEL% NEQ 0 (
    ECHO ERROR: Docker build failed. 
    ECHO Please check the Dockerfile and ensure all required files are present.
    ECHO You can run 'docker build -t quantumbands-api-image -f Dockerfile .' manually to see detailed error.
    EXIT /B 1
)

REM Bước 3: Deploy to Docker Hub
ECHO Deploying image to Docker Hub...

REM Kiểm tra biến môi trường Docker Hub
IF "%DOCKER_HUB_USERNAME%"=="" (
    ECHO DOCKER_HUB_USERNAME environment variable is not set. Skipping Docker Hub deployment.
    GOTO :SKIP_DOCKER_HUB
)

IF "%DOCKER_HUB_TOKEN%"=="" (
    ECHO DOCKER_HUB_TOKEN environment variable is not set. Skipping Docker Hub deployment.
    GOTO :SKIP_DOCKER_HUB
)

REM Login to Docker Hub
ECHO Logging in to Docker Hub...
echo %DOCKER_HUB_TOKEN% | docker login --username %DOCKER_HUB_USERNAME% --password-stdin
IF %ERRORLEVEL% NEQ 0 (
    ECHO Docker Hub login failed. Exiting.
    EXIT /B 1
)

REM Tag image với Docker Hub repository
SET "dockerHubRepo=%DOCKER_HUB_USERNAME%/quantumbands-api"
ECHO Tagging image for Docker Hub: %dockerHubRepo%
docker tag quantumbands-api-image %dockerHubRepo%:latest
IF %ERRORLEVEL% NEQ 0 (
    ECHO Failed to tag image for Docker Hub. Exiting.
    EXIT /B 1
)

REM Tag với version nếu có
IF NOT "%VERSION%"=="" (
    ECHO Tagging image with version: %dockerHubRepo%:%VERSION%
    docker tag quantumbands-api-image %dockerHubRepo%:%VERSION%
)

REM Push image to Docker Hub
ECHO Pushing image to Docker Hub: %dockerHubRepo%:latest
docker push %dockerHubRepo%:latest
IF %ERRORLEVEL% NEQ 0 (
    ECHO Failed to push image to Docker Hub. Exiting.
    EXIT /B 1
)

REM Push version tag nếu có
IF NOT "%VERSION%"=="" (
    ECHO Pushing versioned image to Docker Hub: %dockerHubRepo%:%VERSION%
    docker push %dockerHubRepo%:%VERSION%
    IF %ERRORLEVEL% NEQ 0 (
        ECHO Failed to push versioned image to Docker Hub. Exiting.
        EXIT /B 1
    )
)

ECHO Successfully deployed image to Docker Hub: %dockerHubRepo%

:SKIP_DOCKER_HUB

REM Bước 4: Dừng và xóa container cũ
ECHO Stopping and Removing existing container...
SET "containerName=quantumbands-api-container"
FOR /F "tokens=*" %%i IN ('docker ps -a -q --filter "name=%containerName%" 2^>nul') DO (
    ECHO Stopping and removing existing container: %containerName% (%%i)
    docker stop %containerName% >nul 2>&1
    docker rm %containerName% >nul 2>&1
)
IF NOT ERRORLEVEL 1 (
    ECHO No existing container named %containerName% found or it was successfully removed.
)

REM Bước 5: Chạy container mới
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
    ECHO ERROR: Failed to run new Docker container.
    ECHO Please check if port 6020 is already in use or if there are other issues.
    ECHO You can run the docker command manually to see detailed error.
    EXIT /B 1
)

ECHO New container '%containerName%' started. Access it on host at http://localhost:6020

REM Bước 6: Kiểm tra container có chạy thành công không
ECHO Verifying container is running...
timeout /t 5 >nul 2>&1
docker ps --filter "name=%containerName%" --filter "status=running" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
IF %ERRORLEVEL% NEQ 0 (
    ECHO WARNING: Unable to verify container status. Please check manually with 'docker ps'
) ELSE (
    ECHO Container verification completed.
)

ECHO Deployment process completed successfully.
EXIT /B 0