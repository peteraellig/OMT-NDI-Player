param(
    [string]$InnoCompiler = 'C:\Program Files\Inno Setup 7\ISCC.exe'
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $InnoCompiler)) {
    throw "Inno Setup Compiler nicht gefunden: $InnoCompiler"
}
Push-Location $PSScriptRoot
try {
    $publish = Join-Path $PSScriptRoot 'artifacts\publish\win-x64'
    & dotnet publish 'OMTplay\OMTplay.csproj' -c Release -r win-x64 --self-contained true `
        -p:Platform=x64 -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
        -o $publish --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Publish fehlgeschlagen.' }
    foreach ($file in @('OMTplay.exe', 'libomt.dll', 'libvmx.dll', 'Processing.NDI.Lib.x64.dll',
        'licenses\OMT-LICENSE.txt', 'licenses\NDI SDK License Agreement.pdf', 'licenses\Processing.NDI.Lib.Licenses.txt')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $file))) { throw "Fehlt: $file" }
    }
    & $InnoCompiler (Join-Path $PSScriptRoot 'OMTplay-Setup.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup Build fehlgeschlagen.' }
    Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'artifacts\installer') -Filter '*.exe' |
        Get-FileHash -Algorithm SHA256
}
finally { Pop-Location }
