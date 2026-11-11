param([string]$BaseUrl = 'http://localhost:5000')
$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')
$run = [guid]::NewGuid().ToString('N')
function Check($name, $method, $path, $token, $body, $status) {
    $headers = @{}
    if ($token) { $headers.Authorization = "Bearer $token" }
    $args = @{ Uri = "$base$path"; Method = $method; Headers = $headers; SkipHttpErrorCheck = $true }
    if ($null -ne $body) { $args.ContentType = 'application/json'; $args.Body = ConvertTo-Json -InputObject $body -Compress }
    $response = Invoke-WebRequest @args
    if ([int]$response.StatusCode -ne $status) { throw "$name failed: HTTP $($response.StatusCode), expected $status. $($response.Content)" }
    Write-Host "PASS $name"
    if ($response.Content) { return $response.Content | ConvertFrom-Json }
}
try {
    $users = @()
    foreach ($label in @('owner', 'member', 'viewer', 'outside')) {
        $email = "smoke-$label-$run@example.test"
        $new = Check "register $label" POST '/api/auth/register' $null @{ email=$email; password='SmokeTest!2026StrongPassword'; displayName=$label } 201
        $auth = Check "login $label" POST '/api/auth/login' $null @{ email=$email; password='SmokeTest!2026StrongPassword' } 200
        $users += @{ id=$new.id; token=$auth.accessToken }
    }
    $a,$b,$c,$outside = $users
    $project = Check 'create project' POST '/api/projects' $a.token @{ name="Smoke $run"; description='Integration smoke' } 201
    $id = $project.id
    Check 'add member' POST "/api/projects/$id/members" $a.token @{ userId=$b.id; role='Member' } 201 | Out-Null
    Check 'add viewer' POST "/api/projects/$id/members" $a.token @{ userId=$c.id; role='Viewer' } 201 | Out-Null
    $task = Check 'create task' POST "/api/projects/$id/tasks" $a.token @{ title='Smoke task'; description=''; priority='High'; assigneeId=$b.id; dueDate=$null } 201
    if ([string]$task.assigneeId -ne [string]$b.id) { throw 'Assignee mismatch' }
    $list = Check 'list tasks' GET "/api/projects/$id/tasks?page=1&pageSize=10" $b.token $null 200
    if (-not @($list.items.id).Contains($task.id)) { throw 'Task missing from list' }
    Check 'no JWT' GET "/api/tasks/$($task.id)" $null $null 401 | Out-Null
    Check 'outside project' GET "/api/projects/$id/tasks" $outside.token $null 403 | Out-Null
    Check 'viewer edit denied' PATCH "/api/tasks/$($task.id)/status" $c.token @{ status='InProgress' } 403 | Out-Null
    Check 'invalid enum' PATCH "/api/tasks/$($task.id)/status" $b.token @{ status='Invalid' } 400 | Out-Null
    Check 'change status' PATCH "/api/tasks/$($task.id)/status" $b.token @{ status='InProgress' } 200 | Out-Null
} catch { Write-Error $_; exit 1 }
