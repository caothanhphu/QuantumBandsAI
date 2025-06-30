@echo OFF
ECHO Starting CI/CD Deployment Process...

REM Set local environment variables for testing
SET JWT_SECRET=GenerateAReallyStsadfasdrongAndLongSecretKeyHere_KeepItSecret_KeepItSafe_AtLeast32Chars
SET DB_PASSWORD=YourStrongP@sswordHere_123!

REM Bước 1: Kiểm tra Docker daemon có đang chạy không
ECHO Checking if Docker daemon is running...
docker info >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    ECHO ERROR: Docker daemon is not running or not accessible.
    ECHO Please ensure Docker Desktop is installed and running.
    ECHO You can check by running 'docker info' manually.
    EXIT /B 1
)

REM Bước 1.1: Kiểm tra phiên bản Docker
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