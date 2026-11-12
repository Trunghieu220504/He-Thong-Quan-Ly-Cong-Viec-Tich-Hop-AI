#!/usr/bin/env bash
set -eu
exec 3<>/dev/tcp/127.0.0.1/5000
printf 'GET /health/ready HTTP/1.0\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3
read -r status <&3
[[ "$status" == *" 200 "* ]]
