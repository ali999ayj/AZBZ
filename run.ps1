param(
    [string]$Urls = "http://localhost:5000"
)

$projectPath = Join-Path $PSScriptRoot "AttendanceApp.csproj"
if (-not (Test-Path $projectPath)) {
    Write-Error "لم يتم العثور على AttendanceApp.csproj في $PSScriptRoot. تأكد من فك الضغط في مجلد يحتوي على ملف المشروع."
    exit 1
}

Write-Host "تشغيل التطبيق باستخدام dotnet run --project $projectPath --urls $Urls"
dotnet run --project $projectPath --urls $Urls
