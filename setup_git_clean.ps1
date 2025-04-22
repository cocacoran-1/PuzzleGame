# PowerShell 스크립트: setup_git_clean.ps1

Write-Host "✅ Unity Git 정리 스크립트를 시작합니다..." -ForegroundColor Cyan

# 1. GitHub 원격 저장소 주소 받기
$repoUrl = Read-Host "📌 GitHub 저장소 주소를 입력하세요 (예: https://github.com/user/repo.git)"

# 2. 기존 Git 초기화
if (Test-Path ".git") {
    Remove-Item -Recurse -Force .git
    Write-Host "🧹 기존 Git 기록 제거 완료"
}

# 3. .gitignore 생성
@"
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
UserSettings/
Packages/PackageCache/
*.csproj
*.sln
*.user
*.unityproj
*.pidb
*.suo
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
.vscode/
*.apk
*.unitypackage
"@ | Out-File -Encoding UTF8 .gitignore

Write-Host "📄 .gitignore 생성 완료"

# 4. Git 초기화 및 첫 커밋
git init
git add .
git commit -m "Initial commit: Clean Unity project with proper .gitignore"
Write-Host "✅ Git 초기화 및 커밋 완료"

# 5. 원격 연결 및 강제 푸시
git remote add origin $repoUrl
git branch -M main
git push -u origin main --force
Write-Host "🚀 GitHub에 성공적으로 푸시되었습니다!" -ForegroundColor Green

