<#
.SYNOPSIS
Compara decisão remota e ticket UMA/RPT para um recurso sintético do Altivy.

.DESCRIPTION
Obtém um PAT do resource server altivy-api, reconcilia um recurso sintético pela
Protection API e mede a mesma permissão por resposta de decisão e por ticket
UMA/RPT. Tokens e secrets permanecem apenas em memória e nunca são exibidos.

.PARAMETER BaseUrl
URL do Keycloak sem barra final. Padrão: http://localhost:8080.

.PARAMETER Realm
Realm dedicado ao Altivy. Padrão: altivy.

.PARAMETER ApiClientSecret
Secret do altivy-api. Quando omitido, lê ALTIVY_API_CLIENT_SECRET.

.PARAMETER UserAccessToken
Access token efêmero do usuário avaliado. Quando omitido, lê
ALTIVY_SPIKE_USER_ACCESS_TOKEN ou inicia login interativo Code+PKCE pelo client
altivy-authorization-spike. Não persista esse token em arquivo.

.PARAMETER Scope
Scope avaliado. Padrão: vault:read.

.PARAMETER Iterations
Quantidade de medições de cada fluxo. Padrão: 5.

.PARAMETER RequestTimeoutSeconds
Timeout máximo de cada chamada ao Keycloak, em segundos. Padrão: 15.

.PARAMETER InteractiveLoginTimeoutSeconds
Timeout para concluir o login interativo, em segundos. Padrão: 180.

.PARAMETER ReconcileOnly
Reconcilia o recurso pela Protection API e encerra sem exigir token de usuário.

.PARAMETER DryRun
Valida parâmetros e exibe o plano sem rede ou credenciais.

.PARAMETER RemainingArguments
Argumentos literais remanescentes. Aceita somente --help.

.EXAMPLE
./tools/scripts/test-keycloak-authorization-spike.ps1 --help

