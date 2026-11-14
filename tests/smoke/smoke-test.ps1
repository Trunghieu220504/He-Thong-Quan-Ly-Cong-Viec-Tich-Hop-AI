#Requires -Version 7.0
param(
    [string]$BaseUrl = 'http://localhost:5000',
    [ValidateSet('Baseline', 'Week5')][string]$Mode = 'Week5',
    [ValidateSet('Available', 'Unavailable')][string]$AiState = 'Available'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')
$run = [guid]::NewGuid().ToString('N')
function Check($name, $method, $path, $token, $body, $status) {
    $headers = @{}
    if ($token) { $headers.Authorization = "Bearer $token" }
    $requestArgs = @{ Uri = "$base$path"; Method = $method; Headers = $headers; SkipHttpErrorCheck = $true; TimeoutSec = 30 }
    if ($null -ne $body) { $requestArgs.ContentType = 'application/json'; $requestArgs.Body = ConvertTo-Json -InputObject $body -Compress }
    $response = Invoke-WebRequest @requestArgs
    if ([int]$response.StatusCode -ne $status) { throw "$name failed: HTTP $($response.StatusCode), expected $status. $($response.Content)" }
    Write-Host "PASS $name"
    if ($response.Content -and $response.Headers['Content-Type'] -match 'json') { return $response.Content | ConvertFrom-Json }
}
function Wait-Notification($token, $taskId, $type) {
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        $page = Check 'notifications' GET '/api/notifications?page=1&pageSize=100' $token $null 200
        $matches = @($page.items | Where-Object { $_.taskId -eq $taskId -and $_.type -eq $type })
        if ($matches.Count -gt 0) {
            if ($matches.Count -ne 1) { throw 'Duplicate notification' }
            return $matches[0]
        }
        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Missing $type notification after 5 seconds"
}
try {
    Check 'API readiness' GET '/health/ready' $null $null 200 | Out-Null
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
    $taskBody = @{ title='Smoke task'; description=''; priority='High'; assigneeId=$b.id; dueDate=$null }
    if ($Mode -eq 'Week5') { $taskBody.estimatedHours = 8 }
    $task = Check 'create task' POST "/api/projects/$id/tasks" $a.token $taskBody 201
    if ([string]$task.assigneeId -ne [string]$b.id) { throw 'Assignee mismatch' }
    if ($Mode -eq 'Week5') {
        if ($task.estimatedHours -ne 8) { throw 'Task v2 estimatedHours was not persisted' }
        $assigned = Wait-Notification $b.token $task.id 'TaskAssigned'
        $count = Check 'unread count' GET '/api/notifications/unread-count' $b.token $null 200
        if ($assigned.isRead -or $count.count -ne 1) { throw 'Expected one unread assignment notification' }
    }
    $list = Check 'list tasks' GET "/api/projects/$id/tasks?page=1&pageSize=10" $b.token $null 200
    if (-not @($list.items.id).Contains($task.id)) { throw 'Task missing from list' }
    Check 'no JWT' GET "/api/tasks/$($task.id)" $null $null 401 | Out-Null
    Check 'outside project' GET "/api/projects/$id/tasks" $outside.token $null 403 | Out-Null
    Check 'viewer edit denied' PATCH "/api/tasks/$($task.id)/status" $c.token @{ status='InProgress' } 403 | Out-Null
    Check 'invalid enum' PATCH "/api/tasks/$($task.id)/status" $b.token @{ status='Invalid' } 400 | Out-Null
    Check 'change status' PATCH "/api/tasks/$($task.id)/status" $b.token @{ status='InProgress' } 200 | Out-Null
    if ($Mode -eq 'Week5') {
        Wait-Notification $a.token $task.id 'TaskStatusChanged' | Out-Null
        $edit = @{ title='Edited smoke task'; description='Updated'; priority='High'; dueDate=$null; estimatedHours=12; progressPercent=50 }
        $updated = Check 'owner edits task' PUT "/api/tasks/$($task.id)" $a.token $edit 200
        if ($updated.estimatedHours -ne 12 -or $updated.progressPercent -ne 50) { throw 'Task edit was not persisted' }
        Check 'viewer PUT denied' PUT "/api/tasks/$($task.id)" $c.token $edit 403 | Out-Null
        Check 'foreign notification denied' PATCH "/api/notifications/$($assigned.id)/read" $a.token $null 404 | Out-Null
        # The actor must not receive its own status notification.
        $notifications = Check 'member notifications' GET '/api/notifications?page=1&pageSize=100' $b.token $null 200
        if (@($notifications.items | Where-Object { $_.taskId -eq $task.id -and $_.type -eq 'TaskStatusChanged' }).Count -ne 0) { throw 'Actor received own status notification' }
        $read = Check 'mark assignment read' PATCH "/api/notifications/$($assigned.id)/read" $b.token $null 200
        $count = Check 'unread count after read' GET '/api/notifications/unread-count' $b.token $null 200
        if (-not $read.isRead -or $count.count -ne 0) { throw 'Mark read did not reduce unread count' }
    }
    $riskStatus = if ($AiState -eq 'Available') { 200 } else { 503 }
    $risk = Check "risk AI $AiState" GET "/api/tasks/$($task.id)/risk" $a.token $null $riskStatus
    if ($AiState -eq 'Available' -and $risk.risk -notin @('low','medium','high')) { throw 'Invalid risk label' }
    $start = [DateTime]::UtcNow.Date.ToString('yyyy-MM-ddTHH:mm:ssZ')
    $end = [DateTime]::UtcNow.Date.AddDays(7).ToString('yyyy-MM-ddTHH:mm:ssZ')
    $sprint = Check 'create sprint' POST "/api/projects/$id/sprints" $a.token @{ name='Smoke sprint'; goal='Verify integration'; startDate=$start; endDate=$end } 201
    Check 'start sprint' POST "/api/sprints/$($sprint.id)/start" $a.token $null 200 | Out-Null
    Check 'add sprint task' POST "/api/sprints/$($sprint.id)/tasks" $a.token @{ taskId=$task.id } 201 | Out-Null
    Check 'complete task' PATCH "/api/tasks/$($task.id)/status" $b.token @{ status='Done' } 200 | Out-Null
    $burndown = Check 'burndown' GET "/api/sprints/$($sprint.id)/burndown" $a.token $null 200
    if ($burndown.totalTasks -ne 1 -or $burndown.completedTasks -ne 1 -or $burndown.remainingTasks -ne 0) { throw 'Expected burndown 1/1/0' }
    Check 'complete sprint' POST "/api/sprints/$($sprint.id)/complete" $a.token $null 200 | Out-Null
    if ($Mode -eq 'Week5') {
        $dashboard = Check 'dashboard' GET "/api/projects/$id/dashboard" $a.token $null 200
        if ($dashboard.tasks.total -ne 1 -or $dashboard.tasks.done -ne 1 -or $dashboard.tasks.todo -ne 0 -or $dashboard.tasks.inProgress -ne 0 -or $dashboard.tasks.overdue -ne 0 -or $dashboard.tasks.completionPercent -ne 100) { throw 'Dashboard totals mismatch' }
        $assignee = @($dashboard.byAssignee | Where-Object { $_.assigneeId -eq $b.id })
        if ($assignee.Count -ne 1 -or $assignee[0].total -ne 1 -or $assignee[0].done -ne 1) { throw 'Dashboard assignee totals mismatch' }
        $completed = Check 'read completed task' GET "/api/tasks/$($task.id)" $a.token $null 200
        if ($completed.progressPercent -ne 100) { throw 'Done must set progressPercent to 100' }
    }
    $date = [DateTime]::UtcNow.Date.AddDays(3).ToString('yyyy-MM-ddTHH:mm:ss')
    $dated = Check 'date without Z' POST "/api/projects/$id/tasks" $a.token @{ title='UTC test'; priority='Low'; dueDate=$date } 201
    # Inspect raw JSON: ConvertFrom-Json can convert ISO strings into DateTime.
    $raw = Invoke-WebRequest -Uri "$base/api/tasks/$($dated.id)" -Headers @{ Authorization="Bearer $($a.token)" } -TimeoutSec 30
    if ($raw.Content -notmatch ('"dueDate"\s*:\s*"' + [regex]::Escape($date) + '(?:Z|\+00:00)"')) { throw 'Date without Z was not stored as UTC' }
    Write-Host "PASS $Mode smoke (AI $AiState)"
} catch { Write-Error $_ -ErrorAction Continue; exit 1 }
