$version = (Get-Content .env | Where-Object { $_ -match '^HAPPYMEAL_TAG=' }) -replace '^HAPPYMEAL_TAG=', ''
$sha = git rev-parse --short HEAD
$env:HAPPYMEAL_TAG = "$version-$sha"
docker compose build