.EXAMPLE
$ApiSecret = Read-Host 'Secret do altivy-api' -AsSecureString
$UserToken = Read-Host 'Access token efêmero' -AsSecureString
./tools/scripts/test-keycloak-authorization-spike.ps1 `
  -ApiClientSecret $ApiSecret -UserAccessToken $UserToken -Iterations 10

.INPUTS
Nenhum.

.OUTPUTS
Resumo JSON sanitizado com resultado, latência e tamanho do RPT.

.NOTES
Requer PowerShell 7.4 ou superior e Keycloak 26.7.x. O resource server deve ter
Authorization Services, scopes, políticas e permissões configurados.

.LINK
../../docs/configuracao-keycloak.md
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidatePattern('^https?://')]
    [string] $BaseUrl = 'http://localhost:8080',

    [ValidatePattern('^[a-z0-9-]+$')]
    [string] $Realm = 'altivy',

    [SecureString] $ApiClientSecret,

    [SecureString] $UserAccessToken,

    [ValidateSet('vault:read', 'vault:write', 'vault:manage', 'vault:delete')]
    [string] $Scope = 'vault:read',

    [ValidatePattern('^[a-z0-9-]+$')]
    [string] $ResourceName = 'vault-lab-0001',

    [ValidateRange(1, 100)]
    [int] $Iterations = 5,

    [ValidateRange(1, 120)]
    [int] $RequestTimeoutSeconds = 15,

    [ValidateRange(30, 600)]
    [int] $InteractiveLoginTimeoutSeconds = 180,

    [switch] $ReconcileOnly,

    [switch] $DryRun,

    [Parameter(ValueFromRemainingArguments)]
    [string[]] $RemainingArguments
)

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$baseUri = [Uri]$BaseUrl
$resourceName = $ResourceName
$resourceUri = "urn:altivy:vault:$ResourceName"
$apiClientId = 'altivy-api'
$authorizationClientId = 'altivy-authorization-spike'
$authorizationCallback = 'http://127.0.0.1:8765/callback/'
$useNoProxy = $baseUri.IsLoopback

if ($RemainingArguments -contains '--help') {
    Get-Help $PSCommandPath -Full
    return
}

if ($RemainingArguments.Count -gt 0) {
    [Console]::Error.WriteLine("Argumento desconhecido: $($RemainingArguments -join ' '). Use --help.")
    exit 2
}

if ($Realm -eq 'master') {
    throw 'O spike não pode usar o realm administrativo master.'
}

if ($baseUri.Scheme -ne 'https' -and -not $baseUri.IsLoopback) {
    throw 'BaseUrl deve usar HTTPS fora do ambiente local.'
}

if ($DryRun) {
    [pscustomobject]@{
        baseUrl    = $BaseUrl
        realm      = $Realm
        resource   = $resourceName
        scope      = $Scope
        iterations = $Iterations
        authentication = if ($ReconcileOnly) { 'none' } elseif ($UserAccessToken) { 'provided-token' } else { 'interactive-code-pkce' }
        operations = if ($ReconcileOnly) {
            @('reconcile-resource')
        }
        else {
            @('reconcile-resource', 'remote-decision', 'uma-ticket-rpt')
        }
    } | ConvertTo-Json -Depth 3
    return
}

function ConvertFrom-SecureValue {
    param([Parameter(Mandatory)][SecureString] $Value)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Invoke-SpikeRequest {
    param(
        [Parameter(Mandatory)][ValidateSet('GET', 'POST', 'PUT')][string] $Method,
        [Parameter(Mandatory)][string] $Uri,
        [hashtable] $Headers,
        [object] $Body,
        [string] $ContentType = 'application/json'
    )

    $parameters = @{
        Method               = $Method
        Uri                  = $Uri
        Headers              = $Headers
        ContentType          = $ContentType
        SkipHttpErrorCheck   = $true
        SkipCertificateCheck = $false
        MaximumRedirection   = 0
        TimeoutSec           = $RequestTimeoutSeconds
    }
    if ($useNoProxy) { $parameters.NoProxy = $true }
    if ($null -ne $Body) { $parameters.Body = $Body }

    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest @parameters
    }
    finally {
        $stopwatch.Stop()
    }

    [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        DurationMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
        Content    = $response.Content
    }
}

function Assert-TrustedEndpoint {
    param(
        [Parameter(Mandatory)][string] $Value,
        [Parameter(Mandatory)][string] $Name
    )

    $endpoint = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$endpoint)) {
        throw "$Name não é uma URL absoluta válida."
    }
    if ($endpoint.Scheme -ne $baseUri.Scheme -or $endpoint.Authority -ne $baseUri.Authority) {
        throw "$Name deve usar a mesma origem de BaseUrl."
    }
    if (-not [string]::IsNullOrEmpty($endpoint.UserInfo) -or
        -not [string]::IsNullOrEmpty($endpoint.Query) -or
        -not [string]::IsNullOrEmpty($endpoint.Fragment)) {
        throw "$Name não pode conter userinfo, query ou fragment."
    }

    $endpoint.AbsoluteUri
}

function ConvertFrom-JsonResponse {
    param(
        [Parameter(Mandatory)][object] $Response,
        [Parameter(Mandatory)][string] $Operation
    )

    if ($Response.StatusCode -lt 200 -or $Response.StatusCode -ge 300) {
        throw "$Operation falhou com HTTP $($Response.StatusCode)."
    }

    ConvertFrom-JsonResponseContent $Response $Operation
}

function ConvertFrom-JsonResponseContent {
    param(
        [Parameter(Mandatory)][object] $Response,
        [Parameter(Mandatory)][string] $Operation
    )

    try {
        $Response.Content | ConvertFrom-Json
    }
    catch {
        throw "$Operation retornou JSON inválido."
    }
}

function Get-DenialOutcome {
    param(
        [Parameter(Mandatory)][object] $Response,
        [Parameter(Mandatory)][string] $Operation
    )

    if ($Response.StatusCode -ne 403) {
        throw "$Operation falhou com HTTP $($Response.StatusCode); o resultado não é uma negação de política válida."
    }

    $denial = ConvertFrom-JsonResponseContent $Response $Operation
    $knownDescriptions = @('request_denied', 'not_authorized')
    if ($denial.error -ne 'access_denied' -or $denial.error_description -notin $knownDescriptions) {
        throw "$Operation retornou HTTP 403 com error '$($denial.error)' e error_description '$($denial.error_description)'."
    }

    'deny'
}

function ConvertTo-Base64Url {
    param([Parameter(Mandatory)][byte[]] $Bytes)

    [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Request-InteractiveUserAccessToken {
    param(
        [Parameter(Mandatory)][string] $AuthorizationEndpoint,
        [Parameter(Mandatory)][string] $TokenEndpoint
    )

    $verifierBytes = [byte[]]::new(64)
    [Security.Cryptography.RandomNumberGenerator]::Fill($verifierBytes)
    $codeVerifier = ConvertTo-Base64Url $verifierBytes
    $challengeBytes = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::ASCII.GetBytes($codeVerifier))
    $codeChallenge = ConvertTo-Base64Url $challengeBytes
    $stateBytes = [byte[]]::new(32)
    [Security.Cryptography.RandomNumberGenerator]::Fill($stateBytes)
    $expectedState = ConvertTo-Base64Url $stateBytes

    $query = [Web.HttpUtility]::ParseQueryString('')
    $query['client_id'] = $authorizationClientId
    $query['redirect_uri'] = $authorizationCallback
    $query['response_type'] = 'code'
    $query['scope'] = 'openid'
    $query['code_challenge'] = $codeChallenge
    $query['code_challenge_method'] = 'S256'
    $query['state'] = $expectedState
    $authorizationUri = "$AuthorizationEndpoint`?$($query.ToString())"

    $listener = [Net.HttpListener]::new()
    $listener.Prefixes.Add($authorizationCallback)
    try {
        $listener.Start()
        [Console]::Error.WriteLine("Conclua o login no browser. URL: $authorizationUri")
        Start-Process $authorizationUri
        $pendingContext = $listener.BeginGetContext($null, $null)
        if (-not $pendingContext.AsyncWaitHandle.WaitOne([TimeSpan]::FromSeconds($InteractiveLoginTimeoutSeconds))) {
            throw "Login interativo não concluído em $InteractiveLoginTimeoutSeconds segundos."
        }

        $context = $listener.EndGetContext($pendingContext)
        $callbackQuery = $context.Request.QueryString
        $browserMessage = if ($callbackQuery['error']) {
            'A autenticação não foi concluída. Volte ao terminal.'
        }
        else {
            'Autenticação concluída. Você pode fechar esta janela.'
        }
        $responseBytes = [Text.Encoding]::UTF8.GetBytes($browserMessage)
        $context.Response.StatusCode = 200
        $context.Response.ContentType = 'text/plain; charset=utf-8'
        $context.Response.ContentLength64 = $responseBytes.Length
        $context.Response.OutputStream.Write($responseBytes, 0, $responseBytes.Length)
        $context.Response.Close()

        if ($callbackQuery['error']) {
            throw "Login interativo rejeitado pelo Keycloak: $($callbackQuery['error'])."
        }
        if ([string]::IsNullOrWhiteSpace($callbackQuery['code']) -or
            [string]::IsNullOrWhiteSpace($callbackQuery['state'])) {
            throw 'Callback OIDC não retornou code e state.'
        }
        $actualStateBytes = [Text.Encoding]::ASCII.GetBytes([string]$callbackQuery['state'])
        $expectedStateBytes = [Text.Encoding]::ASCII.GetBytes($expectedState)
        if ($actualStateBytes.Length -ne $expectedStateBytes.Length -or
            -not [Security.Cryptography.CryptographicOperations]::FixedTimeEquals($actualStateBytes, $expectedStateBytes)) {
            throw 'Callback OIDC retornou state inválido.'
        }

        $tokenResponse = ConvertFrom-JsonResponse `
            (Invoke-SpikeRequest POST $TokenEndpoint -ContentType 'application/x-www-form-urlencoded' -Body @{
                    grant_type    = 'authorization_code'
                    client_id     = $authorizationClientId
                    code          = [string]$callbackQuery['code']
                    redirect_uri  = $authorizationCallback
                    code_verifier = $codeVerifier
                }) `
            'Troca do código OIDC'
        if ([string]::IsNullOrWhiteSpace($tokenResponse.access_token)) {
            throw 'Troca do código OIDC não retornou access_token.'
        }
        [string]$tokenResponse.access_token
    }
    finally {
        if ($listener.IsListening) { $listener.Stop() }
        $listener.Close()
        $codeVerifier = $null
        $tokenResponse = $null
        [Array]::Clear($verifierBytes, 0, $verifierBytes.Length)
        [Array]::Clear($stateBytes, 0, $stateBytes.Length)
    }
}

