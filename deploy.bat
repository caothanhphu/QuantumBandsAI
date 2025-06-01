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
FOR /F "tokens=*" %%i IN ('docker ps -a -q --filter "name=%containerName%"') DO (
    ECHO Stopping and removing existing container: %containerName% (%%i)
    docker stop %containerName%
    docker rm %containerName%
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
    ECHO Failed to run new Docker container. Exiting.
    EXIT /B 1
)

ECHO New container '%containerName%' started. Access it on host at http://localhost:6020
ECHO Deployment process completed successfully.
EXIT /B 0