$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    throw 'Execute este utilitário no Windows PowerShell 5.1 (powershell.exe), compatível com o .NET Framework da aplicação.'
}

$applicationAssemblyPath = Join-Path $PSScriptRoot '..\OrcamentoPessoal\bin\OrcamentoPessoal.dll'
$passwordHasherSourcePath = Join-Path $PSScriptRoot '..\OrcamentoPessoal\Security\PasswordHasher.cs'

if (-not (Test-Path $applicationAssemblyPath)) {
    throw 'A aplicação ainda não foi compilada. Compile o projeto antes de gerar o hash.'
}

if ((Get-Item $passwordHasherSourcePath).LastWriteTimeUtc -gt (Get-Item $applicationAssemblyPath).LastWriteTimeUtc) {
    throw 'A implementação de hash foi alterada após a última compilação. Compile o projeto antes de gerar o hash.'
}

$applicationAssembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $applicationAssemblyPath).Path)
$passwordHasherType = $applicationAssembly.GetType('OrcamentoPessoal.Security.PasswordHasher', $true)
$generateHashMethod = $passwordHasherType.GetMethod('GerarHash', [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::Static)

function Get-PlainTextFromSecureString {
    param([System.Security.SecureString]$SecureValue)

    $pointer = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)

    try {
        return [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

$passwordSecure = Read-Host 'Digite a senha que deseja armazenar' -AsSecureString
$confirmationSecure = Read-Host 'Digite a senha novamente para confirmar' -AsSecureString
$password = $null
$confirmation = $null

try {
    $password = Get-PlainTextFromSecureString $passwordSecure
    $confirmation = Get-PlainTextFromSecureString $confirmationSecure

    if ($password -cne $confirmation) {
        throw 'As senhas informadas não conferem.'
    }

    $hash = $generateHashMethod.Invoke($null, [object[]]@($password))
    Write-Output $hash
}
finally {
    $password = $null
    $confirmation = $null
    $passwordSecure.Dispose()
    $confirmationSecure.Dispose()
}
