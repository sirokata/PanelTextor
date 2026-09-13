param([switch]$NoRestore)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'PanelTextor.csproj'
$version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version
$release = Join-Path $repo 'release'
$stage = Join-Path $release ('.stage-' + [guid]::NewGuid().ToString('N'))
$app = Join-Path $stage 'app'
New-Item -ItemType Directory -Path $app -Force | Out-Null
$arguments = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', $app)
if ($NoRestore) { $arguments += '--no-restore' }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$documents = @('README.md','USAGE.md','LICENSE','TERMS.md','THIRD_PARTY_NOTICES.md')
foreach ($base in @('README','USAGE','TERMS')) {
 foreach ($language in @('en','zh-CN','ko')) { $documents += "$base.$language.md" }
}
foreach ($file in $documents) {
 Copy-Item -LiteralPath (Join-Path $repo $file) -Destination $app -Force
}
foreach ($directory in @('licenses','Assets')) { Copy-Item -LiteralPath (Join-Path $repo $directory) -Destination $app -Recurse -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
 $zip = Join-Path $release ('PanelTextor-' + $version + '-win-x64.zip')
 if (Test-Path -LiteralPath $zip) { throw "Artifact already exists: $zip. Keep it or move it before rebuilding this version." }
 [System.IO.Compression.ZipFile]::CreateFromDirectory($app, $zip)
 $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
 ($hash + '  ' + (Split-Path -Leaf $zip)) | Set-Content -LiteralPath ($zip + '.sha256') -Encoding ASCII
 Write-Output $zip
