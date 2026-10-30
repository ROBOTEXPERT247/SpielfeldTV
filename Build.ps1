param([switch]$FullSetup)
$ErrorActionPreference='Stop'
$version='0.8.2'
$root=$PSScriptRoot
$build=Join-Path $root 'build'
$stage=Join-Path $build 'payload\SpielfeldTV'
if(Test-Path $stage){
 $resolvedStage=[IO.Path]::GetFullPath($stage);
 if($resolvedStage -ne [IO.Path]::GetFullPath((Join-Path $root 'build\payload\SpielfeldTV'))){throw 'Unexpected stage path'}
 Remove-Item -LiteralPath $resolvedStage -Recurse -Force
}
New-Item -ItemType Directory -Force $stage | Out-Null
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /platform:x64 /optimize+ "/win32manifest:$root\SpielfeldTV.exe.manifest" "/out:$stage\SpielfeldTV.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.Linq.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll "/resource:$root\Bunker73-logo.png,branding.logo" "$root\SpielfeldTV.cs" "$root\Features.cs"
if($LASTEXITCODE){throw 'App build failed'}
Copy-Item "$root\SpielfeldTV.exe.config","$root\LICENSE","$root\BRANDING.txt" $stage -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $build 'update.zip'
if(Test-Path $zip){Remove-Item -LiteralPath $zip}
[IO.Compression.ZipFile]::CreateFromDirectory((Join-Path $build 'payload'),$zip)
$flag=Join-Path $build 'patch.flag';Set-Content $flag 'patch'
& $csc /nologo /target:winexe /platform:x64 /optimize+ "/out:$build\SpielfeldTV-Update-$version.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/resource:$zip,payload.zip" "/resource:$flag,patch.flag" "$root\Setup.cs"
if($LASTEXITCODE){throw 'Updater build failed'}
if($FullSetup){
 $runtime=Join-Path $build 'vlc-3.0.24-win64.zip'
 if(!(Test-Path $runtime)){Invoke-WebRequest 'https://download.videolan.org/pub/videolan/vlc/3.0.24/win64/vlc-3.0.24-win64.zip' -OutFile $runtime}
 if((Get-FileHash $runtime -Algorithm SHA256).Hash.ToLowerInvariant() -ne 'fcf30850371ad10c9373cc4f0f4501e7dee49e3e9ae9f20c72fb2661a1ca6323'){throw 'VLC hash mismatch'}
 $unpack=Join-Path $build 'runtime';if(!(Test-Path $unpack)){Expand-Archive $runtime $unpack}
 New-Item -ItemType Directory -Force "$stage\vlc" | Out-Null
 Copy-Item "$unpack\vlc-3.0.24\*" "$stage\vlc" -Recurse -Force
 $full=Join-Path $build 'full.zip';if(Test-Path $full){Remove-Item -LiteralPath $full}
 [IO.Compression.ZipFile]::CreateFromDirectory((Join-Path $build 'payload'),$full)
 & $csc /nologo /target:winexe /platform:x64 /optimize+ "/out:$build\SpielfeldTV-Setup-$version.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/resource:$full,payload.zip" "$root\Setup.cs"
 if($LASTEXITCODE){throw 'Setup build failed'}
}
Get-ChildItem $build -Filter "SpielfeldTV-*-$version.exe" | ForEach-Object { $hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant();Set-Content ($_.FullName+'.sha256') ($hash+'  '+$_.Name) -Encoding ascii }