if (-not $ApiClientSecret) {
    if (-not $env:ALTIVY_API_CLIENT_SECRET) {
        throw 'Informe -ApiClientSecret ou defina ALTIVY_API_CLIENT_SECRET. Use --help para detalhes.'
    }
    $ApiClientSecret = ConvertTo-SecureString $env:ALTIVY_API_CLIENT_SECRET -AsPlainText -Force
}

if (-not $ReconcileOnly -and -not $UserAccessToken) {
    if ($env:ALTIVY_SPIKE_USER_ACCESS_TOKEN) {
        $UserAccessToken = ConvertTo-SecureString $env:ALTIVY_SPIKE_USER_ACCESS_TOKEN -AsPlainText -Force
    }
}

$apiSecretPlain = ConvertFrom-SecureValue $ApiClientSecret
$userTokenPlain = if ($UserAccessToken) { ConvertFrom-SecureValue $UserAccessToken } else { $null }
try {
    $umaDiscovery = ConvertFrom-JsonResponse `
    (Invoke-SpikeRequest GET "$BaseUrl/realms/$Realm/.well-known/uma2-configuration") `
        'Discovery UMA'
    $tokenEndpoint = Assert-TrustedEndpoint $umaDiscovery.token_endpoint 'token_endpoint'
    $authorizationEndpoint = Assert-TrustedEndpoint $umaDiscovery.authorization_endpoint 'authorization_endpoint'
    $resourceEndpoint = Assert-TrustedEndpoint $umaDiscovery.resource_registration_endpoint 'resource_registration_endpoint'
    $permissionEndpoint = Assert-TrustedEndpoint $umaDiscovery.permission_endpoint 'permission_endpoint'

    if (-not $ReconcileOnly -and -not $userTokenPlain) {
        $userTokenPlain = Request-InteractiveUserAccessToken $authorizationEndpoint $tokenEndpoint
    }

    $patResponse = ConvertFrom-JsonResponse `
    (Invoke-SpikeRequest POST $tokenEndpoint -ContentType 'application/x-www-form-urlencoded' -Body @{
            grant_type    = 'client_credentials'
            client_id     = $apiClientId
            client_secret = $apiSecretPlain
        }) `
        'Emissão do PAT'
    if (-not $patResponse.access_token) { throw 'A emissão do PAT não retornou access_token.' }
    $patHeaders = @{ Authorization = "Bearer $($patResponse.access_token)" }

    $encodedName = [Uri]::EscapeDataString($resourceName)
    $resourceQuery = ConvertFrom-JsonResponse `
    (Invoke-SpikeRequest GET "$resourceEndpoint`?name=$encodedName&exactName=true" -Headers $patHeaders) `
        'Consulta do recurso sintético'
    $resourceIds = @($resourceQuery)
    if ($resourceIds.Count -gt 1) { throw 'A Protection API retornou recursos sintéticos duplicados.' }

    $resourceBody = @{
        name               = $resourceName
        uris               = @($resourceUri)
        resource_scopes    = @('vault:read', 'vault:write', 'vault:manage', 'vault:delete')
        ownerManagedAccess = $false
    } | ConvertTo-Json -Depth 4 -Compress
    if ($resourceIds.Count -eq 0) {
        $createResponse = Invoke-SpikeRequest POST $resourceEndpoint -Headers $patHeaders -Body $resourceBody
        if ($createResponse.StatusCode -eq 409) {
            $resourceIds = @(ConvertFrom-JsonResponse `
                (Invoke-SpikeRequest GET "$resourceEndpoint`?name=$encodedName&exactName=true" -Headers $patHeaders) `
                    'Consulta do recurso após conflito')
            if ($resourceIds.Count -ne 1) { throw 'Conflito de criação não resultou em exatamente um recurso sintético.' }
            $resourceId = [string]$resourceIds[0]
        }
        else {
            $createdResource = ConvertFrom-JsonResponse $createResponse 'Criação do recurso sintético'
            $resourceId = $createdResource._id
        }
    }
    else {
        $resourceId = [string]$resourceIds[0]
        $updateResponse = Invoke-SpikeRequest PUT "$($resourceEndpoint.TrimEnd('/'))/$resourceId" `
            -Headers $patHeaders -Body $resourceBody
        if ($updateResponse.StatusCode -lt 200 -or $updateResponse.StatusCode -ge 300) {
            throw "Atualização do recurso sintético falhou com HTTP $($updateResponse.StatusCode)."
        }
    }
    if (-not $resourceId) { throw 'A Protection API não retornou o identificador do recurso sintético.' }

    if ($ReconcileOnly) {
        [pscustomobject]@{
            realm      = $Realm
            resource   = $resourceName
            reconciled = $true
        } | ConvertTo-Json
    }
    else {
        $measurements = [Collections.Generic.List[object]]::new()
        $userHeaders = @{ Authorization = "Bearer $userTokenPlain" }

        function Measure-RemoteDecision {
            param([Parameter(Mandatory)][int] $Iteration)

            $response = Invoke-SpikeRequest POST $tokenEndpoint -Headers $userHeaders `
                -ContentType 'application/x-www-form-urlencoded' -Body @{
                grant_type    = 'urn:ietf:params:oauth:grant-type:uma-ticket'
                audience      = $apiClientId
                permission    = "$resourceId#$Scope"
                response_mode = 'decision'
            }
            if ($response.StatusCode -eq 200) {
                $decision = ConvertFrom-JsonResponse $response 'Decisão remota'
                if ($decision.result -isnot [bool]) { throw 'Decisão remota não retornou result booleano.' }
                $outcome = if ($decision.result) { 'grant' } else { 'deny' }
            }
            else {
                $outcome = Get-DenialOutcome $response 'Decisão remota'
            }

            [pscustomobject]@{
                flow       = 'remote-decision'
                iteration  = $Iteration
                outcome    = $outcome
                statusCode = $response.StatusCode
                durationMs = $response.DurationMs
                tokenBytes = 0
            }
        }

        function Measure-UmaTicketRpt {
            param([Parameter(Mandatory)][int] $Iteration)

            $permissionRequest = ConvertTo-Json -InputObject @(@{
                        resource_id     = $resourceId
                        resource_scopes = @($Scope)
                    }) -Depth 4 -Compress
            $ticketResponse = Invoke-SpikeRequest POST $permissionEndpoint -Headers $patHeaders -Body $permissionRequest
            $ticket = ConvertFrom-JsonResponse $ticketResponse 'Emissão do permission ticket'
            if ([string]::IsNullOrWhiteSpace($ticket.ticket)) { throw 'Permission endpoint não retornou ticket.' }

            $rptResponse = Invoke-SpikeRequest POST $tokenEndpoint -Headers $userHeaders `
                -ContentType 'application/x-www-form-urlencoded' -Body @{
                grant_type     = 'urn:ietf:params:oauth:grant-type:uma-ticket'
                ticket         = $ticket.ticket
                submit_request = 'false'
            }
            $rptBytes = 0
            if ($rptResponse.StatusCode -eq 200) {
                $rpt = ConvertFrom-JsonResponse $rptResponse 'Emissão do RPT'
                if ([string]::IsNullOrWhiteSpace($rpt.access_token)) { throw 'Emissão do RPT não retornou access_token.' }
                $rptBytes = [Text.Encoding]::UTF8.GetByteCount([string]$rpt.access_token)
                $outcome = 'grant'
            }
            else {
                $outcome = Get-DenialOutcome $rptResponse 'Emissão do RPT'
            }

            [pscustomobject]@{
                flow       = 'uma-ticket-rpt'
                iteration  = $Iteration
                outcome    = $outcome
                statusCode = $rptResponse.StatusCode
                durationMs = [Math]::Round($ticketResponse.DurationMs + $rptResponse.DurationMs, 2)
                tokenBytes = $rptBytes
            }
        }

        for ($index = 1; $index -le $Iterations; $index++) {
            if ($index % 2 -eq 1) {
                $measurements.Add((Measure-RemoteDecision $index))
                $measurements.Add((Measure-UmaTicketRpt $index))
            }
            else {
                $measurements.Add((Measure-UmaTicketRpt $index))
                $measurements.Add((Measure-RemoteDecision $index))
            }
        }

        [pscustomobject]@{
            realm        = $Realm
            resource     = $resourceName
            scope        = $Scope
            measurements = $measurements
        } | ConvertTo-Json -Depth 5
    }
}
finally {
    $apiSecretPlain = $null
    $userTokenPlain = $null
    $patResponse = $null
    $patHeaders = $null
    $userHeaders = $null
    $ticket = $null
    $ticketResponse = $null
    $rpt = $null
    $rptResponse = $null
}